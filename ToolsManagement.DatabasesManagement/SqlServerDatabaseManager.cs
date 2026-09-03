using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DatabaseTools.DbTools;
using DatabaseTools.DbTools.Errors;
using DatabaseTools.DbTools.Models;
using DatabaseTools.DbToolsFactory;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibDatabaseParameters;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;
using ToolsManagement.DatabasesManagement.Errors;
using WebAgentContracts.WebAgentDatabasesApiContracts.V1.Responses;

namespace ToolsManagement.DatabasesManagement;

public sealed class SqlServerDatabaseManager : IDatabaseManager
{
    private readonly string _appName;
    private readonly DatabaseServerConnectionDataDomain _databaseServerConnectionDataDomain;
    private readonly ILogger _logger;
    private readonly IMessagesDataManager? _messagesDataManager;
    private readonly bool _useConsole;
    private readonly string? _userName;

    
    public SqlServerDatabaseManager(string appName, ILogger logger, bool useConsole,
        DatabaseServerConnectionDataDomain databaseServerConnectionDataDomain,
        IMessagesDataManager? messagesDataManager, string? userName)
    {
        _appName = appName;
        _logger = logger;
        _useConsole = useConsole;
        _databaseServerConnectionDataDomain = databaseServerConnectionDataDomain;
        _messagesDataManager = messagesDataManager;
        _userName = userName;
    }

    public async ValueTask<Result> CheckRepairDatabase(string databaseName,
        CancellationToken cancellationToken = default)
    {
        Result<DbClient> getDatabaseClientResult =
            await GetDatabaseClient(EDatabaseProvider.SqlServer, null, cancellationToken);

        if (getDatabaseClientResult.IsFailure)
        {
            return getDatabaseClientResult.Error;
        }

        DbClient dc = getDatabaseClientResult.Value;

        return await dc.CheckRepairDatabase(databaseName, cancellationToken);
    }

    //სერვერის მხარეს მონაცემთა ბაზაში ბრძანების გაშვება
    public async ValueTask<Result> ExecuteCommand(string executeQueryCommand, string? databaseName = null,
        CancellationToken cancellationToken = default)
    {
        Result<DbClient> getDatabaseClientResult =
            await GetDatabaseClient(EDatabaseProvider.SqlServer, databaseName, cancellationToken);

        if (getDatabaseClientResult.IsFailure)
        {
            return getDatabaseClientResult.Error;
        }

        DbClient dc = getDatabaseClientResult.Value;

        return await dc.ExecuteCommand(executeQueryCommand, true, true, cancellationToken);
    }

    //მონაცემთა ბაზების სიის მიღება სერვერიდან
    public async Task<Result<List<DatabaseInfoModel>>> GetDatabaseNames(CancellationToken cancellationToken = default)
    {
        Result<DbClient> getDatabaseClientResult =
            await GetDatabaseClient(EDatabaseProvider.SqlServer, null, cancellationToken);

        if (getDatabaseClientResult.IsFailure)
        {
            return getDatabaseClientResult.Error;
        }

        DbClient dc = getDatabaseClientResult.Value;

        return await dc.GetDatabaseInfos(cancellationToken);
    }

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, იმის დასადგენად,
    //მიზნის ბაზა უკვე არსებობს თუ არა, რომ არ მოხდეს ამ ბაზის ისე წაშლა ახლით,
    //რომ არსებულის გადანახვა არ მოხდეს.
    public async Task<Result<bool>> IsDatabaseExists(string databaseName, CancellationToken cancellationToken = default)
    {
        //მონაცემთა ბაზის კლიენტის მომზადება პროვაიდერის მიხედვით
        Result<DbClient> getDatabaseClientResult =
            await GetDatabaseClient(EDatabaseProvider.SqlServer, null, cancellationToken);

        if (getDatabaseClientResult.IsFailure)
        {
            return getDatabaseClientResult.Error;
        }

        DbClient dc = getDatabaseClientResult.Value;

        return await dc.IsDatabaseExists(databaseName, cancellationToken);
    }

    //მონაცემთა ბაზაში არსებული პროცედურების რეკომპილირება
    public async ValueTask<Result> RecompileProcedures(string databaseName,
        CancellationToken cancellationToken = default)
    {
        Result<DbClient> getDatabaseClientResult =
            await GetDatabaseClient(EDatabaseProvider.SqlServer, null, cancellationToken);

        if (getDatabaseClientResult.IsFailure)
        {
            return getDatabaseClientResult.Error;
        }

        DbClient dc = getDatabaseClientResult.Value;

        return await dc.RecompileProcedures(databaseName, cancellationToken);
    }

