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
using LanguageExt;
using Microsoft.Extensions.Logging;
using OneOf;
using ParametersManagement.LibDatabaseParameters;
using SystemTools.SystemToolsShared;
using SystemTools.SystemToolsShared.Errors;
using ToolsManagement.DatabasesManagement.Errors;
using WebAgentContracts.WebAgentDatabasesApiContracts.V1.Responses;

namespace ToolsManagement.DatabasesManagement;

public sealed class SqlServerDatabaseManager : IDatabaseManager
{
    private readonly DatabaseServerConnectionDataDomain _databaseServerConnectionDataDomain;
    private readonly ILogger _logger;
    private readonly IMessagesDataManager? _messagesDataManager;
    private readonly bool _useConsole;
    private readonly string? _userName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SqlServerDatabaseManager(ILogger logger, bool useConsole,
        DatabaseServerConnectionDataDomain databaseServerConnectionDataDomain,
        IMessagesDataManager? messagesDataManager, string? userName)
    {
        _logger = logger;
        _useConsole = useConsole;
        _databaseServerConnectionDataDomain = databaseServerConnectionDataDomain;
        _messagesDataManager = messagesDataManager;
        _userName = userName;
    }

    public async ValueTask<Option<Err[]>> CheckRepairDatabase(string databaseName,
        CancellationToken cancellationToken = default)
    {
        OneOf<DbClient, Err[]> getDatabaseClientResult =
            await GetDatabaseClient(EDatabaseProvider.SqlServer, null, cancellationToken);

        if (getDatabaseClientResult.IsT1)
        {
            return getDatabaseClientResult.AsT1;
        }

        DbClient? dc = getDatabaseClientResult.AsT0;

        return await dc.CheckRepairDatabase(databaseName, cancellationToken);
    }

    //სერვერის მხარეს მონაცემთა ბაზაში ბრძანების გაშვება
    public async ValueTask<Option<Err[]>> ExecuteCommand(string executeQueryCommand, string? databaseName = null,
        CancellationToken cancellationToken = default)
    {
        OneOf<DbClient, Err[]> getDatabaseClientResult =
            await GetDatabaseClient(EDatabaseProvider.SqlServer, databaseName, cancellationToken);

        if (getDatabaseClientResult.IsT1)
        {
            return getDatabaseClientResult.AsT1;
        }

        DbClient? dc = getDatabaseClientResult.AsT0;

        return await dc.ExecuteCommand(executeQueryCommand, true, true, cancellationToken);
    }

    //მონაცემთა ბაზების სიის მიღება სერვერიდან
    public async Task<OneOf<List<DatabaseInfoModel>, Err[]>> GetDatabaseNames(
        CancellationToken cancellationToken = default)
    {
        OneOf<DbClient, Err[]> getDatabaseClientResult =
            await GetDatabaseClient(EDatabaseProvider.SqlServer, null, cancellationToken);

        if (getDatabaseClientResult.IsT1)
        {
            return getDatabaseClientResult.AsT1;
        }

        DbClient? dc = getDatabaseClientResult.AsT0;

        return await dc.GetDatabaseInfos(cancellationToken);
    }

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, იმის დასადგენად,
    //მიზნის ბაზა უკვე არსებობს თუ არა, რომ არ მოხდეს ამ ბაზის ისე წაშლა ახლით,
    //რომ არსებულის გადანახვა არ მოხდეს.
    public async Task<OneOf<bool, Err[]>> IsDatabaseExists(string databaseName,
        CancellationToken cancellationToken = default)
    {
        //მონაცემთა ბაზის კლიენტის მომზადება პროვაიდერის მიხედვით
        OneOf<DbClient, Err[]> getDatabaseClientResult =
            await GetDatabaseClient(EDatabaseProvider.SqlServer, null, cancellationToken);

        if (getDatabaseClientResult.IsT1)
        {
            return getDatabaseClientResult.AsT1;
        }

        DbClient? dc = getDatabaseClientResult.AsT0;

        return await dc.IsDatabaseExists(databaseName, cancellationToken);
    }

