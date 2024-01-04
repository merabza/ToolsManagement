using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DbTools;
using DbTools.Models;
using DbToolsFabric;
using LanguageExt;
using LibDatabaseParameters;
using Microsoft.Extensions.Logging;
using OneOf;
using SystemToolsShared;
using WebAgentProjectsApiContracts.V1.Responses;

namespace DatabasesManagement;

public sealed class SqlServerManagementClient : IDatabaseApiClient
{
    private readonly DatabaseServerConnectionDataDomain _databaseServerConnectionDataDomain;
    private readonly ILogger _logger;
    private readonly IMessagesDataManager? _messagesDataManager;
    private readonly bool _useConsole;
    private readonly string? _userName;

    private SqlServerManagementClient(ILogger logger, bool useConsole,
        DatabaseServerConnectionDataDomain databaseServerConnectionDataDomain,
        IMessagesDataManager? messagesDataManager, string? userName)
    {
        _logger = logger;
        _useConsole = useConsole;
        _databaseServerConnectionDataDomain = databaseServerConnectionDataDomain;
        _messagesDataManager = messagesDataManager;
        _userName = userName;
    }

    //შემოწმდეს არსებული ბაზის მდგომარეობა და საჭიროების შემთხვევაში გამოასწოროს ბაზა
    public async Task<Option<Err[]>> CheckRepairDatabase(string databaseName, CancellationToken cancellationToken)
    {
        var getDatabaseClientResult = await GetDatabaseClient(cancellationToken);

        if (getDatabaseClientResult.IsT1)
            return getDatabaseClientResult.AsT1;
        var dc = getDatabaseClientResult.AsT0;

        return await dc.CheckRepairDatabase(databaseName, cancellationToken);
    }

    //დამზადდეს ბაზის სარეზერვო ასლი სერვერის მხარეს.
    //ასევე ამ მეთოდის ამოცანაა უზრუნველყოს ბექაპის ჩამოსაქაჩად ხელმისაწვდომ ადგილას მოხვედრა
    public async Task<OneOf<BackupFileParameters, Err[]>> CreateBackup(
        DatabaseBackupParametersDomain dbBackupParameters,
        string backupBaseName, CancellationToken cancellationToken)
    {
        //მონაცემთა ბაზის კლიენტის მომზადება პროვაიდერის მიხედვით
        var getDatabaseClientResult = await GetDatabaseClient(cancellationToken);

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
        var backupFileNamePrefix = dbBackupParameters.GetPrefix(databaseName);
        //string backupFileNameSuffix = databaseBackupParametersModel.BackupFileExtension.AddNeedLeadPart(".");
        var backupFileNameSuffix = dbBackupParameters.GetSuffix();
        var backupFileName = backupFileNamePrefix + DateTime.Now.ToString(dbBackupParameters.DateMask) +
                             backupFileNameSuffix;

        var backupFolder = dbBackupParameters.DbServerSideBackupPath ??
                           _databaseServerConnectionDataDomain.BackupFolderName;

        var backupFileFullName = backupFolder.AddNeedLastPart(dirSeparator) + backupFileName;

        //ბექაპის ლოგიკური ფაილის სახელის მომზადება
        var backupName = databaseName;
        if (dbBackupParameters.BackupType == EBackupType.Full)
            backupName += "-full";

        //ბექაპის პროცესის გაშვება
        var backupDatabaseResult = await dc.BackupDatabase(databaseName, backupFileFullName, backupName,
            EBackupType.Full, dbBackupParameters.Compress, cancellationToken);

        if (backupDatabaseResult.IsSome)
            //return await Task.FromResult<BackupFileParameters?>(null);
            return (Err[])backupDatabaseResult;

        if (dbBackupParameters.Verify)
        {
            var verifyBackupResult = await dc.VerifyBackup(databaseName, backupFileFullName, cancellationToken);
            if (verifyBackupResult.IsSome)
                return (Err[])verifyBackupResult;
        }

        BackupFileParameters backupFileParameters = new(backupFileName, backupFileNamePrefix, backupFileNameSuffix,
            dbBackupParameters.DateMask);

        return backupFileParameters;
    }

    //სერვერის მხარეს მონაცემთა ბაზაში ბრძანების გაშვება
    public async Task<Option<Err[]>> ExecuteCommand(string executeQueryCommand, CancellationToken cancellationToken,
        string? databaseName = null)
    {
        var getDatabaseClientResult = await GetDatabaseClient(cancellationToken, databaseName);

        if (getDatabaseClientResult.IsT1)
            return getDatabaseClientResult.AsT1;
        var dc = getDatabaseClientResult.AsT0;

        return await dc.ExecuteCommandAsync(executeQueryCommand, cancellationToken, true, true);
    }

