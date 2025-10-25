using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DatabasesManagement.Errors;
using DbTools;
using DbTools.Errors;
using DbTools.Models;
using DbToolsFactory;
using LanguageExt;
using LibDatabaseParameters;
using Microsoft.Extensions.Logging;
using OneOf;
using SystemToolsShared;
using SystemToolsShared.Errors;
using WebAgentDatabasesApiContracts.V1.Responses;

namespace DatabasesManagement;

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
        var getDatabaseClientResult = await GetDatabaseClient(EDatabaseProvider.SqlServer, null, cancellationToken);

        if (getDatabaseClientResult.IsT1)
            return getDatabaseClientResult.AsT1;
        var dc = getDatabaseClientResult.AsT0;

        return await dc.CheckRepairDatabase(databaseName, cancellationToken);
    }

    //სერვერის მხარეს მონაცემთა ბაზაში ბრძანების გაშვება
    public async ValueTask<Option<Err[]>> ExecuteCommand(string executeQueryCommand,
        string? databaseName = null, CancellationToken cancellationToken = default)
    {
        var getDatabaseClientResult =
            await GetDatabaseClient(EDatabaseProvider.SqlServer, databaseName, cancellationToken);

        if (getDatabaseClientResult.IsT1)
            return getDatabaseClientResult.AsT1;
        var dc = getDatabaseClientResult.AsT0;

        return await dc.ExecuteCommand(executeQueryCommand, true, true, cancellationToken);
    }

    //მონაცემთა ბაზების სიის მიღება სერვერიდან
    public async Task<OneOf<List<DatabaseInfoModel>, Err[]>> GetDatabaseNames(
        CancellationToken cancellationToken = default)
    {
        var getDatabaseClientResult = await GetDatabaseClient(EDatabaseProvider.SqlServer, null, cancellationToken);

        if (getDatabaseClientResult.IsT1)
            return getDatabaseClientResult.AsT1;
        var dc = getDatabaseClientResult.AsT0;

        return await dc.GetDatabaseInfos(cancellationToken);
    }

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, იმის დასადგენად,
    //მიზნის ბაზა უკვე არსებობს თუ არა, რომ არ მოხდეს ამ ბაზის ისე წაშლა ახლით,
    //რომ არსებულის გადანახვა არ მოხდეს.
    public async Task<OneOf<bool, Err[]>> IsDatabaseExists(string databaseName,
        CancellationToken cancellationToken = default)
    {
        //მონაცემთა ბაზის კლიენტის მომზადება პროვაიდერის მიხედვით
        var getDatabaseClientResult = await GetDatabaseClient(EDatabaseProvider.SqlServer, null, cancellationToken);

        if (getDatabaseClientResult.IsT1)
            return getDatabaseClientResult.AsT1;
        var dc = getDatabaseClientResult.AsT0;

        return await dc.IsDatabaseExists(databaseName, cancellationToken);
    }

    //მონაცემთა ბაზაში არსებული პროცედურების რეკომპილირება
    public async ValueTask<Option<Err[]>> RecompileProcedures(string databaseName,
        CancellationToken cancellationToken = default)
    {
        var getDatabaseClientResult = await GetDatabaseClient(EDatabaseProvider.SqlServer, null, cancellationToken);

        if (getDatabaseClientResult.IsT1)
            return getDatabaseClientResult.AsT1;
        var dc = getDatabaseClientResult.AsT0;

        return await dc.RecompileProcedures(databaseName, cancellationToken);
    }

    public async Task<Option<Err[]>> TestConnection(string? databaseName,
        CancellationToken cancellationToken = default)
    {
        var getDatabaseClientResult =
            await GetDatabaseClient(EDatabaseProvider.SqlServer, databaseName, cancellationToken);

        if (getDatabaseClientResult.IsT1)
            return getDatabaseClientResult.AsT1;
        var dc = getDatabaseClientResult.AsT0;

        return await dc.TestConnection(databaseName is not null, cancellationToken);
    }

    //მონაცემთა ბაზაში არსებული სტატისტიკების დაანგარიშება
    public async ValueTask<Option<Err[]>> UpdateStatistics(string databaseName,
        CancellationToken cancellationToken = default)
    {
        var getDatabaseClientResult = await GetDatabaseClient(EDatabaseProvider.SqlServer, null, cancellationToken);

        if (getDatabaseClientResult.IsT1)
            return getDatabaseClientResult.AsT1;
        var dc = getDatabaseClientResult.AsT0;

        return await dc.UpdateStatistics(databaseName, cancellationToken);
    }

    public async Task<Option<Err[]>> SetDefaultFolders(string defBackupFolder, string defDataFolder,
        string defLogFolder, CancellationToken cancellationToken = default)
    {
        var getDatabaseClientResult = await GetDatabaseClient(EDatabaseProvider.SqlServer, null, cancellationToken);

        if (getDatabaseClientResult.IsT1)
            return getDatabaseClientResult.AsT1;
        var dc = getDatabaseClientResult.AsT0;

        return await dc.SetDefaultFolders(defBackupFolder, defDataFolder, defLogFolder, cancellationToken);
    }

    //public Task<OneOf<List<string>, Err[]>> GetDatabaseConnectionNames(CancellationToken cancellationToken)
    //{
    //    throw new NotImplementedException();
    //}

    public async Task<OneOf<List<string>, Err[]>> GetDatabaseFoldersSetNames(
        CancellationToken cancellationToken)
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
        var getDatabaseClientResult = await GetDatabaseClient(EDatabaseProvider.SqlServer, null, cancellationToken);

        if (getDatabaseClientResult.IsT1)
            return getDatabaseClientResult.AsT1;
        var dc = getDatabaseClientResult.AsT0;

        return await dc.ChangeDatabaseRecoveryModel(databaseName, databaseRecoveryModel, cancellationToken);
    }

    //public Task<OneOf<DbServerInfo, Err[]>> GetDbServerInfo(CancellationToken cancellationToken = default)
    //{

    //    var dc = DbClientFactory.GetDbClient(_logger, true, EDatabaseProvider.SqlServer,
    //        _databaseServerConnectionDataDomain.ServerAddress, _databaseServerConnectionDataDomain.DbAuthSettings,
    //        _databaseServerConnectionDataDomain.TrustServerCertificate, ProgramAttributes.Instance.AppName);

    //    if (dc is null)
    //    {
    //        StShared.WriteErrorLine("Database Client is not created", true);
    //        return false;
    //    }

    //    //var testConnectionResult = dc.TestConnection(false, CancellationToken.None).Result;
    //    //if (testConnectionResult.IsSome)
    //    //{
    //    //    Err.PrintErrorsOnConsole((Err[])testConnectionResult);
    //    //    return false;
    //    //}

    //}

    //მონაცემთა ბაზების სერვერის შესახებ ზოგადი ინფორმაციის მიღება
    public async Task<OneOf<DbServerInfo, Err[]>> GetDatabaseServerInfo(
        CancellationToken cancellationToken = default)
    {
        var getDatabaseClientResult = await GetDatabaseClient(EDatabaseProvider.SqlServer, null, cancellationToken);

        if (getDatabaseClientResult.IsT1)
            return getDatabaseClientResult.AsT1;
        var dc = getDatabaseClientResult.AsT0;

        return await dc.GetDbServerInfo(cancellationToken);
    }

    //გამოიყენება იმის დასადგენად მონაცემთა ბაზის სერვერი ლოკალურია თუ არა
    public async Task<OneOf<bool, Err[]>> IsServerLocal(CancellationToken cancellationToken = default)
    {
        //მონაცემთა ბაზის კლიენტის მომზადება პროვაიდერის მიხედვით
        var getDatabaseClientResult = await GetDatabaseClient(EDatabaseProvider.SqlServer, null, cancellationToken);

        if (getDatabaseClientResult.IsT1)
            return getDatabaseClientResult.AsT1;
        var dc = getDatabaseClientResult.AsT0;

        return await dc.IsServerLocal(cancellationToken);
    }

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, დაკოპირებული ბაზის აღსადგენად,
    public async Task<Option<Err[]>> RestoreDatabaseFromBackup(BackupFileParameters backupFileParameters,
        string databaseName, string dbServerFoldersSetName, EDatabaseRecoveryModel databaseRecoveryModel,
        string? restoreFromFolderPath = null, CancellationToken cancellationToken = default)
    {
        //მონაცემთა ბაზის კლიენტის მომზადება პროვაიდერის მიხედვით
        var getDatabaseClientResult = await GetDatabaseClient(EDatabaseProvider.SqlServer, null, cancellationToken);

        if (getDatabaseClientResult.IsT1)
            return getDatabaseClientResult.AsT1;
        var dc = getDatabaseClientResult.AsT0;

        var hostPlatformResult = await dc.HostPlatform(cancellationToken);

        if (hostPlatformResult.IsT1)
        {
            if (_messagesDataManager is not null)
                await _messagesDataManager.SendMessage(_userName, "Host platform does not detected", cancellationToken);
            _logger.LogError("Host platform does not detected");
            return Err.RecreateErrors(hostPlatformResult.AsT1,
                SqlServerDatabaseManagerErrors.HostPlatformDoesNotDetected);
        }

        var hostPlatformName = hostPlatformResult.AsT0;

        var dirSeparator = "\\";
        if (hostPlatformName == "Linux")
            dirSeparator = "/";

        var backupFolder = _databaseServerConnectionDataDomain.DatabaseFoldersSets[dbServerFoldersSetName].Backup;

        if (string.IsNullOrWhiteSpace(backupFolder))
            return new[] { DbClientErrors.NoRestoreFrom };

        var backupFileFullName =
            (string.IsNullOrWhiteSpace(restoreFromFolderPath) || !Directory.Exists(restoreFromFolderPath)
                ? backupFolder
                : restoreFromFolderPath).AddNeedLastPart(dirSeparator) + backupFileParameters.Name;

        var getRestoreFilesResult = await dc.GetRestoreFiles(backupFileFullName, cancellationToken);
        if (getRestoreFilesResult.IsT1)
        {
            if (_messagesDataManager is not null)
                await _messagesDataManager.SendMessage(_userName, "Restore Files does not detected", cancellationToken);
            _logger.LogError("Restore Files does not detected");
            return Err.RecreateErrors(getRestoreFilesResult.AsT1,
                SqlServerDatabaseManagerErrors.RestoreFilesDoesNotDetected);
        }

        var files = getRestoreFilesResult.AsT0;

        var dataFolder = //_databaseBackupParameters.destinationDbServerSideDataFolderPath ??
            _databaseServerConnectionDataDomain.DatabaseFoldersSets[dbServerFoldersSetName].Data;

        if (string.IsNullOrWhiteSpace(dataFolder))
            return new[] { DbClientErrors.NoDataFolder };

        var dataLogFolder = //_databaseBackupParameters.destinationDbServerSideLogFolderPath ??
            _databaseServerConnectionDataDomain.DatabaseFoldersSets[dbServerFoldersSetName].DataLog;

        if (string.IsNullOrWhiteSpace(dataLogFolder))
            return new[] { DbClientErrors.NoDataLogFolder };

        var restoreDatabaseResult = await dc.RestoreDatabase(databaseName, backupFileFullName, files, dataFolder,
            dataLogFolder, dirSeparator, cancellationToken);

        if (restoreDatabaseResult.IsSome)
            return (Err[])restoreDatabaseResult;

        if (databaseRecoveryModel == EDatabaseRecoveryModel.Full)
            return null;

        var changeDatabaseRecoveryModelResult =
            await dc.ChangeDatabaseRecoveryModel(databaseName, databaseRecoveryModel, cancellationToken);

        if (changeDatabaseRecoveryModelResult.IsSome)
            return (Err[])changeDatabaseRecoveryModelResult;

        return null;
    }

    //დამზადდეს ბაზის სარეზერვო ასლი სერვერის მხარეს.
    //ასევე ამ მეთოდის ამოცანაა უზრუნველყოს ბექაპის ჩამოსაქაჩად ხელმისაწვდომ ადგილას მოხვედრა
    public async ValueTask<OneOf<BackupFileParameters, Err[]>> CreateBackup(
        DatabaseBackupParametersDomain databaseBackupParameters, string backupBaseName, string dbServerFoldersSetName,
        CancellationToken cancellationToken = default)
    {
        //მონაცემთა ბაზის კლიენტის მომზადება პროვაიდერის მიხედვით
        var getDatabaseClientResult = await GetDatabaseClient(EDatabaseProvider.SqlServer, null, cancellationToken);

        if (getDatabaseClientResult.IsT1)
            return getDatabaseClientResult.AsT1;
        var dc = getDatabaseClientResult.AsT0;

        var hostPlatformResult = await dc.HostPlatform(cancellationToken);
        if (hostPlatformResult.IsT1)
            return hostPlatformResult.AsT1;
        var hostPlatformName = hostPlatformResult.AsT0;
        var dirSeparator = "\\";
        if (hostPlatformName == "Linux")
            dirSeparator = "/";

        var backupFileNamePrefix = databaseBackupParameters.GetPrefix(backupBaseName);
        var backupFileNameSuffix = databaseBackupParameters.GetSuffix();
        var backupFileName = backupFileNamePrefix + DateTime.Now.ToString(databaseBackupParameters.DateMask) +
                             backupFileNameSuffix;

        var backupFolder = _databaseServerConnectionDataDomain.DatabaseFoldersSets[dbServerFoldersSetName].Backup;

        if (string.IsNullOrWhiteSpace(backupFolder))
            return new[] { DbClientErrors.NoBackupFolder };

        var backupFileFullName = backupFolder.AddNeedLastPart(dirSeparator) + backupFileName;

        //ბექაპის ლოგიკური ფაილის სახელის მომზადება
        var backupName = backupBaseName;
        if (databaseBackupParameters.BackupType == EBackupType.Full)
            backupName += "-full";

        //ბექაპის პროცესის გაშვება
        var backupDatabaseResult = await dc.BackupDatabase(backupBaseName, backupFileFullName, backupName,
            EBackupType.Full, databaseBackupParameters.Compress, cancellationToken);

        if (backupDatabaseResult.IsSome)
            //return await Task.FromResult<BackupFileParameters?>(null);
            return (Err[])backupDatabaseResult;

        if (databaseBackupParameters.Verify)
        {
            var verifyBackupResult = await dc.VerifyBackup(backupBaseName, backupFileFullName, cancellationToken);
            if (verifyBackupResult.IsSome)
                return (Err[])verifyBackupResult;
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
        var dc = DbClientFactory.GetDbClient(_logger, _useConsole, dataProvider,
            _databaseServerConnectionDataDomain.ServerAddress, _databaseServerConnectionDataDomain.DbAuthSettings,
            _databaseServerConnectionDataDomain.TrustServerCertificate, ProgramAttributes.Instance.AppName,
            databaseName, _messagesDataManager, _userName);

        if (dc is not null)
            return dc;

        if (_messagesDataManager is not null)
            await _messagesDataManager.SendMessage(_userName, $"Cannot create DbClient for database {databaseName}",
                cancellationToken);
        _logger.LogError("Cannot create DbClient for database {databaseName}", databaseName);
        return new[] { SqlServerDatabaseManagerErrors.CannotCreateDbClient(databaseName) };
    }
}