    //მონაცემთა ბაზაში არსებული პროცედურების რეკომპილირება
    public async ValueTask<Option<Err[]>> RecompileProcedures(string databaseName,
        CancellationToken cancellationToken = default)
    {
        OneOf<DbClient, Err[]> getDatabaseClientResult =
            await GetDatabaseClient(EDatabaseProvider.SqlServer, null, cancellationToken);

        if (getDatabaseClientResult.IsT1)
        {
            return getDatabaseClientResult.AsT1;
        }

        DbClient? dc = getDatabaseClientResult.AsT0;

        return await dc.RecompileProcedures(databaseName, cancellationToken);
    }

    public async Task<Option<Err[]>> TestConnection(string? databaseName, CancellationToken cancellationToken = default)
    {
        OneOf<DbClient, Err[]> getDatabaseClientResult =
            await GetDatabaseClient(EDatabaseProvider.SqlServer, databaseName, cancellationToken);

        if (getDatabaseClientResult.IsT1)
        {
            return getDatabaseClientResult.AsT1;
        }

        DbClient? dc = getDatabaseClientResult.AsT0;

        return await dc.TestConnection(databaseName is not null, cancellationToken);
    }

    //მონაცემთა ბაზაში არსებული სტატისტიკების დაანგარიშება
    public async ValueTask<Option<Err[]>> UpdateStatistics(string databaseName,
        CancellationToken cancellationToken = default)
    {
        OneOf<DbClient, Err[]> getDatabaseClientResult =
            await GetDatabaseClient(EDatabaseProvider.SqlServer, null, cancellationToken);

        if (getDatabaseClientResult.IsT1)
        {
            return getDatabaseClientResult.AsT1;
        }

        DbClient? dc = getDatabaseClientResult.AsT0;

        return await dc.UpdateStatistics(databaseName, cancellationToken);
    }

    public async Task<Option<Err[]>> SetDefaultFolders(string defBackupFolder, string defDataFolder,
        string defLogFolder, CancellationToken cancellationToken = default)
    {
        OneOf<DbClient, Err[]> getDatabaseClientResult =
            await GetDatabaseClient(EDatabaseProvider.SqlServer, null, cancellationToken);

        if (getDatabaseClientResult.IsT1)
        {
            return getDatabaseClientResult.AsT1;
        }

        DbClient? dc = getDatabaseClientResult.AsT0;

        return await dc.SetDefaultFolders(defBackupFolder, defDataFolder, defLogFolder, cancellationToken);
    }

    //public Task<OneOf<List<string>, Err[]>> GetDatabaseConnectionNames(CancellationToken cancellationToken)
    //{
    //    throw new NotImplementedException();
    //}

    public async Task<OneOf<List<string>, Err[]>> GetDatabaseFoldersSetNames(CancellationToken cancellationToken)
    {
        //var appSettings = AppSettings.Create(_config);

        //if (appSettings is null)
        //    return new Dictionary<string, DatabaseFoldersSet>();

        //var databaseServerConnections = new DatabaseServerConnections(appSettings.DatabaseServerConnections);
        //var getDatabaseClientResult = await GetDatabaseClient(null, cancellationToken);

        //if (getDatabaseClientResult.IsT1)
        //    return getDatabaseClientResult.AsT1;
        //var dc = getDatabaseClientResult.AsT0;

        return await Task.FromResult(_databaseServerConnectionDataDomain.DatabaseFoldersSets.Keys.ToList());
    }

    public async ValueTask<Option<Err[]>> ChangeDatabaseRecoveryModel(string databaseName,
        EDatabaseRecoveryModel databaseRecoveryModel, CancellationToken cancellationToken)
    {
        OneOf<DbClient, Err[]> getDatabaseClientResult =
            await GetDatabaseClient(EDatabaseProvider.SqlServer, null, cancellationToken);

        if (getDatabaseClientResult.IsT1)
        {
            return getDatabaseClientResult.AsT1;
        }

        DbClient? dc = getDatabaseClientResult.AsT0;

        return await dc.ChangeDatabaseRecoveryModel(databaseName, databaseRecoveryModel, cancellationToken);
    }

    //მონაცემთა ბაზების სერვერის შესახებ ზოგადი ინფორმაციის მიღება
    public async Task<OneOf<DbServerInfo, Err[]>> GetDatabaseServerInfo(CancellationToken cancellationToken = default)
    {
        OneOf<DbClient, Err[]> getDatabaseClientResult =
            await GetDatabaseClient(EDatabaseProvider.SqlServer, null, cancellationToken);

        if (getDatabaseClientResult.IsT1)
        {
            return getDatabaseClientResult.AsT1;
        }

        DbClient? dc = getDatabaseClientResult.AsT0;

        return await dc.GetDbServerInfo(cancellationToken);
    }

