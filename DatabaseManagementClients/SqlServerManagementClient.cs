using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DbTools;
using DbTools.Models;
using DbToolsFabric;
using LibDatabaseParameters;
using Microsoft.Extensions.Logging;
using OneOf;
using SystemToolsShared;
using WebAgentContracts.V1.Responses;

namespace DatabaseManagementClients;

public sealed class SqlServerManagementClient : DatabaseManagementClient
{
    private readonly DatabaseServerConnectionDataDomain _databaseServerConnectionDataDomain;
    private readonly ILogger _logger;

    private SqlServerManagementClient(ILogger logger, bool useConsole,
        DatabaseServerConnectionDataDomain databaseServerConnectionDataDomain) : base(useConsole, logger)
    {
        _logger = logger;
        _databaseServerConnectionDataDomain = databaseServerConnectionDataDomain;
    }

    public static SqlServerManagementClient? Create(ILogger logger, bool useConsole,
        DatabaseServerConnectionData databaseServerConnectionData)
    {
        if (string.IsNullOrWhiteSpace(databaseServerConnectionData.ServerAddress))
        {
            logger.LogError("ServerAddress is empty, Cannot create SqlServerManagementClient");
            return null;
        }

        if (string.IsNullOrWhiteSpace(databaseServerConnectionData.BackupFolderName))
        {
            logger.LogError("BackupFolderName is empty, Cannot create SqlServerManagementClient");
            return null;
        }

        if (string.IsNullOrWhiteSpace(databaseServerConnectionData.DataFolderName))
        {
            logger.LogError("DataFolderName is empty, Cannot create SqlServerManagementClient");
            return null;
        }

        if (string.IsNullOrWhiteSpace(databaseServerConnectionData.DataLogFolderName))
        {
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

        return new SqlServerManagementClient(logger, useConsole, databaseServerConnectionDataDomain);
    }

    private DbClient GetDatabaseClient(string? databaseName = null)
    {
        var dc = DbClientFabric.GetDbClient(_logger, UseConsole, _databaseServerConnectionDataDomain.DataProvider,
            _databaseServerConnectionDataDomain.ServerAddress, _databaseServerConnectionDataDomain.DbAuthSettings,
            ProgramAttributes.Instance.GetAttribute<string>("AppName"), databaseName);

        if (dc is not null)
            return dc;

        _logger.LogError($"Cannot create DbClient for database {databaseName}");
        throw new Exception($"Cannot create DbClient for database {databaseName}");
    }

    //შემოწმდეს არსებული ბაზის მდგომარეობა და საჭიროების შემთხვევაში გამოასწოროს ბაზა
    public override async Task<bool> CheckRepairDatabase(string databaseName)
    {
        var dc = GetDatabaseClient();
        return await dc.CheckRepairDatabase(databaseName);
    }

    //დამზადდეს ბაზის სარეზერვო ასლი სერვერის მხარეს.
    //ასევე ამ მეთოდის ამოცანაა უზრუნველყოს ბექაპის ჩამოსაქაჩად ხელმისაწვდომ ადგილას მოხვედრა
    public override async Task<BackupFileParameters?> CreateBackup(DatabaseBackupParametersDomain dbBackupParameters,
        string backupBaseName)
    {
        //მონაცემთა ბაზის კლიენტის მომზადება პროვაიდერის მიხედვით
        var dc = GetDatabaseClient();

        var hostPlatformName = await dc.HostPlatform();
        var dirSeparator = "\\";
        if (hostPlatformName == "Linux")
            dirSeparator = "/";

        //string backupFileNamePrefix = databaseBackupParametersModel.BackupNamePrefix + databaseName +
        //                              databaseBackupParametersModel.BackupNameMiddlePart;
        var databaseName = backupBaseName; //dbBackupParameters.BackupBaseName;
        var backupFileNamePrefix = dbBackupParameters.GetPrefix(databaseName);
        //string backupFileNameSuffix = databaseBackupParametersModel.BackupFileExtension.AddNeedLeadPart(".");
        var backupFileNameSuffix = dbBackupParameters.GetSuffix();
        var backupFileName = backupFileNamePrefix +
                             DateTime.Now.ToString(dbBackupParameters.DateMask) +
                             backupFileNameSuffix;

        var backupFolder = dbBackupParameters.DbServerSideBackupPath ??
                           _databaseServerConnectionDataDomain.BackupFolderName;

        var backupFileFullName = backupFolder.AddNeedLastPart(dirSeparator) + backupFileName;

        //ბეკაპის ლოგიკური ფაილის სახელის მომზადება
        var backupName = databaseName;
        if (dbBackupParameters.BackupType == EBackupType.Full)
            backupName += "-full";

        //ბექაპის პროცესის გაშვება
        if (!await dc.BackupDatabase(databaseName, backupFileFullName, backupName, EBackupType.Full,
                dbBackupParameters.Compress))
            return await Task.FromResult<BackupFileParameters?>(null);

        if (dbBackupParameters.Verify)
            if (!await dc.VerifyBackup(databaseName, backupFileFullName))
                return null;

        BackupFileParameters backupFileParameters = new(backupFileName, backupFileNamePrefix, backupFileNameSuffix,
            dbBackupParameters.DateMask);

        return backupFileParameters;
    }

    //სერვერის მხარეს მონაცემთა ბაზაში ბრძანების გაშვება
    public override async Task<bool> ExecuteCommand(string executeQueryCommand, string? databaseName = null)
    {
        var dc = GetDatabaseClient(databaseName);
        return await dc.ExecuteCommandAsync(executeQueryCommand, true, true);
    }

    //მონაცემთა ბაზების სიის მიღება სერვერიდან
    public override async Task<List<DatabaseInfoModel>> GetDatabaseNames()
    {
        var dc = GetDatabaseClient();
        return await dc.GetDatabaseInfos();
    }

    //მონაცემთა ბაზების სერვერის შესახებ ზოგადი ინფორმაციის მიღება
    public override async Task<DbServerInfo?> GetDatabaseServerInfo()
    {
        var dc = GetDatabaseClient();
        return await dc.GetDbServerInfo();
    }

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, იმის დასადგენად,
    //მიზნის ბაზა უკვე არსებობს თუ არა, რომ არ მოხდეს ამ ბაზის ისე წაშლა ახლით,
    //რომ არსებულის გადანახვა არ მოხდეს.
    public override async Task<OneOf<bool, IEnumerable<Err>>> IsDatabaseExists(string databaseName)
    {
        //მონაცემთა ბაზის კლიენტის მომზადება პროვაიდერის მიხედვით
        var dc = GetDatabaseClient();
        return await dc.CheckDatabase(databaseName);
    }

    //გამოიყენება იმის დასადგენად მონაცემთა ბაზის სერვერი ლოკალურია თუ არა
    public override bool IsServerLocal()
    {
        //მონაცემთა ბაზის კლიენტის მომზადება პროვაიდერის მიხედვით
        var dc = GetDatabaseClient();
        return dc.IsServerLocal();
    }


    //მონაცემთა ბაზაში არსებული პროცედურების რეკომპილირება
    public override async Task<bool> RecompileProcedures(string databaseName)
    {
        var dc = GetDatabaseClient();
        return await dc.RecompileProcedures(databaseName);
    }

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, დაკოპირებული ბაზის აღსადგენად,
    public override async Task<bool> RestoreDatabaseFromBackup(BackupFileParameters backupFileParameters,
        string databaseName, string? restoreFromFolderPath = null)
    {
        //მონაცემთა ბაზის კლიენტის მომზადება პროვაიდერის მიხედვით
        var dc = GetDatabaseClient();

        var hostPlatformName = dc.HostPlatform().Result;

        if (hostPlatformName is null)
        {
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
            dirSeparator);
    }

    public override async Task<bool> TestConnection(string? databaseName)
    {
        var dc = GetDatabaseClient(databaseName);
        return await Task.FromResult(dc.TestConnection(databaseName != null));
    }

    //მონაცემთა ბაზაში არსებული სტატისტიკების დაანგარიშება
    public override async Task<bool> UpdateStatistics(string databaseName)
    {
        var dc = GetDatabaseClient();
        return await dc.UpdateStatistics(databaseName);
    }
}