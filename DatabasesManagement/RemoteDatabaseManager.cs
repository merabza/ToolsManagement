using DbTools.ErrorModels;
using DbTools.Models;
using LanguageExt;
using LibDatabaseParameters;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OneOf;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SystemToolsShared;
using WebAgentDatabasesApiContracts;
using WebAgentDatabasesApiContracts.V1.Requests;
using WebAgentDatabasesApiContracts.V1.Responses;

namespace DatabasesManagement;

public sealed class RemoteDatabaseManager : IDatabaseManager
{
    //public const string ApiName = "DatabaseApi";

    private readonly ILogger _logger;
    private readonly DatabaseApiClient _databaseApiClient;

    // ReSharper disable once ConvertToPrimaryConstructor
    public RemoteDatabaseManager(ILogger logger, DatabaseApiClient databaseApiClient)
        //: base(logger, httpClientFactory, apiClientSettingsDomain.Server, apiClientSettingsDomain.ApiKey, null, apiClientSettingsDomain.WithMessaging)
    {
        _logger = logger;
        _databaseApiClient = databaseApiClient;
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

        return await _databaseApiClient.CreateBackup(bodyJsonData, backupBaseName, cancellationToken);
    }

    //მონაცემთა ბაზების სიის მიღება სერვერიდან
    public async Task<OneOf<List<DatabaseInfoModel>, Err[]>> GetDatabaseNames(CancellationToken cancellationToken)
    {
        return await _databaseApiClient.GetDatabaseNames(cancellationToken);
    }

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, იმის დასადგენად,
    //მიზნის ბაზა უკვე არსებობს თუ არა, რომ არ მოხდეს ამ ბაზის ისე წაშლა ახლით,
    //რომ არსებულის გადანახვა არ მოხდეს.
    public async Task<OneOf<bool, Err[]>> IsDatabaseExists(string databaseName, CancellationToken cancellationToken)
    {
        return await _databaseApiClient.IsDatabaseExists(databaseName, cancellationToken);
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
        return await _databaseApiClient.RestoreDatabaseFromBackup(bodyJsonData, databaseName, cancellationToken);
    }

    //შემოწმდეს არსებული ბაზის მდგომარეობა და საჭიროების შემთხვევაში გამოასწოროს ბაზა
    public async Task<Option<Err[]>> CheckRepairDatabase(string databaseName, CancellationToken cancellationToken)
    {
        return await _databaseApiClient.CheckRepairDatabase(databaseName, cancellationToken);
    }

    //სერვერის მხარეს მონაცემთა ბაზაში ბრძანების გაშვება
    public async Task<Option<Err[]>> ExecuteCommand(string executeQueryCommand, CancellationToken cancellationToken,
        string? databaseName = null)
    {
        return await _databaseApiClient.ExecuteCommand(executeQueryCommand, cancellationToken, databaseName);
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
        return await _databaseApiClient.RecompileProcedures(databaseName, cancellationToken);
    }

    public async Task<Option<Err[]>> TestConnection(string? databaseName, CancellationToken cancellationToken)
    {
        return await _databaseApiClient.TestConnection(databaseName, cancellationToken);
    }

    //მონაცემთა ბაზაში არსებული სტატისტიკების დაანგარიშება
    public async Task<Option<Err[]>> UpdateStatistics(string databaseName, CancellationToken cancellationToken)
    {
        return await _databaseApiClient.UpdateStatistics(databaseName, cancellationToken);
    }

    public Task<Option<Err[]>> SetDefaultFolders(string defBackupFolder, string defDataFolder, string defLogFolder,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    //public static async Task<DatabaseApiClient?> Create(ILogger logger, IHttpClientFactory httpClientFactory,
    //    ApiClientSettings? apiClientSettings,
    //    IMessagesDataManager? messagesDataManager, string? userName, CancellationToken cancellationToken)
    //{
    //    if (apiClientSettings is null || string.IsNullOrWhiteSpace(apiClientSettings.Server))
    //    {
    //        if (messagesDataManager is not null)
    //            await messagesDataManager.SendMessage(userName, "cannot create DatabaseApiClient", cancellationToken);
    //        logger.LogError("cannot create DatabaseApiClient");
    //        return null;
    //    }

    //    ApiClientSettingsDomain apiClientSettingsDomain = new(apiClientSettings.Server, apiClientSettings.ApiKey,
    //        apiClientSettings.WithMessaging);
    //    // ReSharper disable once DisposableConstructor
    //    return new DatabaseApiClient(logger, httpClientFactory, apiClientSettingsDomain);
    //}
}