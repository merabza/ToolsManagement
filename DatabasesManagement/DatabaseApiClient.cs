using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ApiClientsManagement;
using DatabasesManagement.Responses;
using DbTools.ErrorModels;
using DbTools.Models;
using LanguageExt;
using LibApiClientParameters;
using LibDatabaseParameters;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OneOf;
using SystemToolsShared;
using WebAgentDatabasesApiContracts.V1.Requests;
using WebAgentProjectsApiContracts.V1.Requests;

namespace DatabasesManagement;

public sealed class DatabaseApiClient : ApiClient, IDatabaseApiClient
{
    private readonly ILogger _logger;

    private DatabaseApiClient(ILogger logger, ApiClientSettingsDomain apiClientSettingsDomain) : base(logger,
        apiClientSettingsDomain.Server, apiClientSettingsDomain.ApiKey, null, apiClientSettingsDomain.WithMessaging)
    {
        _logger = logger;
    }

    //დამზადდეს ბაზის სარეზერვო ასლი სერვერის მხარეს.
    //ასევე ამ მეთოდის ამოცანაა უზრუნველყოს ბექაპის ჩამოსაქაჩად ხელმისაწვდომ ადგილას მოხვედრა
    public async Task<OneOf<BackupFileParameters, Err[]>> CreateBackup(
        DatabaseBackupParametersDomain databaseBackupParametersModel, string backupBaseName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(backupBaseName))
        {
            _logger.LogError(DbClientErrors.DatabaseNameIsNotSpecifiedForBackup.ErrorMessage);
            return new[] { DbClientErrors.DatabaseNameIsNotSpecifiedForBackup };
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

        return await PostAsyncReturn<BackupFileParameters>($"databases/createbackup/{backupBaseName}",
            cancellationToken, bodyJsonData);
    }

    //მონაცემთა ბაზების სიის მიღება სერვერიდან
    public async Task<OneOf<List<DatabaseInfoModel>, Err[]>> GetDatabaseNames(CancellationToken cancellationToken)
    {
        return await GetAsyncReturn<List<DatabaseInfoModel>>("databases/getdatabasenames", cancellationToken);
    }

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, იმის დასადგენად,
    //მიზნის ბაზა უკვე არსებობს თუ არა, რომ არ მოხდეს ამ ბაზის ისე წაშლა ახლით,
    //რომ არსებულის გადანახვა არ მოხდეს.
    public async Task<OneOf<bool, Err[]>> IsDatabaseExists(string databaseName, CancellationToken cancellationToken)
    {
        return await GetAsyncReturn<bool>($"databases/isdatabaseexists/{databaseName}", cancellationToken);
    }

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, დაკოპირებული ბაზის აღსადგენად,
    public async Task<Option<Err[]>> RestoreDatabaseFromBackup(BackupFileParameters backupFileParameters,
        string? destinationDbServerSideDataFolderPath, string? destinationDbServerSideLogFolderPath,
        string databaseName, CancellationToken cancellationToken, string? restoreFromFolderPath = null)
    {
        var bodyJsonData = JsonConvert.SerializeObject(new RestoreBackupRequest
        {
            Prefix = backupFileParameters.Prefix,
            Suffix = backupFileParameters.Suffix,
            Name = backupFileParameters.Name,
            DateMask = backupFileParameters.DateMask,
            DestinationDbServerSideDataFolderPath = destinationDbServerSideDataFolderPath,
            DestinationDbServerSideLogFolderPath = destinationDbServerSideLogFolderPath
        });
        return await PutAsync($"databases/restorebackup/{databaseName}", cancellationToken, bodyJsonData);
    }

    //შემოწმდეს არსებული ბაზის მდგომარეობა და საჭიროების შემთხვევაში გამოასწოროს ბაზა
    public async Task<Option<Err[]>> CheckRepairDatabase(string databaseName, CancellationToken cancellationToken)
    {
        return await PostAsync(
            $"databases/checkrepairdatabase{(string.IsNullOrWhiteSpace(databaseName) ? "" : $"/{databaseName}")}",
            cancellationToken);
    }

    //სერვერის მხარეს მონაცემთა ბაზაში ბრძანების გაშვება
    public async Task<Option<Err[]>> ExecuteCommand(string executeQueryCommand, CancellationToken cancellationToken,
        string? databaseName = null)
    {
        return await PostAsync(
            $"databases/executecommand{(string.IsNullOrWhiteSpace(databaseName) ? "" : $"/{databaseName}")}",
            cancellationToken);
    }

    public Task<OneOf<DbServerInfo, Err[]>> GetDatabaseServerInfo(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<OneOf<bool, Err[]>> IsServerLocal(CancellationToken cancellationToken)
    {
        return await Task.FromResult(false);
    }

    //მონაცემთა ბაზაში არსებული პროცედურების რეკომპილირება
    public async Task<Option<Err[]>> RecompileProcedures(string databaseName, CancellationToken cancellationToken)
    {
        return await PostAsync(
            $"databases/recompileprocedures{(string.IsNullOrWhiteSpace(databaseName) ? "" : $"/{databaseName}")}",
            cancellationToken);
    }

    public async Task<Option<Err[]>> TestConnection(string? databaseName, CancellationToken cancellationToken)
    {
        return await GetAsync($"databases/testconnection{(databaseName == null ? "" : $"/{databaseName}")}",
            cancellationToken);
    }

    //მონაცემთა ბაზაში არსებული სტატისტიკების დაანგარიშება
    public async Task<Option<Err[]>> UpdateStatistics(string databaseName, CancellationToken cancellationToken)
    {
        return await PostAsync(
            $"databases/updatestatistics{(string.IsNullOrWhiteSpace(databaseName) ? "" : $"/{databaseName}")}",
            cancellationToken);
    }

    public Task<Option<Err[]>> SetDefaultFolders(string defBackupFolder, string defDataFolder, string defLogFolder,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public static async Task<DatabaseApiClient?> Create(ILogger logger, ApiClientSettings? apiClientSettings,
        IMessagesDataManager? messagesDataManager, string? userName, CancellationToken cancellationToken)
    {
        if (apiClientSettings is null || string.IsNullOrWhiteSpace(apiClientSettings.Server))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName, "cannot create DatabaseApiClient", cancellationToken);
            logger.LogError("cannot create DatabaseApiClient");
            return null;
        }

        ApiClientSettingsDomain apiClientSettingsDomain = new(apiClientSettings.Server, apiClientSettings.ApiKey,
            apiClientSettings.WithMessaging);
        // ReSharper disable once DisposableConstructor
        return new DatabaseApiClient(logger, apiClientSettingsDomain);
    }
}