    //გამოიყენება იმის დასადგენად მონაცემთა ბაზის სერვერი ლოკალურია თუ არა
    public async Task<OneOf<bool, Err[]>> IsServerLocal(CancellationToken cancellationToken = default)
    {
        //მონაცემთა ბაზის კლიენტის მომზადება პროვაიდერის მიხედვით
        OneOf<DbClient, Err[]> getDatabaseClientResult =
            await GetDatabaseClient(EDatabaseProvider.SqlServer, null, cancellationToken);

        if (getDatabaseClientResult.IsT1)
        {
            return getDatabaseClientResult.AsT1;
        }

        DbClient? dc = getDatabaseClientResult.AsT0;

        return await dc.IsServerLocal(cancellationToken);
    }

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, დაკოპირებული ბაზის აღსადგენად,
    public async Task<Option<Err[]>> RestoreDatabaseFromBackup(BackupFileParameters backupFileParameters,
        string databaseName, string dbServerFoldersSetName, EDatabaseRecoveryModel databaseRecoveryModel,
        string? restoreFromFolderPath = null, CancellationToken cancellationToken = default)
    {
        //მონაცემთა ბაზის კლიენტის მომზადება პროვაიდერის მიხედვით
        OneOf<DbClient, Err[]> getDatabaseClientResult =
            await GetDatabaseClient(EDatabaseProvider.SqlServer, null, cancellationToken);

        if (getDatabaseClientResult.IsT1)
        {
            return getDatabaseClientResult.AsT1;
        }

        DbClient? dc = getDatabaseClientResult.AsT0;

        OneOf<string, Err[]> hostPlatformResult = await dc.HostPlatform(cancellationToken);

        if (hostPlatformResult.IsT1)
        {
            if (_messagesDataManager is not null)
            {
                await _messagesDataManager.SendMessage(_userName, "Host platform does not detected", cancellationToken);
            }

            _logger.LogError("Host platform does not detected");
            return Err.RecreateErrors(hostPlatformResult.AsT1,
                SqlServerDatabaseManagerErrors.HostPlatformDoesNotDetected);
        }

        string? hostPlatformName = hostPlatformResult.AsT0;

        string dirSeparator = "\\";
        if (hostPlatformName == "Linux")
        {
            dirSeparator = "/";
        }

        string? backupFolder = _databaseServerConnectionDataDomain.DatabaseFoldersSets[dbServerFoldersSetName].Backup;

        if (string.IsNullOrWhiteSpace(backupFolder))
        {
            return new[] { DbClientErrors.NoRestoreFrom };
        }

        string backupFileFullName =
            (string.IsNullOrWhiteSpace(restoreFromFolderPath) || !Directory.Exists(restoreFromFolderPath)
                ? backupFolder
                : restoreFromFolderPath).AddNeedLastPart(dirSeparator) + backupFileParameters.Name;

        OneOf<List<RestoreFileModel>, Err[]> getRestoreFilesResult =
            await dc.GetRestoreFiles(backupFileFullName, cancellationToken);
        if (getRestoreFilesResult.IsT1)
        {
            if (_messagesDataManager is not null)
            {
                await _messagesDataManager.SendMessage(_userName, "Restore Files does not detected", cancellationToken);
            }

            _logger.LogError("Restore Files does not detected");
            return Err.RecreateErrors(getRestoreFilesResult.AsT1,
                SqlServerDatabaseManagerErrors.RestoreFilesDoesNotDetected);
        }

        List<RestoreFileModel>? files = getRestoreFilesResult.AsT0;

        string? dataFolder = //_databaseBackupParameters.destinationDbServerSideDataFolderPath ??
            _databaseServerConnectionDataDomain.DatabaseFoldersSets[dbServerFoldersSetName].Data;

        if (string.IsNullOrWhiteSpace(dataFolder))
        {
            return new[] { DbClientErrors.NoDataFolder };
        }

        string? dataLogFolder = //_databaseBackupParameters.destinationDbServerSideLogFolderPath ??
            _databaseServerConnectionDataDomain.DatabaseFoldersSets[dbServerFoldersSetName].DataLog;

        if (string.IsNullOrWhiteSpace(dataLogFolder))
        {
            return new[] { DbClientErrors.NoDataLogFolder };
        }

        Option<Err[]> restoreDatabaseResult = await dc.RestoreDatabase(databaseName, backupFileFullName, files,
            dataFolder, dataLogFolder, dirSeparator, cancellationToken);

        if (restoreDatabaseResult.IsSome)
        {
            return (Err[])restoreDatabaseResult;
        }

        if (databaseRecoveryModel == EDatabaseRecoveryModel.Full)
        {
            return null;
        }

        Option<Err[]> changeDatabaseRecoveryModelResult =
            await dc.ChangeDatabaseRecoveryModel(databaseName, databaseRecoveryModel, cancellationToken);

        if (changeDatabaseRecoveryModelResult.IsSome)
        {
            return (Err[])changeDatabaseRecoveryModelResult;
        }

        return null;
    }