    //მონაცემთა ბაზების სიის მიღება სერვერიდან
    public async Task<OneOf<List<DatabaseInfoModel>, Err[]>> GetDatabaseNames(CancellationToken cancellationToken)
    {
        var getDatabaseClientResult = await GetDatabaseClient(cancellationToken);

        if (getDatabaseClientResult.IsT1)
            return getDatabaseClientResult.AsT1;
        var dc = getDatabaseClientResult.AsT0;

        return await dc.GetDatabaseInfos(cancellationToken);
    }

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, იმის დასადგენად,
    //მიზნის ბაზა უკვე არსებობს თუ არა, რომ არ მოხდეს ამ ბაზის ისე წაშლა ახლით,
    //რომ არსებულის გადანახვა არ მოხდეს.
    public async Task<OneOf<bool, Err[]>> IsDatabaseExists(string databaseName, CancellationToken cancellationToken)
    {
        //მონაცემთა ბაზის კლიენტის მომზადება პროვაიდერის მიხედვით
        var getDatabaseClientResult = await GetDatabaseClient(cancellationToken);

        if (getDatabaseClientResult.IsT1)
            return getDatabaseClientResult.AsT1;
        var dc = getDatabaseClientResult.AsT0;

        return await dc.IsDatabaseExists(databaseName, cancellationToken);
    }


    //მონაცემთა ბაზაში არსებული პროცედურების რეკომპილირება
    public async Task<Option<Err[]>> RecompileProcedures(string databaseName, CancellationToken cancellationToken)
    {
        var getDatabaseClientResult = await GetDatabaseClient(cancellationToken);

        if (getDatabaseClientResult.IsT1)
            return getDatabaseClientResult.AsT1;
        var dc = getDatabaseClientResult.AsT0;

        return await dc.RecompileProcedures(databaseName, cancellationToken);
    }

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, დაკოპირებული ბაზის აღსადგენად,
    public async Task<Option<Err[]>> RestoreDatabaseFromBackup(BackupFileParameters backupFileParameters,
        string databaseName, CancellationToken cancellationToken, string? restoreFromFolderPath = null)
    {
        //მონაცემთა ბაზის კლიენტის მომზადება პროვაიდერის მიხედვით
        var getDatabaseClientResult = await GetDatabaseClient(cancellationToken);

        if (getDatabaseClientResult.IsT1)
            return getDatabaseClientResult.AsT1;
        var dc = getDatabaseClientResult.AsT0;

        var hostPlatformResult = await dc.HostPlatform(cancellationToken);

        if (hostPlatformResult.IsT1)
        {
            if (_messagesDataManager is not null)
                await _messagesDataManager.SendMessage(_userName, "Host platform does not detected",
                    cancellationToken);
            _logger.LogError("Host platform does not detected");
            return Err.RecreateErrors(hostPlatformResult.AsT1,
                new Err
                {
                    ErrorCode = "HostPlatformDoesNotDetected", ErrorMessage = "Host platform does not detected"
                });
        }

        var hostPlatformName = hostPlatformResult.AsT0;

        var dirSeparator = "\\";
        if (hostPlatformName == "Linux")
            dirSeparator = "/";


        var backupFileFullName =
            (string.IsNullOrWhiteSpace(restoreFromFolderPath) || !Directory.Exists(restoreFromFolderPath)
                ? _databaseServerConnectionDataDomain.BackupFolderName
                : restoreFromFolderPath).AddNeedLastPart(dirSeparator) + backupFileParameters.Name;

        var getRestoreFilesResult = dc.GetRestoreFiles(backupFileFullName);
        if (getRestoreFilesResult.IsT1)
        {
            if (_messagesDataManager is not null)
                await _messagesDataManager.SendMessage(_userName, "Restore Files does not detected",
                    cancellationToken);
            _logger.LogError("Restore Files does not detected");
            return Err.RecreateErrors(getRestoreFilesResult.AsT1,
                new Err
                {
                    ErrorCode = "RestoreFilesDoesNotDetected", ErrorMessage = "Restore Files does not detected"
                });
        }

        var files = getRestoreFilesResult.AsT0;

        return await dc.RestoreDatabase(databaseName, backupFileFullName, files,
            _databaseServerConnectionDataDomain.DataFolderName, _databaseServerConnectionDataDomain.DataLogFolderName,
            dirSeparator, cancellationToken);
    }

    public async Task<Option<Err[]>> TestConnection(string? databaseName, CancellationToken cancellationToken)
    {
        var getDatabaseClientResult = await GetDatabaseClient(cancellationToken, databaseName);

        if (getDatabaseClientResult.IsT1)
            return getDatabaseClientResult.AsT1;
        var dc = getDatabaseClientResult.AsT0;

        return dc.TestConnection(databaseName != null);
    }