    public async Task<Result> TestConnection(string? databaseName, CancellationToken cancellationToken = default)
    {
        Result<DbClient> getDatabaseClientResult =
            await GetDatabaseClient(EDatabaseProvider.SqlServer, databaseName, cancellationToken);

        if (getDatabaseClientResult.IsFailure)
        {
            return getDatabaseClientResult.Error;
        }

        DbClient dc = getDatabaseClientResult.Value;

        return await dc.TestConnection(databaseName is not null, cancellationToken);
    }

    //მონაცემთა ბაზაში არსებული სტატისტიკების დაანგარიშება
    public async ValueTask<Result> UpdateStatistics(string databaseName, CancellationToken cancellationToken = default)
    {
        Result<DbClient> getDatabaseClientResult =
            await GetDatabaseClient(EDatabaseProvider.SqlServer, null, cancellationToken);

        if (getDatabaseClientResult.IsFailure)
        {
            return getDatabaseClientResult.Error;
        }

        DbClient dc = getDatabaseClientResult.Value;

        return await dc.UpdateStatistics(databaseName, cancellationToken);
    }

    public async Task<Result> SetDefaultFolders(string defBackupFolder, string defDataFolder, string defLogFolder,
        CancellationToken cancellationToken = default)
    {
        Result<DbClient> getDatabaseClientResult =
            await GetDatabaseClient(EDatabaseProvider.SqlServer, null, cancellationToken);

        if (getDatabaseClientResult.IsFailure)
        {
            return getDatabaseClientResult.Error;
        }

        DbClient dc = getDatabaseClientResult.Value;

        return await dc.SetDefaultFolders(defBackupFolder, defDataFolder, defLogFolder, cancellationToken);
    }

    //public Task<Result<List<string>>> GetDatabaseConnectionNames(CancellationToken cancellationToken)
    //{
    //    throw new NotImplementedException();
    //}

    public async Task<Result<List<string>>> GetDatabaseFoldersSetNames(CancellationToken cancellationToken)
    {
        //var appSettings = AppSettings.Create(_config);

        //if (appSettings is null)
        //    return new Dictionary<string, DatabaseFoldersSet>();

        //var databaseServerConnections = new DatabaseServerConnections(appSettings.DatabaseServerConnections);
        //var getDatabaseClientResult = await GetDatabaseClient(null, cancellationToken);

        //if (getDatabaseClientResult.IsFailure)
        //    return getDatabaseClientResult.Error;
        //var dc = getDatabaseClientResult.Value;

        return await Task.FromResult(_databaseServerConnectionDataDomain.DatabaseFoldersSets.Keys.ToList());
    }

    public async ValueTask<Result> ChangeDatabaseRecoveryModel(string databaseName,
        EDatabaseRecoveryModel databaseRecoveryModel, CancellationToken cancellationToken)
    {
        Result<DbClient> getDatabaseClientResult =
            await GetDatabaseClient(EDatabaseProvider.SqlServer, null, cancellationToken);

        if (getDatabaseClientResult.IsFailure)
        {
            return getDatabaseClientResult.Error;
        }

        DbClient dc = getDatabaseClientResult.Value;

        return await dc.ChangeDatabaseRecoveryModel(databaseName, databaseRecoveryModel, cancellationToken);
    }

    //მონაცემთა ბაზების სერვერის შესახებ ზოგადი ინფორმაციის მიღება
    public async Task<Result<DbServerInfo>> GetDatabaseServerInfo(CancellationToken cancellationToken = default)
    {
        Result<DbClient> getDatabaseClientResult =
            await GetDatabaseClient(EDatabaseProvider.SqlServer, null, cancellationToken);

        if (getDatabaseClientResult.IsFailure)
        {
            return getDatabaseClientResult.Error;
        }

        DbClient dc = getDatabaseClientResult.Value;

        return await dc.GetDbServerInfo(cancellationToken);
    }