    //დამზადდეს ბაზის სარეზერვო ასლი სერვერის მხარეს.
    //ასევე ამ მეთოდის ამოცანაა უზრუნველყოს ბექაპის ჩამოსაქაჩად ხელმისაწვდომ ადგილას მოხვედრა
    public async ValueTask<OneOf<BackupFileParameters, Err[]>> CreateBackup(
        DatabaseBackupParametersDomain databaseBackupParameters, string backupBaseName, string dbServerFoldersSetName,
        CancellationToken cancellationToken = default)
    {
        //მონაცემთა ბაზის კლიენტის მომზადება პროვაიდერის მიხედვით
        OneOf<DbClient, Err[]> getDatabaseClientResult =
            await GetDatabaseClient(EDatabaseProvider.SqlServer, null, cancellationToken);

        if (getDatabaseClientResult.IsT1)
        {
            return getDatabaseClientResult.AsT1;
        }

        DbClient? dc = getDatabaseClientResult.AsT0;

        OneOf<string, Err[]> hostPlatformResult = await dc.HostPlatform(cancellationToken);
        if (hostPlatformResult.IsT1)
        {
            return hostPlatformResult.AsT1;
        }

        string? hostPlatformName = hostPlatformResult.AsT0;
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
            return new[] { DbClientErrors.NoBackupFolder };
        }

        string backupFileFullName = backupFolder.AddNeedLastPart(dirSeparator) + backupFileName;

        //ბექაპის ლოგიკური ფაილის სახელის მომზადება
        string backupName = backupBaseName;
        if (databaseBackupParameters.BackupType == EBackupType.Full)
        {
            backupName += "-full";
        }

        //ბექაპის პროცესის გაშვება
        Option<Err[]> backupDatabaseResult = await dc.BackupDatabase(backupBaseName, backupFileFullName, backupName,
            EBackupType.Full, databaseBackupParameters.Compress, cancellationToken);

        if (backupDatabaseResult.IsSome)
            //return await Task.FromResult<BackupFileParameters?>(null);
        {
            return (Err[])backupDatabaseResult;
        }

        if (databaseBackupParameters.Verify)
        {
            Option<Err[]> verifyBackupResult =
                await dc.VerifyBackup(backupBaseName, backupFileFullName, cancellationToken);
            if (verifyBackupResult.IsSome)
            {
                return (Err[])verifyBackupResult;
            }
        }

        var backupFileParameters = new BackupFileParameters(backupFolder, backupFileName, backupFileNamePrefix,
            backupFileNameSuffix, databaseBackupParameters.DateMask);

        return backupFileParameters;
    }

    public ValueTask<OneOf<BackupFileParameters, Err[]>> CreateBackup(string backupBaseName,
        string dbServerFoldersSetName, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    private async ValueTask<OneOf<DbClient, Err[]>> GetDatabaseClient(EDatabaseProvider dataProvider,
        string? databaseName = null, CancellationToken cancellationToken = default)
    {
        DbClient? dc = DbClientFactory.GetDbClient(_logger, _useConsole, dataProvider,
            _databaseServerConnectionDataDomain.ServerAddress, _databaseServerConnectionDataDomain.DbAuthSettings,
            _databaseServerConnectionDataDomain.TrustServerCertificate, ProgramAttributes.Instance.AppName,
            databaseName, _messagesDataManager, _userName);

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
        return new[] { SqlServerDatabaseManagerErrors.CannotCreateDbClient(databaseName) };
    }
}
