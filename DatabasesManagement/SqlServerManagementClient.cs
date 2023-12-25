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
using SystemToolsShared;
using WebAgentProjectsApiContracts.V1.Responses;

namespace DatabasesManagement;

public sealed class SqlServerManagementClient : IDatabaseApiClient
{
    private readonly DatabaseServerConnectionDataDomain _databaseServerConnectionDataDomain;
    private readonly IMessagesDataManager? _messagesDataManager;
    private readonly string? _userName;
    private readonly ILogger _logger;
    private readonly bool _useConsole;

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

    public static SqlServerManagementClient? Create(ILogger logger, bool useConsole,
        DatabaseServerConnectionData databaseServerConnectionData, IMessagesDataManager? messagesDataManager,
        string? userName)
    {
        if (string.IsNullOrWhiteSpace(databaseServerConnectionData.ServerAddress))
        {
            messagesDataManager
                ?.SendMessage(userName, "ServerAddress is empty, Cannot create SqlServerManagementClient",
                    CancellationToken.None).Wait();
            logger.LogError("ServerAddress is empty, Cannot create SqlServerManagementClient");
            return null;
        }

        if (string.IsNullOrWhiteSpace(databaseServerConnectionData.BackupFolderName))
        {
            messagesDataManager
                ?.SendMessage(userName, "BackupFolderName is empty, Cannot create SqlServerManagementClient",
                    CancellationToken.None).Wait();
            logger.LogError("BackupFolderName is empty, Cannot create SqlServerManagementClient");
            return null;
        }

        if (string.IsNullOrWhiteSpace(databaseServerConnectionData.DataFolderName))
        {
            messagesDataManager
                ?.SendMessage(userName, "DataFolderName is empty, Cannot create SqlServerManagementClient",
                    CancellationToken.None).Wait();
            logger.LogError("DataFolderName is empty, Cannot create SqlServerManagementClient");
            return null;
        }

        if (string.IsNullOrWhiteSpace(databaseServerConnectionData.DataLogFolderName))
        {
            messagesDataManager
                ?.SendMessage(userName, "DataLogFolderName is empty, Cannot create SqlServerManagementClient",
                    CancellationToken.None).Wait();
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

    private DbClient GetDatabaseClient(string? databaseName = null)
    {
        var dc = DbClientFabric.GetDbClient(_logger, _useConsole, _databaseServerConnectionDataDomain.DataProvider,
            _databaseServerConnectionDataDomain.ServerAddress, _databaseServerConnectionDataDomain.DbAuthSettings,
            ProgramAttributes.Instance.GetAttribute<string>("AppName"), databaseName);

        if (dc is not null)
            return dc;

        _messagesDataManager?.SendMessage(_userName, $"Cannot create DbClient for database {databaseName}",
            CancellationToken.None).Wait();
        _logger.LogError("Cannot create DbClient for database {databaseName}", databaseName);
        throw new Exception($"Cannot create DbClient for database {databaseName}");
    }

    //შემოწმდეს არსებული ბაზის მდგომარეობა და საჭიროების შემთხვევაში გამოასწოროს ბაზა
    public async Task<bool> CheckRepairDatabase(string databaseName, CancellationToken cancellationToken)
    {
        var dc = GetDatabaseClient();
        return await dc.CheckRepairDatabase(databaseName, cancellationToken);
    }

    //დამზადდეს ბაზის სარეზერვო ასლი სერვერის მხარეს.
    //ასევე ამ მეთოდის ამოცანაა უზრუნველყოს ბექაპის ჩამოსაქაჩად ხელმისაწვდომ ადგილას მოხვედრა
    public async Task<Option<BackupFileParameters>> CreateBackup(DatabaseBackupParametersDomain dbBackupParameters,
        string backupBaseName, CancellationToken cancellationToken)
    {
        //მონაცემთა ბაზის კლიენტის მომზადება პროვაიდერის მიხედვით
        var dc = GetDatabaseClient();

        var hostPlatformName = await dc.HostPlatform(cancellationToken);
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
        if (!await dc.BackupDatabase(databaseName, backupFileFullName, backupName, EBackupType.Full,
                dbBackupParameters.Compress, cancellationToken))
            return await Task.FromResult<BackupFileParameters?>(null);

        if (dbBackupParameters.Verify)
            if (!await dc.VerifyBackup(databaseName, backupFileFullName, cancellationToken))
                return null;

        BackupFileParameters backupFileParameters = new(backupFileName, backupFileNamePrefix, backupFileNameSuffix,
            dbBackupParameters.DateMask);

        return backupFileParameters;
    }

    //სერვერის მხარეს მონაცემთა ბაზაში ბრძანების გაშვება
    public async Task<bool> ExecuteCommand(string executeQueryCommand, CancellationToken cancellationToken,
        string? databaseName = null)
    {
        var dc = GetDatabaseClient(databaseName);
        return await dc.ExecuteCommandAsync(executeQueryCommand, cancellationToken, true, true);
    }

    //მონაცემთა ბაზების სიის მიღება სერვერიდან
    public async Task<Option<List<DatabaseInfoModel>>> GetDatabaseNames(CancellationToken cancellationToken)
    {
        var dc = GetDatabaseClient();
        return await dc.GetDatabaseInfos(cancellationToken);
    }

    //მონაცემთა ბაზების სერვერის შესახებ ზოგადი ინფორმაციის მიღება
    public async Task<DbServerInfo?> GetDatabaseServerInfo(CancellationToken cancellationToken)
    {
        var dc = GetDatabaseClient();
        return await dc.GetDbServerInfo(cancellationToken);
    }

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, იმის დასადგენად,
    //მიზნის ბაზა უკვე არსებობს თუ არა, რომ არ მოხდეს ამ ბაზის ისე წაშლა ახლით,
    //რომ არსებულის გადანახვა არ მოხდეს.
    public async Task<Option<bool>> IsDatabaseExists(string databaseName, CancellationToken cancellationToken)
    {
        //მონაცემთა ბაზის კლიენტის მომზადება პროვაიდერის მიხედვით
        var dc = GetDatabaseClient();
        return await dc.CheckDatabase(databaseName, cancellationToken);
    }

    //გამოიყენება იმის დასადგენად მონაცემთა ბაზის სერვერი ლოკალურია თუ არა
    public bool IsServerLocal()
    {
        //მონაცემთა ბაზის კლიენტის მომზადება პროვაიდერის მიხედვით
        var dc = GetDatabaseClient();
        return dc.IsServerLocal();
    }


    //მონაცემთა ბაზაში არსებული პროცედურების რეკომპილირება
    public async Task<bool> RecompileProcedures(string databaseName, CancellationToken cancellationToken)
    {
        var dc = GetDatabaseClient();
        return await dc.RecompileProcedures(databaseName, cancellationToken);
    }

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, დაკოპირებული ბაზის აღსადგენად,
    public async Task<bool> RestoreDatabaseFromBackup(BackupFileParameters backupFileParameters,
        string databaseName, CancellationToken cancellationToken, string? restoreFromFolderPath = null)
    {
        //მონაცემთა ბაზის კლიენტის მომზადება პროვაიდერის მიხედვით
        var dc = GetDatabaseClient();

        var hostPlatformName = dc.HostPlatform(cancellationToken).Result;

        if (hostPlatformName is null)
        {
            if (_messagesDataManager is not null)
                await _messagesDataManager.SendMessage(_userName, "Host platform does not detected",
                    CancellationToken.None);
            _logger.LogError("Host platform does not detected");
            return false;
        }

        var dirSeparator = "\\";
        if (hostPlatformName == "Linux")
            dirSeparator = "/";


        var backupFileFullName =
            (string.IsNullOrWhiteSpace(restoreFromFolderPath) || !Directory.Exists(restoreFromFolderPath)
                ? _databaseServerConnectionDataDomain.BackupFolderName
                : restoreFromFolderPath).AddNeedLastPart(dirSeparator) + backupFileParameters.Name;

        var files = dc.GetRestoreFiles(backupFileFullName);

        return await dc.RestoreDatabase(databaseName, backupFileFullName, files,
            _databaseServerConnectionDataDomain.DataFolderName, _databaseServerConnectionDataDomain.DataLogFolderName,
            dirSeparator, cancellationToken);
    }

    public async Task<bool> TestConnection(string? databaseName, CancellationToken cancellationToken)
    {
        var dc = GetDatabaseClient(databaseName);
        return await Task.FromResult(dc.TestConnection(databaseName != null));
    }

    //მონაცემთა ბაზაში არსებული სტატისტიკების დაანგარიშება
    public async Task<bool> UpdateStatistics(string databaseName, CancellationToken cancellationToken)
    {
        var dc = GetDatabaseClient();
        return await dc.UpdateStatistics(databaseName, cancellationToken);
    }
}