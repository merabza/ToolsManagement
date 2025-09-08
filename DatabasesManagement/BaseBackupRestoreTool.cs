using System.Threading;
using System.Threading.Tasks;
using DatabasesManagement.Models;
using DbTools;
using FileManagersMain;
using LibDatabaseParameters;
using LibFileParameters.Models;
using Microsoft.Extensions.Logging;
using SystemToolsShared.Errors;
using WebAgentDatabasesApiContracts.V1.Responses;

namespace DatabasesManagement;

public sealed class BaseBackupRestoreTool
{
    private readonly BaseBackupParameters _baseBackupParameters;
    private readonly ILogger _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public BaseBackupRestoreTool(ILogger logger, BaseBackupParameters baseBackupParameters)
    {
        _logger = logger;
        _baseBackupParameters = baseBackupParameters;
    }

    public async Task<bool> RestoreDatabaseFromBackup(BackupFileParameters backupFileParameters,
        EDatabaseRecoveryModel databaseRecoveryModel, CancellationToken cancellationToken = default)
    {
        var backupRestoreParameters = _baseBackupParameters.BackupRestoreParameters;
        var databaseManager = backupRestoreParameters.DatabaseManager;
        var databaseName = backupRestoreParameters.DatabaseName;

        //მიზნის ბაზის აღდგენა აქაჩული ბექაპის გამოყენებით
        _logger.LogInformation("Restoring database {destinationDatabaseName}", databaseName);

        var restoreDatabaseFromBackupResult = await databaseManager.RestoreDatabaseFromBackup(backupFileParameters,
            databaseName, backupRestoreParameters.DbServerFoldersSetName, databaseRecoveryModel,
            _baseBackupParameters.LocalPath, cancellationToken);

        if (restoreDatabaseFromBackupResult.IsNone)
            return true;

        Err.PrintErrorsOnConsole((Err[])restoreDatabaseFromBackupResult);
        _logger.LogError("something went wrong");
        return false;
    }

    public async Task<BackupFileParameters?> CreateDatabaseBackup(CancellationToken cancellationToken = default)
    {
        var backupRestoreParameters = _baseBackupParameters.BackupRestoreParameters;
        var databaseManager = backupRestoreParameters.DatabaseManager;
        var databaseName = backupRestoreParameters.DatabaseName;
        _logger.LogInformation("Check if Destination base {databaseName} exists", databaseName);

        //შევამოწმოთ მიზნის ბაზის არსებობა
        var isDatabaseExistsResult = await databaseManager.IsDatabaseExists(databaseName, cancellationToken);

        if (isDatabaseExistsResult.IsT1)
        {
            _logger.LogInformation("The existence of the base could not be determined");
            return null;
        }

        var isDatabaseExists = isDatabaseExistsResult.AsT0;

        if (!isDatabaseExists)
        {
            _logger.LogWarning("database {databaseName} does not exist", databaseName);
            return null;
        }

        _logger.LogInformation("Create backup of existing database {databaseName}", databaseName);

        //ბექაპის დამზადება წყაროს მხარეს
        var createBackupResult = await databaseManager.CreateBackup(
            new DatabaseBackupParametersDomain(_baseBackupParameters.BackupNamePrefix, _baseBackupParameters.DateMask,
                _baseBackupParameters.BackupFileExtension, _baseBackupParameters.BackupNameMiddlePart,
                _baseBackupParameters.Compress, _baseBackupParameters.Verify, _baseBackupParameters.BackupType),
            databaseName, backupRestoreParameters.DbServerFoldersSetName, cancellationToken);

        //თუ ბექაპის დამზადებისას რაიმე პრობლემა დაფიქსირდა, ვჩერდებით.
        if (createBackupResult.IsT1)
        {
            _logger.LogError("Backup not created");
            return null;
        }

        var backupFileParametersForSource = createBackupResult.AsT0;
        var backupCreateFolderName = backupFileParametersForSource.FolderName;
        var fileName = backupFileParametersForSource.Name;
        var prefix = backupFileParametersForSource.Prefix;
        var suffix = backupFileParametersForSource.Suffix;
        var dateMask = backupFileParametersForSource.DateMask;

        if (!string.IsNullOrWhiteSpace(backupCreateFolderName) &&
            !FileStorageData.IsSameToLocal(backupRestoreParameters.FileStorage, backupCreateFolderName))
        {
            var backupFolderFileManager = FileManagersFactory.CreateFileManager(true, _logger, backupCreateFolderName,
                _baseBackupParameters.LocalPath);

            if (backupFolderFileManager == null)
            {
                _logger.LogError("backupFolderFileManager does Not Created");
                return null;
            }

            _logger.LogInformation("Download File {fileName}", fileName);

            //წყაროდან ლოკალურ ფოლდერში მოქაჩვა
            if (!backupFolderFileManager.DownloadFile(fileName, _baseBackupParameters.DownloadTempExtension))
            {
                _logger.LogError("Can not Download File {fileName}", fileName);
                return null;
            }

            _logger.LogInformation("Remove Redundant Files for source");
            backupFolderFileManager.RemoveRedundantFiles(prefix, dateMask, suffix, backupRestoreParameters.SmartSchema);
        }

        //თუ წყაროს ფაილსაცავი ლოკალურია და მისი ფოლდერი ემთხვევა პარამეტრების ლოკალურ ფოლდერს.
        //   მაშინ მოქაჩვა საჭირო აღარ არის
        else if (_baseBackupParameters.NeedDownload)
        {
            _logger.LogInformation("Download File {fileName}", fileName);

            //წყაროდან ლოკალურ ფოლდერში მოქაჩვა
            if (!backupRestoreParameters.FileManager.DownloadFile(fileName,
                    _baseBackupParameters.DownloadTempExtension))
            {
                _logger.LogError("Can not Download File {fileName}", fileName);
                return null;
            }

            _logger.LogInformation("Remove Redundant Files for source");
            backupRestoreParameters.FileManager.RemoveRedundantFiles(prefix, dateMask, suffix,
                backupRestoreParameters.SmartSchema);
        }

        if (_baseBackupParameters is not { NeedUploadToExchange: true, ExchangeFileManager: not null })
            return null;

        _logger.LogInformation("Upload File {fileName} to Exchange", fileName);

        if (!_baseBackupParameters.ExchangeFileManager.UploadFile(fileName, _baseBackupParameters.UploadTempExtension))
        {
            _logger.LogError("Can not Upload File {destinationFileName}", fileName);
            return null;
        }

        _logger.LogInformation("Remove Redundant Files for local");
        _baseBackupParameters.LocalFileManager.RemoveRedundantFiles(prefix, dateMask, suffix,
            _baseBackupParameters.LocalSmartSchema);

        backupFileParametersForSource.FolderName = null;
        return backupFileParametersForSource;
    }
}