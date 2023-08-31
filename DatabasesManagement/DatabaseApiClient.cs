using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DbTools.Models;
using Installer.Domain;
using LibApiClientParameters;
using LibDatabaseParameters;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SystemToolsShared;
using WebAgentDatabasesApiContracts.V1.Requests;
using WebAgentProjectsApiContracts.V1.Responses;

namespace DatabasesManagement;

public sealed class DatabaseApiClient : ApiClient, IDatabaseApiClient
{
    private readonly ILogger _logger;

    private DatabaseApiClient(ILogger logger, ApiClientSettingsDomain apiClientSettingsDomain) : base(logger,
        apiClientSettingsDomain.Server, apiClientSettingsDomain.ApiKey)
    {
        _logger = logger;
    }

    public static DatabaseApiClient? Create(ILogger logger, ApiClientSettings? apiClientSettings,
        IMessagesDataManager? messagesDataManager, string? userName)
    {
        if (apiClientSettings is null || string.IsNullOrWhiteSpace(apiClientSettings.Server))
        {
            messagesDataManager?.SendMessage(userName, "cannot create DatabaseApiClient").Wait();
            logger.LogError("cannot create DatabaseApiClient");
            return null;
        }

        ApiClientSettingsDomain apiClientSettingsDomain = new(apiClientSettings.Server, apiClientSettings.ApiKey);
        return new DatabaseApiClient(logger, apiClientSettingsDomain);
    }

    //დამზადდეს ბაზის სარეზერვო ასლი სერვერის მხარეს.
    //ასევე ამ მეთოდის ამოცანაა უზრუნველყოს ბექაპის ჩამოსაქაჩად ხელმისაწვდომ ადგილას მოხვედრა
    public async Task<BackupFileParameters?> CreateBackup(
        DatabaseBackupParametersDomain databaseBackupParametersModel, string backupBaseName)
    {
        if (string.IsNullOrWhiteSpace(backupBaseName))
        {
            _logger.LogError("Database Name does Not Specified For Backup");
            return null;
        }

        var bodyJsonData = JsonConvert.SerializeObject(new CreateBackupRequest
        {
            BackupNamePrefix = databaseBackupParametersModel.BackupNamePrefix,
            DateMask = databaseBackupParametersModel.DateMask,
            BackupFileExtension = databaseBackupParametersModel.BackupFileExtension,
            BackupNameMiddlePart = databaseBackupParametersModel.BackupNameMiddlePart,
            Compress = databaseBackupParametersModel.Compress,
            Verify = databaseBackupParametersModel.Verify,
            BackupType = databaseBackupParametersModel.BackupType,
            DbServerSideBackupPath = databaseBackupParametersModel.DbServerSideBackupPath
        });

        return await PostAsyncReturn<BackupFileParameters>($"databases/createbackup/{backupBaseName}", bodyJsonData);
    }

    //მონაცემთა ბაზების სიის მიღება სერვერიდან
    public async Task<List<DatabaseInfoModel>> GetDatabaseNames()
    {
        return await PostAsyncReturn<List<DatabaseInfoModel>>("databases/getdatabasenames") ??
               new List<DatabaseInfoModel>();
    }

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, იმის დასადგენად,
    //მიზნის ბაზა უკვე არსებობს თუ არა, რომ არ მოხდეს ამ ბაზის ისე წაშლა ახლით,
    //რომ არსებულის გადანახვა არ მოხდეს.
    public async Task<bool> IsDatabaseExists(string databaseName)
    {
        return await PostAsyncReturn<bool>($"databases/isdatabaseexists/{databaseName}");
    }

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, დაკოპირებული ბაზის აღსადგენად,
    public async Task<bool> RestoreDatabaseFromBackup(BackupFileParameters backupFileParameters,
        string databaseName, string? restoreFromFolderPath = null)
    {
        return await PostAsync($"databases/restorebackup/{databaseName}");
    }

    //შემოწმდეს არსებული ბაზის მდგომარეობა და საჭიროების შემთხვევაში გამოასწოროს ბაზა
    public async Task<bool> CheckRepairDatabase(string databaseName)
    {
        return await PostAsync(
            $"databases/checkrepairdatabase{(string.IsNullOrWhiteSpace(databaseName) ? "" : $"/{databaseName}")}");
    }

    //სერვერის მხარეს მონაცემთა ბაზაში ბრძანების გაშვება
    public async Task<bool> ExecuteCommand(string executeQueryCommand, string? databaseName = null)
    {
        return await PostAsync(
            $"databases/executecommand{(string.IsNullOrWhiteSpace(databaseName) ? "" : $"/{databaseName}")}");
    }

    public Task<DbServerInfo?> GetDatabaseServerInfo()
    {
        throw new NotImplementedException();
    }

    public bool IsServerLocal()
    {
        throw new NotImplementedException();
    }

    //მონაცემთა ბაზაში არსებული პროცედურების რეკომპილირება
    public async Task<bool> RecompileProcedures(string databaseName)
    {
        return await PostAsync(
            $"databases/recompileprocedures{(string.IsNullOrWhiteSpace(databaseName) ? "" : $"/{databaseName}")}");
    }

    public async Task<bool> TestConnection(string? databaseName)
    {
        return await GetAsync($"databases/testconnection{(databaseName == null ? "" : $"/{databaseName}")}");
    }

    //მონაცემთა ბაზაში არსებული სტატისტიკების დაანგარიშება
    public async Task<bool> UpdateStatistics(string databaseName)
    {
        return await PostAsync(
            $"databases/updatestatistics{(string.IsNullOrWhiteSpace(databaseName) ? "" : $"/{databaseName}")}");
    }
}