using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DatabasesManagement.Errors;
using DbTools;
using DbTools.Models;
using DbToolsFabric;
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
    private readonly DatabaseBackupParametersDomain _databaseBackupParameters;
    private readonly DatabaseServerConnectionDataDomain _databaseServerConnectionDataDomain;
    private readonly ILogger _logger;
    private readonly IMessagesDataManager? _messagesDataManager;
    private readonly bool _useConsole;
    private readonly string? _userName;

    private SqlServerDatabaseManager(ILogger logger, bool useConsole,
        DatabaseServerConnectionDataDomain databaseServerConnectionDataDomain,
        DatabaseBackupParametersDomain databaseBackupParameters, IMessagesDataManager? messagesDataManager,
        string? userName)
    {
        _logger = logger;
        _useConsole = useConsole;
        _databaseServerConnectionDataDomain = databaseServerConnectionDataDomain;
        _databaseBackupParameters = databaseBackupParameters;
        _messagesDataManager = messagesDataManager;
        _userName = userName;
    }

    //შემოწმდეს არსებული ბაზის მდგომარეობა და საჭიროების შემთხვევაში გამოასწოროს ბაზა
    public async ValueTask<Option<Err[]>> CheckRepairDatabase(string databaseName,
        CancellationToken cancellationToken = default)
    {
        var getDatabaseClientResult = await GetDatabaseClient(null, cancellationToken);

        if (getDatabaseClientResult.IsT1)
            return getDatabaseClientResult.AsT1;
        var dc = getDatabaseClientResult.AsT0;

        return await dc.CheckRepairDatabase(databaseName, cancellationToken);
    }

    //დამზადდეს ბაზის სარეზერვო ასლი სერვერის მხარეს.
    //ასევე ამ მეთოდის ამოცანაა უზრუნველყოს ბექაპის ჩამოსაქაჩად ხელმისაწვდომ ადგილას მოხვედრა
    public async ValueTask<OneOf<BackupFileParameters, Err[]>> CreateBackup(string backupBaseName,
        CancellationToken cancellationToken = default)
    {
        //მონაცემთა ბაზის კლიენტის მომზადება პროვაიდერის მიხედვით
        var getDatabaseClientResult = await GetDatabaseClient(null, cancellationToken);

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

        //string backupFileNamePrefix = databaseBackupParametersModel.BackupNamePrefix + databaseName +
        //                              databaseBackupParametersModel.BackupNameMiddlePart;
        var databaseName = backupBaseName; //dbBackupParameters.BackupBaseName;
        var backupFileNamePrefix = _databaseBackupParameters.GetPrefix(databaseName);
        //string backupFileNameSuffix = databaseBackupParametersModel.BackupFileExtension.AddNeedLeadPart(".");
        var backupFileNameSuffix = _databaseBackupParameters.GetSuffix();
        var backupFileName = backupFileNamePrefix + DateTime.Now.ToString(_databaseBackupParameters.DateMask) +
                             backupFileNameSuffix;

        var backupFolder = //_databaseBackupParameters.DbServerSideBackupPath ??
                           _databaseServerConnectionDataDomain.BackupFolderName;

        var backupFileFullName = backupFolder.AddNeedLastPart(dirSeparator) + backupFileName;

        //ბექაპის ლოგიკური ფაილის სახელის მომზადება
        var backupName = databaseName;
        if (_databaseBackupParameters.BackupType == EBackupType.Full)
            backupName += "-full";

        //ბექაპის პროცესის გაშვება
        var backupDatabaseResult = await dc.BackupDatabase(databaseName, backupFileFullName, backupName,
            EBackupType.Full, _databaseBackupParameters.Compress, cancellationToken);

        if (backupDatabaseResult.IsSome)
            //return await Task.FromResult<BackupFileParameters?>(null);
            return (Err[])backupDatabaseResult;

        if (_databaseBackupParameters.Verify)
        {
            var verifyBackupResult = await dc.VerifyBackup(databaseName, backupFileFullName, cancellationToken);
            if (verifyBackupResult.IsSome)
                return (Err[])verifyBackupResult;
        }

        BackupFileParameters backupFileParameters = new(backupFileName, backupFileNamePrefix, backupFileNameSuffix,
            _databaseBackupParameters.DateMask);

        return backupFileParameters;
    }

    //სერვერის მხარეს მონაცემთა ბაზაში ბრძანების გაშვება
    public async ValueTask<Option<Err[]>> ExecuteCommand(string executeQueryCommand, string? databaseName = null,
        CancellationToken cancellationToken = default)
    {
        var getDatabaseClientResult = await GetDatabaseClient(databaseName, cancellationToken);

        if (getDatabaseClientResult.IsT1)
            return getDatabaseClientResult.AsT1;
        var dc = getDatabaseClientResult.AsT0;

        return await dc.ExecuteCommand(executeQueryCommand, true, true, cancellationToken);
    }

    //მონაცემთა ბაზების სიის მიღება სერვერიდან
    public async Task<OneOf<List<DatabaseInfoModel>, Err[]>> GetDatabaseNames(
        CancellationToken cancellationToken = default)
    {
        var getDatabaseClientResult = await GetDatabaseClient(null, cancellationToken);

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
        var getDatabaseClientResult = await GetDatabaseClient(null, cancellationToken);

        if (getDatabaseClientResult.IsT1)
            return getDatabaseClientResult.AsT1;
        var dc = getDatabaseClientResult.AsT0;

        return await dc.IsDatabaseExists(databaseName, cancellationToken);
    }


    //მონაცემთა ბაზაში არსებული პროცედურების რეკომპილირება
    public async ValueTask<Option<Err[]>> RecompileProcedures(string databaseName,
        CancellationToken cancellationToken = default)
    {
        var getDatabaseClientResult = await GetDatabaseClient(null, cancellationToken);

        if (getDatabaseClientResult.IsT1)
            return getDatabaseClientResult.AsT1;
        var dc = getDatabaseClientResult.AsT0;

        return await dc.RecompileProcedures(databaseName, cancellationToken);
    }

    public async Task<Option<Err[]>> TestConnection(string? databaseName, CancellationToken cancellationToken = default)
    {
        var getDatabaseClientResult = await GetDatabaseClient(databaseName, cancellationToken);

        if (getDatabaseClientResult.IsT1)
            return getDatabaseClientResult.AsT1;
        var dc = getDatabaseClientResult.AsT0;

        return await dc.TestConnection(databaseName is not null, cancellationToken);
    }

    //მონაცემთა ბაზაში არსებული სტატისტიკების დაანგარიშება
    public async ValueTask<Option<Err[]>> UpdateStatistics(string databaseName,
        CancellationToken cancellationToken = default)
    {
        var getDatabaseClientResult = await GetDatabaseClient(null, cancellationToken);

        if (getDatabaseClientResult.IsT1)
            return getDatabaseClientResult.AsT1;
        var dc = getDatabaseClientResult.AsT0;

        return await dc.UpdateStatistics(databaseName, cancellationToken);
    }

    public async Task<Option<Err[]>> SetDefaultFolders(string defBackupFolder, string defDataFolder,
        string defLogFolder, CancellationToken cancellationToken = default)
    {
        var getDatabaseClientResult = await GetDatabaseClient(null, cancellationToken);

        if (getDatabaseClientResult.IsT1)
            return getDatabaseClientResult.AsT1;
        var dc = getDatabaseClientResult.AsT0;

        return await dc.SetDefaultFolders(defBackupFolder, defDataFolder, defLogFolder, cancellationToken);
    }

    //მონაცემთა ბაზების სერვერის შესახებ ზოგადი ინფორმაციის მიღება
    public async Task<OneOf<DbServerInfo, Err[]>> GetDatabaseServerInfo(CancellationToken cancellationToken = default)
    {
        var getDatabaseClientResult = await GetDatabaseClient(null, cancellationToken);

        if (getDatabaseClientResult.IsT1)
            return getDatabaseClientResult.AsT1;
        var dc = getDatabaseClientResult.AsT0;

        return await dc.GetDbServerInfo(cancellationToken);
    }

    //გამოიყენება იმის დასადგენად მონაცემთა ბაზის სერვერი ლოკალურია თუ არა
    public async Task<OneOf<bool, Err[]>> IsServerLocal(CancellationToken cancellationToken = default)
    {
        //მონაცემთა ბაზის კლიენტის მომზადება პროვაიდერის მიხედვით
        var getDatabaseClientResult = await GetDatabaseClient(null, cancellationToken);

        if (getDatabaseClientResult.IsT1)
            return getDatabaseClientResult.AsT1;
        var dc = getDatabaseClientResult.AsT0;

        return await dc.IsServerLocal(cancellationToken);
    }

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, დაკოპირებული ბაზის აღსადგენად,
    public async Task<Option<Err[]>> RestoreDatabaseFromBackup(BackupFileParameters backupFileParameters,
        //string? destinationDbServerSideDataFolderPath, string? destinationDbServerSideLogFolderPath,
        string databaseName, string? restoreFromFolderPath = null, CancellationToken cancellationToken = default)
    {
        //მონაცემთა ბაზის კლიენტის მომზადება პროვაიდერის მიხედვით
        var getDatabaseClientResult = await GetDatabaseClient(null, cancellationToken);

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


        var backupFileFullName =
            (string.IsNullOrWhiteSpace(restoreFromFolderPath) || !Directory.Exists(restoreFromFolderPath)
                ? _databaseServerConnectionDataDomain.BackupFolderName
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

        return await dc.RestoreDatabase(databaseName, backupFileFullName, files,
            /*destinationDbServerSideDataFolderPath ??*/ _databaseServerConnectionDataDomain.DataFolderName,
            /*destinationDbServerSideLogFolderPath ?? */_databaseServerConnectionDataDomain.DataLogFolderName,
            dirSeparator, cancellationToken);
    }

    public static async ValueTask<SqlServerDatabaseManager?> Create(ILogger logger, bool useConsole,
        DatabaseServerConnectionData databaseServerConnectionData, IMessagesDataManager? messagesDataManager,
        string? userName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(databaseServerConnectionData.ServerAddress))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName,
                    "ServerAddress is empty, Cannot create SqlServerManagementClient", cancellationToken);
            logger.LogError("ServerAddress is empty, Cannot create SqlServerManagementClient");
            return null;
        }

        if (string.IsNullOrWhiteSpace(databaseServerConnectionData.BackupFolderName))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName,
                    "BackupFolderName is empty, Cannot create SqlServerManagementClient", cancellationToken);
            logger.LogError("BackupFolderName is empty, Cannot create SqlServerManagementClient");
            return null;
        }

        if (string.IsNullOrWhiteSpace(databaseServerConnectionData.DataFolderName))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName,
                    "DataFolderName is empty, Cannot create SqlServerManagementClient", cancellationToken);
            logger.LogError("DataFolderName is empty, Cannot create SqlServerManagementClient");
            return null;
        }

        if (string.IsNullOrWhiteSpace(databaseServerConnectionData.DataLogFolderName))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName,
                    "DataLogFolderName is empty, Cannot create SqlServerManagementClient", cancellationToken);
            logger.LogError("DataLogFolderName is empty, Cannot create SqlServerManagementClient");
            return null;
        }

        var dbAuthSettings = DbAuthSettingsCreator.Create(databaseServerConnectionData.WindowsNtIntegratedSecurity,
            databaseServerConnectionData.ServerUser, databaseServerConnectionData.ServerPass);

        if (dbAuthSettings is null)
            return null;


        //// databaseServerConnectionData.ProductionDbServerSideBackupPath
        var destinationDbBackupParameters =
            DatabaseBackupParametersDomain.Create(databaseServerConnectionData.FullDbBackupParameters);


        if (destinationDbBackupParameters is null)
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName,
                    "destinationDbBackupParameters is null, Cannot create SqlServerManagementClient",
                    cancellationToken);
            logger.LogError("destinationDbBackupParameters is null, Cannot create SqlServerManagementClient");
            return null;
        }


        DatabaseServerConnectionDataDomain databaseServerConnectionDataDomain = new(
            databaseServerConnectionData.DataProvider, databaseServerConnectionData.ServerAddress, dbAuthSettings,
            databaseServerConnectionData.TrustServerCertificate, databaseServerConnectionData.BackupFolderName,
            databaseServerConnectionData.DataFolderName, databaseServerConnectionData.DataLogFolderName);

        return new SqlServerDatabaseManager(logger, useConsole, databaseServerConnectionDataDomain,
            destinationDbBackupParameters, messagesDataManager, userName);
    }

    private async ValueTask<OneOf<DbClient, Err[]>> GetDatabaseClient(string? databaseName = null,
        CancellationToken cancellationToken = default)
    {
        var dc = DbClientFabric.GetDbClient(_logger, _useConsole, _databaseServerConnectionDataDomain.DataProvider,
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