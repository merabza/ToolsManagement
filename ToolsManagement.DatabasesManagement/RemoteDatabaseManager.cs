using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DatabaseTools.DbTools;
using DatabaseTools.DbTools.Errors;
using DatabaseTools.DbTools.Models;
using LanguageExt;
using Microsoft.Extensions.Logging;
using OneOf;
using ParametersManagement.LibDatabaseParameters;
using SystemTools.SystemToolsShared.Errors;
using WebAgentContracts.WebAgentDatabasesApiContracts;
using WebAgentContracts.WebAgentDatabasesApiContracts.V1.Responses;

namespace ToolsManagement.DatabasesManagement;

public sealed class RemoteDatabaseManager : IDatabaseManager
{
    private readonly ILogger _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public RemoteDatabaseManager(ILogger logger, DatabaseApiClient databaseApiClient)
    {
        _logger = logger;
        ApiClient = databaseApiClient;
    }

    public DatabaseApiClient ApiClient { get; }

    //დამზადდეს ბაზის სარეზერვო ასლი სერვერის მხარეს.
    //ასევე ამ მეთოდის ამოცანაა უზრუნველყოს ბექაპის ჩამოსაქაჩად ხელმისაწვდომ ადგილას მოხვედრა
    public async ValueTask<OneOf<BackupFileParameters, ErrorOmd[]>> CreateBackup(
        DatabaseBackupParametersDomain databaseBackupParameters, string backupBaseName, string dbServerFoldersSetName,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(backupBaseName))
        {
            return await ApiClient.CreateBackup(databaseBackupParameters, backupBaseName, dbServerFoldersSetName,
                cancellationToken);
        }

#pragma warning disable CA2254
        _logger.LogError(DbClientErrors.DatabaseNameIsNotSpecifiedForBackup.Name);
#pragma warning restore CA2254
        return new[] { DbClientErrors.DatabaseNameIsNotSpecifiedForBackup };
    }

    //მონაცემთა ბაზების სიის მიღება სერვერიდან
    public Task<OneOf<List<DatabaseInfoModel>, ErrorOmd[]>> GetDatabaseNames(
        CancellationToken cancellationToken = default)
    {
        return ApiClient.GetDatabaseNames(cancellationToken);
    }

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, იმის დასადგენად,
    //მიზნის ბაზა უკვე არსებობს თუ არა, რომ არ მოხდეს ამ ბაზის ისე წაშლა ახლით,
    //რომ არსებულის გადანახვა არ მოხდეს.
    public Task<OneOf<bool, ErrorOmd[]>> IsDatabaseExists(string databaseName,
        CancellationToken cancellationToken = default)
    {
        return ApiClient.IsDatabaseExists(databaseName, cancellationToken);
    }

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, დაკოპირებული ბაზის აღსადგენად,
    public Task<Option<ErrorOmd[]>> RestoreDatabaseFromBackup(BackupFileParameters backupFileParameters,
        string databaseName, string dbServerFoldersSetName, EDatabaseRecoveryModel databaseRecoveryModel,
        string? restoreFromFolderPath = null, CancellationToken cancellationToken = default)
    {
        return ApiClient.RestoreDatabaseFromBackup(backupFileParameters.Prefix, backupFileParameters.Suffix,
            backupFileParameters.Name, backupFileParameters.DateMask, databaseName, dbServerFoldersSetName,
            databaseRecoveryModel, cancellationToken);
    }

    //შემოწმდეს არსებული ბაზის მდგომარეობა და საჭიროების შემთხვევაში გამოასწოროს ბაზა
    public ValueTask<Option<ErrorOmd[]>> CheckRepairDatabase(string databaseName,
        CancellationToken cancellationToken = default)
    {
        return ApiClient.CheckRepairDatabase(databaseName, cancellationToken);
    }

    public Task<OneOf<DbServerInfo, ErrorOmd[]>> GetDatabaseServerInfo(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<OneOf<bool, ErrorOmd[]>> IsServerLocal(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(false);
    }

    //მონაცემთა ბაზაში არსებული პროცედურების რეკომპილირება
    public ValueTask<Option<ErrorOmd[]>> RecompileProcedures(string databaseName,
        CancellationToken cancellationToken = default)
    {
        return ApiClient.RecompileProcedures(databaseName, cancellationToken);
    }

    public Task<Option<ErrorOmd[]>> TestConnection(string? databaseName, CancellationToken cancellationToken = default)
    {
        return ApiClient.TestConnection(databaseName, cancellationToken);
    }

    //მონაცემთა ბაზაში არსებული სტატისტიკების დაანგარიშება
    public ValueTask<Option<ErrorOmd[]>> UpdateStatistics(string databaseName,
        CancellationToken cancellationToken = default)
    {
        return ApiClient.UpdateStatistics(databaseName, cancellationToken);
    }

    public Task<Option<ErrorOmd[]>> SetDefaultFolders(string defBackupFolder, string defDataFolder, string defLogFolder,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    //public Task<OneOf<List<string>, ErrorOmd[]>> GetDatabaseConnectionNames(CancellationToken cancellationToken)
    //{
    //    return _databaseApiClient.GetDatabaseConnectionNames(cancellationToken);
    //}

    public Task<OneOf<List<string>, ErrorOmd[]>> GetDatabaseFoldersSetNames(CancellationToken cancellationToken)
    {
        return ApiClient.GetDatabaseFoldersSetNames(cancellationToken);
    }

    public ValueTask<Option<ErrorOmd[]>> ChangeDatabaseRecoveryModel(string databaseName,
        EDatabaseRecoveryModel databaseRecoveryModel, CancellationToken cancellationToken)
    {
        return ApiClient.ChangeDatabaseRecoveryModel(databaseName, databaseRecoveryModel, cancellationToken);
    }

    //სერვერის მხარეს მონაცემთა ბაზაში ბრძანების გაშვება
    public ValueTask<Option<ErrorOmd[]>> ExecuteCommand(string executeQueryCommand, string? databaseName = null,
        CancellationToken cancellationToken = default)
    {
        return ApiClient.ExecuteCommand(executeQueryCommand, databaseName, cancellationToken);
    }
}