    //მონაცემთა ბაზაში არსებული სტატისტიკების დაანგარიშება
    public async Task<Option<Err[]>> UpdateStatistics(string databaseName, CancellationToken cancellationToken)
    {
        var getDatabaseClientResult = await GetDatabaseClient(cancellationToken);

        if (getDatabaseClientResult.IsT1)
            return getDatabaseClientResult.AsT1;
        var dc = getDatabaseClientResult.AsT0;

        return await dc.UpdateStatistics(databaseName, cancellationToken);
    }

    public static async Task<SqlServerManagementClient?> Create(ILogger logger, bool useConsole,
        DatabaseServerConnectionData databaseServerConnectionData, IMessagesDataManager? messagesDataManager,
        string? userName, CancellationToken cancellationToken)
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
                    "BackupFolderName is empty, Cannot create SqlServerManagementClient",
                    cancellationToken);
            logger.LogError("BackupFolderName is empty, Cannot create SqlServerManagementClient");
            return null;
        }

        if (string.IsNullOrWhiteSpace(databaseServerConnectionData.DataFolderName))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName,
                    "DataFolderName is empty, Cannot create SqlServerManagementClient",
                    cancellationToken);
            logger.LogError("DataFolderName is empty, Cannot create SqlServerManagementClient");
            return null;
        }

        if (string.IsNullOrWhiteSpace(databaseServerConnectionData.DataLogFolderName))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName,
                    "DataLogFolderName is empty, Cannot create SqlServerManagementClient",
                    cancellationToken);
            logger.LogError("DataLogFolderName is empty, Cannot create SqlServerManagementClient");
            return null;
        }

        var dbAuthSettings = DbAuthSettingsCreator.Create(
            databaseServerConnectionData.WindowsNtIntegratedSecurity,
            databaseServerConnectionData.ServerUser, databaseServerConnectionData.ServerPass);

        if (dbAuthSettings is null)
            return null;

        DatabaseServerConnectionDataDomain databaseServerConnectionDataDomain = new(
            databaseServerConnectionData.DataProvider, databaseServerConnectionData.ServerAddress,
            dbAuthSettings,
            databaseServerConnectionData.BackupFolderName, databaseServerConnectionData.DataFolderName,
            databaseServerConnectionData.DataLogFolderName);

        return new SqlServerManagementClient(logger, useConsole, databaseServerConnectionDataDomain,
            messagesDataManager, userName);
    }

    private async Task<OneOf<DbClient, Err[]>> GetDatabaseClient(CancellationToken cancellationToken,
        string? databaseName = null)
    {
        var dc = DbClientFabric.GetDbClient(_logger, _useConsole, _databaseServerConnectionDataDomain.DataProvider,
            _databaseServerConnectionDataDomain.ServerAddress, _databaseServerConnectionDataDomain.DbAuthSettings,
            ProgramAttributes.Instance.GetAttribute<string>("AppName"), databaseName, _messagesDataManager);

        if (dc is not null)
            return dc;

        if (_messagesDataManager is not null)
            await _messagesDataManager.SendMessage(_userName, $"Cannot create DbClient for database {databaseName}",
                cancellationToken);
        _logger.LogError("Cannot create DbClient for database {databaseName}", databaseName);
        //throw new Exception($"Cannot create DbClient for database {databaseName}");
        return new Err[]
        {
            new()
            {
                ErrorCode = "CannotCreateDbClient", ErrorMessage = $"Cannot create DbClient for database {databaseName}"
            }
        };
    }

    //მონაცემთა ბაზების სერვერის შესახებ ზოგადი ინფორმაციის მიღება
    public async Task<OneOf<DbServerInfo, Err[]>> GetDatabaseServerInfo(CancellationToken cancellationToken)
    {
        var getDatabaseClientResult = await GetDatabaseClient(cancellationToken);

        if (getDatabaseClientResult.IsT1)
            return getDatabaseClientResult.AsT1;
        var dc = getDatabaseClientResult.AsT0;

        return await dc.GetDbServerInfo(cancellationToken);
    }

    //გამოიყენება იმის დასადგენად მონაცემთა ბაზის სერვერი ლოკალურია თუ არა
    public async Task<OneOf<bool, Err[]>> IsServerLocal(CancellationToken cancellationToken)
    {
        //მონაცემთა ბაზის კლიენტის მომზადება პროვაიდერის მიხედვით
        var getDatabaseClientResult = await GetDatabaseClient(cancellationToken);

        if (getDatabaseClientResult.IsT1)
            return getDatabaseClientResult.AsT1;
        var dc = getDatabaseClientResult.AsT0;

        return await dc.IsServerLocal(cancellationToken);
    }
}