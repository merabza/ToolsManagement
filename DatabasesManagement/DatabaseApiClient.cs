using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DbTools.Models;
using Installer.Domain;
using LibApiClientParameters;
using LibDatabaseParameters;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OneOf;
using SystemToolsShared;
using WebAgentDatabasesApiContracts.V1.Requests;
using WebAgentProjectsApiContracts.V1.Requests;
using WebAgentProjectsApiContracts.V1.Responses;

namespace DatabasesManagement;

public sealed class DatabaseApiClient : ApiClient, IDatabaseApiClient
{
    private readonly ILogger _logger;
    private readonly ApiClientSettingsDomain _apiClientSettingsDomain;

    private readonly HttpClient _client;

    private DatabaseApiClient(ILogger logger, ApiClientSettingsDomain apiClientSettingsDomain) : base(logger,
        apiClientSettingsDomain.Server, apiClientSettingsDomain.ApiKey)
    {
        _logger = logger;
        _apiClientSettingsDomain = apiClientSettingsDomain;
        _client = new HttpClient();
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

    //შემოწმდეს არსებული ბაზის მდგომარეობა და საჭიროების შემთხვევაში გამოასწოროს ბაზა
    public async Task<bool> CheckRepairDatabase(string databaseName)
    {
        return await DoServerSide(databaseName, "checkrepairdatabase", "");
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

        var bodyApiKeyJsonData = JsonConvert.SerializeObject(new CreateBackupRequest
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

        Uri uri = new(
            $"{_apiClientSettingsDomain.Server}database/createbackup/{backupBaseName}{(string.IsNullOrWhiteSpace(_apiClientSettingsDomain.ApiKey) ? "" : $"?apikey={_apiClientSettingsDomain.ApiKey}")}");

        var response = _client
            .PostAsync(uri, new StringContent(bodyApiKeyJsonData, Encoding.UTF8, "application/json")).Result;

        if (!response.IsSuccessStatusCode)
        {
            LogResponseErrorMessage(response);
            return null;
        }

        var result = await response.Content.ReadAsStringAsync();
        var backupFileParameters = JsonConvert.DeserializeObject<BackupFileParameters>(result);
        return backupFileParameters;
    }

    //სერვერის მხარეს მონაცემთა ბაზაში ბრძანების გაშვება
    public async Task<bool> ExecuteCommand(string executeQueryCommand, string? databaseName = null)
    {
        return await DoServerSide(databaseName, "executecommand", executeQueryCommand);
    }

    public Task<DbServerInfo?> GetDatabaseServerInfo()
    {
        throw new NotImplementedException();
    }

    //მონაცემთა ბაზების სიის მიღება სერვერიდან
    public async Task<List<DatabaseInfoModel>> GetDatabaseNames()
    {
        Uri uri = new(
            $"{_apiClientSettingsDomain.Server}database/getdatabasenames{(string.IsNullOrWhiteSpace(_apiClientSettingsDomain.ApiKey) ? "" : $"?apikey={_apiClientSettingsDomain.ApiKey}")}");
        var response = await _client.GetAsync(uri);
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<List<DatabaseInfoModel>>(responseBody) ?? new List<DatabaseInfoModel>();
    }

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, იმის დასადგენად,
    //მიზნის ბაზა უკვე არსებობს თუ არა, რომ არ მოხდეს ამ ბაზის ისე წაშლა ახლით,
    //რომ არსებულის გადანახვა არ მოხდეს.
    public async Task<OneOf<bool, IEnumerable<Err>>> IsDatabaseExists(string databaseName)
    {
        Uri uri = new(
            $"{_apiClientSettingsDomain.Server}database/isdatabaseexists/{databaseName}{(string.IsNullOrWhiteSpace(_apiClientSettingsDomain.ApiKey) ? "" : $"?apikey={_apiClientSettingsDomain.ApiKey}")}");
        var response = await _client.GetAsync(uri);
        var responseBody = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode)
            return JsonConvert.DeserializeObject<bool>(responseBody);

        var errors = JsonConvert.DeserializeObject<IEnumerable<Err>>(responseBody);

        return errors is null
            ? new[] { new Err { ErrorCode = "EmptyError", ErrorMessage = "Empty Error" } }
            : errors.ToArray();
    }

    public bool IsServerLocal()
    {
        throw new NotImplementedException();
    }

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, დაკოპირებული ბაზის აღსადგენად,
    public async Task<bool> RestoreDatabaseFromBackup(BackupFileParameters backupFileParameters,
        string databaseName, string? restoreFromFolderPath = null)
    {
        var bodyApiKeyJsonData = JsonConvert.SerializeObject(new RestoreBackupRequest
        {
            Prefix = backupFileParameters.Prefix, Suffix = backupFileParameters.Suffix,
            Name = backupFileParameters.Name, DateMask = backupFileParameters.DateMask
        });

        Uri uri = new(
            $"{_apiClientSettingsDomain.Server}database/restorebackup/{databaseName}{(string.IsNullOrWhiteSpace(_apiClientSettingsDomain.ApiKey) ? "" : $"?apikey={_apiClientSettingsDomain.ApiKey}")}");

        var response =
            await _client.PutAsync(uri, new StringContent(bodyApiKeyJsonData, Encoding.UTF8, "application/json"));

        return response.IsSuccessStatusCode;
    }

    //მონაცემთა ბაზაში არსებული პროცედურების რეკომპილირება
    public async Task<bool> RecompileProcedures(string databaseName)
    {
        return await DoServerSide(databaseName, "recompileprocedures", "");
    }

    public async Task<bool> TestConnection(string? databaseName)
    {
        Uri uri = new(
            $"{_apiClientSettingsDomain.Server}database/testconnection{(databaseName == null ? "" : $"/{databaseName}")}{(string.IsNullOrWhiteSpace(_apiClientSettingsDomain.ApiKey) ? "" : $"?apikey={_apiClientSettingsDomain.ApiKey}")}");

        var response = await _client.GetAsync(uri);

        if (response.IsSuccessStatusCode)
            return true;

        LogResponseErrorMessage(response);
        return false;
    }


    //მონაცემთა ბაზაში არსებული სტატისტიკების დაანგარიშება
    public async Task<bool> UpdateStatistics(string databaseName)
    {
        return await DoServerSide(databaseName, "updatestatistics", "");
    }

    private void LogResponseErrorMessage(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;
        var errorMessage = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        _logger.LogError("Returned error message from Database ApiClient: {errorMessage}", errorMessage);
    }

    private async Task<bool> DoServerSide(string? databaseName, string methodName, string content)
    {
        Uri uri = new(
            $"{_apiClientSettingsDomain.Server}database/{methodName}{(string.IsNullOrWhiteSpace(databaseName) ? "" : $"/{databaseName}")}{(string.IsNullOrWhiteSpace(_apiClientSettingsDomain.ApiKey) ? "" : $"?apikey={_apiClientSettingsDomain.ApiKey}")}");

        var response =
            await _client.PostAsync(uri, new StringContent(content, Encoding.UTF8, "application/json"));

        if (response.IsSuccessStatusCode)
            return true;

        LogResponseErrorMessage(response);
        return false;
    }
}