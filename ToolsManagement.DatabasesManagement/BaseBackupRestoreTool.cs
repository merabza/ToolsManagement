using System.Threading;
using System.Threading.Tasks;
using DatabaseTools.DbTools;
using LanguageExt;
using Microsoft.Extensions.Logging;
using OneOf;
using ParametersManagement.LibDatabaseParameters;
using ParametersManagement.LibFileParameters.Models;
using SystemTools.SystemToolsShared.Errors;
using ToolsManagement.DatabasesManagement.Models;
using ToolsManagement.FileManagersMain;
using WebAgentContracts.WebAgentDatabasesApiContracts.V1.Responses;

namespace ToolsManagement.DatabasesManagement;

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
        BackupRestoreParameters backupRestoreParameters = _baseBackupParameters.BackupRestoreParameters;
        IDatabaseManager databaseManager = backupRestoreParameters.DatabaseManager;
        string databaseName = backupRestoreParameters.DatabaseName;

        //მიზნის ბაზის აღდგენა აქაჩული ბექაპის გამოყენებით
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Restoring database {DestinationDatabaseName}", databaseName);
        }

        Option<Err[]> restoreDatabaseFromBackupResult = await databaseManager.RestoreDatabaseFromBackup(
            backupFileParameters, databaseName, backupRestoreParameters.DbServerFoldersSetName, databaseRecoveryModel,
            _baseBackupParameters.LocalPath, cancellationToken);

        if (restoreDatabaseFromBackupResult.IsNone)
        {
            return true;
        }

        Err.PrintErrorsOnConsole((Err[])restoreDatabaseFromBackupResult);
        _logger.LogError("something went wrong");
        return false;
    }

    public async Task<BackupFileParameters?> CreateDatabaseBackup(CancellationToken cancellationToken = default)
    {
        BackupRestoreParameters backupRestoreParameters = _baseBackupParameters.BackupRestoreParameters;
        IDatabaseManager databaseManager = backupRestoreParameters.DatabaseManager;
        string databaseName = backupRestoreParameters.DatabaseName;

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Check if Destination base {DatabaseName} exists", databaseName);
        }

        //შევამოწმოთ მიზნის ბაზის არსებობა
        OneOf<bool, Err[]> isDatabaseExistsResult =
            await databaseManager.IsDatabaseExists(databaseName, cancellationToken);

        if (isDatabaseExistsResult.IsT1)
        {
            _logger.LogInformation("The existence of the base could not be determined");
            return null;
        }

        bool isDatabaseExists = isDatabaseExistsResult.AsT0;

        if (!isDatabaseExists)
        {
            _logger.LogWarning("database {DatabaseName} does not exist", databaseName);
            return null;
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Create backup of existing database {DatabaseName}", databaseName);
        }

        //ბექაპის დამზადება წყაროს მხარეს
        OneOf<BackupFileParameters, Err[]> createBackupResult = await databaseManager.CreateBackup(
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

        BackupFileParameters? backupFileParametersForSource = createBackupResult.AsT0;
        string? backupCreateFolderName = backupFileParametersForSource.FolderName;
        string fileName = backupFileParametersForSource.Name;
        string prefix = backupFileParametersForSource.Prefix;
        string suffix = backupFileParametersForSource.Suffix;
        string dateMask = backupFileParametersForSource.DateMask;

        if (!string.IsNullOrWhiteSpace(backupCreateFolderName) &&
            !FileStorageData.IsSameToLocal(backupRestoreParameters.FileStorage, backupCreateFolderName))
        {
            FileManager? backupFolderFileManager = FileManagersFactory.CreateFileManager(true, _logger,
                backupCreateFolderName, _baseBackupParameters.LocalPath);

            if (backupFolderFileManager == null)
            {
                _logger.LogError("backupFolderFileManager does Not Created");
                return null;
            }

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Download File {FileName}", fileName);
            }

            //წყაროდან ლოკალურ ფოლდერში მოქაჩვა
            if (!backupFolderFileManager.DownloadFile(fileName, _baseBackupParameters.DownloadTempExtension))
            {
                _logger.LogError("Can not Download File {FileName}", fileName);
                return null;
            }

            _logger.LogInformation("Remove Redundant Files for source");
            backupFolderFileManager.RemoveRedundantFiles(prefix, dateMask, suffix, backupRestoreParameters.SmartSchema);
        }

        //თუ წყაროს ფაილსაცავი ლოკალურია და მისი ფოლდერი ემთხვევა პარამეტრების ლოკალურ ფოლდერს.
        //   მაშინ მოქაჩვა საჭირო აღარ არის
        else if (_baseBackupParameters.NeedDownload)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Download File {FileName}", fileName);
            }

            //წყაროდან ლოკალურ ფოლდერში მოქაჩვა
            if (!backupRestoreParameters.FileManager.DownloadFile(fileName,
                    _baseBackupParameters.DownloadTempExtension))
            {
                _logger.LogError("Can not Download File {FileName}", fileName);
                return null;
            }

            _logger.LogInformation("Remove Redundant Files for source");
            backupRestoreParameters.FileManager.RemoveRedundantFiles(prefix, dateMask, suffix,
                backupRestoreParameters.SmartSchema);
        }

        if (_baseBackupParameters is not { NeedUploadToExchange: true, ExchangeFileManager: not null })
        {
            return null;
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Upload File {FileName} to Exchange", fileName);
        }

        if (!_baseBackupParameters.ExchangeFileManager.UploadFile(fileName, _baseBackupParameters.UploadTempExtension))
        {
            _logger.LogError("Can not Upload File {DestinationFileName}", fileName);
            return null;
        }

        _logger.LogInformation("Remove Redundant Files for local");
        _baseBackupParameters.LocalFileManager.RemoveRedundantFiles(prefix, dateMask, suffix,
            _baseBackupParameters.LocalSmartSchema);

        backupFileParametersForSource.FolderName = null;
        return backupFileParametersForSource;
    }
}