    //გამოიყენება იმის დასადგენად მონაცემთა ბაზის სერვერი ლოკალურია თუ არა
    public async Task<Result<bool>> IsServerLocal(CancellationToken cancellationToken = default)
    {
        //მონაცემთა ბაზის კლიენტის მომზადება პროვაიდერის მიხედვით
        Result<DbClient> getDatabaseClientResult =
            await GetDatabaseClient(EDatabaseProvider.SqlServer, null, cancellationToken);

        if (getDatabaseClientResult.IsFailure)
        {
            return getDatabaseClientResult.Error;
        }

        DbClient dc = getDatabaseClientResult.Value;

        return await dc.IsServerLocal(cancellationToken);
    }

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, დაკოპირებული ბაზის აღსადგენად,
    public async Task<Result> RestoreDatabaseFromBackup(BackupFileParameters backupFileParameters, string databaseName,
        string dbServerFoldersSetName, EDatabaseRecoveryModel databaseRecoveryModel,
        string? restoreFromFolderPath = null, CancellationToken cancellationToken = default)
    {
        //მონაცემთა ბაზის კლიენტის მომზადება პროვაიდერის მიხედვით
        Result<DbClient> getDatabaseClientResult =
            await GetDatabaseClient(EDatabaseProvider.SqlServer, null, cancellationToken);

        if (getDatabaseClientResult.IsFailure)
        {
            return getDatabaseClientResult.Error;
        }

        DbClient dc = getDatabaseClientResult.Value;

        Result<string> hostPlatformResult = await dc.HostPlatform(cancellationToken);

        if (hostPlatformResult.IsFailure)
        {
            if (_messagesDataManager is not null)
            {
                await _messagesDataManager.SendMessage(_userName, "Host platform does not detected", cancellationToken);
            }

            _logger.LogError("Host platform does not detected");
            return hostPlatformResult.Error;
        }

        string hostPlatformName = hostPlatformResult.Value;

        string dirSeparator = "\\";
        if (hostPlatformName == "Linux")
        {
            dirSeparator = "/";
        }

        string? backupFolder = _databaseServerConnectionDataDomain.DatabaseFoldersSets[dbServerFoldersSetName].Backup;

        if (string.IsNullOrWhiteSpace(backupFolder))
        {
            return DbClientErrors.NoRestoreFrom;
        }

        string backupFileFullName =
            (string.IsNullOrWhiteSpace(restoreFromFolderPath) || !Directory.Exists(restoreFromFolderPath)
                ? backupFolder
                : restoreFromFolderPath).AddNeedLastPart(dirSeparator) + backupFileParameters.Name;

        Result<List<RestoreFileModel>> getRestoreFilesResult =
            await dc.GetRestoreFiles(backupFileFullName, cancellationToken);
        if (getRestoreFilesResult.IsFailure)
        {
            if (_messagesDataManager is not null)
            {
                await _messagesDataManager.SendMessage(_userName, "Restore Files does not detected", cancellationToken);
            }

            _logger.LogError("Restore Files does not detected");
            return getRestoreFilesResult.Error;
        }

        List<RestoreFileModel> files = getRestoreFilesResult.Value;

        string? dataFolder = //_databaseBackupParameters.destinationDbServerSideDataFolderPath ??
            _databaseServerConnectionDataDomain.DatabaseFoldersSets[dbServerFoldersSetName].Data;

        if (string.IsNullOrWhiteSpace(dataFolder))
        {
            return DbClientErrors.NoDataFolder;
        }

        string? dataLogFolder = //_databaseBackupParameters.destinationDbServerSideLogFolderPath ??
            _databaseServerConnectionDataDomain.DatabaseFoldersSets[dbServerFoldersSetName].DataLog;

        if (string.IsNullOrWhiteSpace(dataLogFolder))
        {
            return DbClientErrors.NoDataLogFolder;
        }

        Result restoreDatabaseResult = await dc.RestoreDatabase(databaseName, backupFileFullName, files, dataFolder,
            dataLogFolder, dirSeparator, cancellationToken);

        if (restoreDatabaseResult.IsFailure)
        {
            return restoreDatabaseResult;
        }

        if (databaseRecoveryModel == EDatabaseRecoveryModel.Full)
        {
            return null;
        }

        Result changeDatabaseRecoveryModelResult =
            await dc.ChangeDatabaseRecoveryModel(databaseName, databaseRecoveryModel, cancellationToken);

        if (changeDatabaseRecoveryModelResult.IsFailure)
        {
            return changeDatabaseRecoveryModelResult;
        }

        return null;
    }

    //დამზადდეს ბაზის სარეზერვო ასლი სერვერის მხარეს.
    //ასევე ამ მეთოდის ამოცანაა უზრუნველყოს ბექაპის ჩამოსაქაჩად ხელმისაწვდომ ადგილას მოხვედრა
    public async ValueTask<Result<BackupFileParameters>> CreateBackup(
        DatabaseBackupParametersDomain databaseBackupParameters, string backupBaseName, string dbServerFoldersSetName,
        CancellationToken cancellationToken = default)
    {
        //მონაცემთა ბაზის კლიენტის მომზადება პროვაიდერის მიხედვით
        Result<DbClient> getDatabaseClientResult =
            await GetDatabaseClient(EDatabaseProvider.SqlServer, null, cancellationToken);

        if (getDatabaseClientResult.IsFailure)
        {
            return getDatabaseClientResult.Error;
        }

        DbClient dc = getDatabaseClientResult.Value;

        Result<string> hostPlatformResult = await dc.HostPlatform(cancellationToken);
        if (hostPlatformResult.IsFailure)
        {
            return hostPlatformResult.Error;
        }

        string hostPlatformName = hostPlatformResult.Value;
        string dirSeparator = "\\";
        if (hostPlatformName == "Linux")
        {
            dirSeparator = "/";
        }

        string backupFileNamePrefix = databaseBackupParameters.GetPrefix(backupBaseName);
        string backupFileNameSuffix = databaseBackupParameters.GetSuffix();
        string backupFileName = backupFileNamePrefix +
                                DateTime.Now.ToString(databaseBackupParameters.DateMask, CultureInfo.InvariantCulture) +
                                backupFileNameSuffix;

        string? backupFolder = _databaseServerConnectionDataDomain.DatabaseFoldersSets[dbServerFoldersSetName].Backup;

        if (string.IsNullOrWhiteSpace(backupFolder))
        {
            return DbClientErrors.NoBackupFolder;
        }

        string backupFileFullName = backupFolder.AddNeedLastPart(dirSeparator) + backupFileName;

        //ბექაპის ლოგიკური ფაილის სახელის მომზადება
        string backupName = backupBaseName;
        if (databaseBackupParameters.BackupType == EBackupType.Full)
        {
            backupName += "-full";
        }

        //ბექაპის პროცესის გაშვება
        Result backupDatabaseResult = await dc.BackupDatabase(backupBaseName, backupFileFullName, backupName,
            EBackupType.Full, databaseBackupParameters.Compress, cancellationToken);

        if (backupDatabaseResult.IsFailure)
            //return await Task.FromResult<BackupFileParameters?>(null);
        {
            return backupDatabaseResult.Error;
        }

        if (databaseBackupParameters.Verify)
        {
            Result verifyBackupResult = await dc.VerifyBackup(backupBaseName, backupFileFullName, cancellationToken);
            if (verifyBackupResult.IsFailure)
            {
                return verifyBackupResult.Error;
            }
        }

        var backupFileParameters = new BackupFileParameters(backupFolder, backupFileName, backupFileNamePrefix,
            backupFileNameSuffix, databaseBackupParameters.DateMask);

        return backupFileParameters;
    }

    //public ValueTask<Result<BackupFileParameters>> CreateBackup(string backupBaseName,
    //    string dbServerFoldersSetName, CancellationToken cancellationToken = default)
    //{
    //    throw new NotImplementedException();
    //}

    private async ValueTask<Result<DbClient>> GetDatabaseClient(EDatabaseProvider dataProvider,
        string? databaseName = null, CancellationToken cancellationToken = default)
    {
        DbClient? dc = DbClientFactory.GetDbClient(_logger, _useConsole, dataProvider,
            _databaseServerConnectionDataDomain.ServerAddress, _databaseServerConnectionDataDomain.DbAuthSettings,
            _databaseServerConnectionDataDomain.TrustServerCertificate, _appName, databaseName, _messagesDataManager,
            _userName);

        if (dc is not null)
        {
            return dc;
        }

        if (_messagesDataManager is not null)
        {
            await _messagesDataManager.SendMessage(_userName, $"Cannot create DbClient for database {databaseName}",
                cancellationToken);
        }

        _logger.LogError("Cannot create DbClient for database {DatabaseName}", databaseName);
        return SqlServerDatabaseManagerErrors.CannotCreateDbClient(databaseName);
    }
}
