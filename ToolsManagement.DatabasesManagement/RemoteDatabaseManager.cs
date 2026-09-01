using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DatabaseTools.DbTools;
using DatabaseTools.DbTools.Errors;
using DatabaseTools.DbTools.Models;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibDatabaseParameters;
using SystemTools.SharedKernel;
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
    public async ValueTask<Result<BackupFileParameters>> CreateBackup(
        DatabaseBackupParametersDomain databaseBackupParameters, string backupBaseName, string dbServerFoldersSetName,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(backupBaseName))
        {
            return await ApiClient.CreateBackup(databaseBackupParameters, backupBaseName, dbServerFoldersSetName,
                cancellationToken);
        }

#pragma warning disable CA2254
        _logger.LogError(DbClientErrors.DatabaseNameIsNotSpecifiedForBackup.Description);
#pragma warning restore CA2254
        return DbClientErrors.DatabaseNameIsNotSpecifiedForBackup;
    }

    //მონაცემთა ბაზების სიის მიღება სერვერიდან
    public Task<Result<List<DatabaseInfoModel>>> GetDatabaseNames(CancellationToken cancellationToken = default)
    {
        return ApiClient.GetDatabaseNames(cancellationToken);
    }

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, იმის დასადგენად,
    //მიზნის ბაზა უკვე არსებობს თუ არა, რომ არ მოხდეს ამ ბაზის ისე წაშლა ახლით,
    //რომ არსებულის გადანახვა არ მოხდეს.
    public Task<Result<bool>> IsDatabaseExists(string databaseName, CancellationToken cancellationToken = default)
    {
        return ApiClient.IsDatabaseExists(databaseName, cancellationToken);
    }

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, დაკოპირებული ბაზის აღსადგენად,
    public Task<Result> RestoreDatabaseFromBackup(BackupFileParameters backupFileParameters, string databaseName,
        string dbServerFoldersSetName, EDatabaseRecoveryModel databaseRecoveryModel,
        string? restoreFromFolderPath = null, CancellationToken cancellationToken = default)
    {
        return ApiClient.RestoreDatabaseFromBackup(backupFileParameters.Prefix, backupFileParameters.Suffix,
            backupFileParameters.Name, backupFileParameters.DateMask, databaseName, dbServerFoldersSetName,
            databaseRecoveryModel, cancellationToken);
    }

    //შემოწმდეს არსებული ბაზის მდგომარეობა და საჭიროების შემთხვევაში გამოასწოროს ბაზა
    public ValueTask<Result> CheckRepairDatabase(string databaseName, CancellationToken cancellationToken = default)
    {
        return ApiClient.CheckRepairDatabase(databaseName, cancellationToken);
    }

    public Task<Result<DbServerInfo>> GetDatabaseServerInfo(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<bool>> IsServerLocal(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(false);
    }

    //მონაცემთა ბაზაში არსებული პროცედურების რეკომპილირება
    public ValueTask<Result> RecompileProcedures(string databaseName, CancellationToken cancellationToken = default)
    {
        return ApiClient.RecompileProcedures(databaseName, cancellationToken);
    }

    public Task<Result> TestConnection(string? databaseName, CancellationToken cancellationToken = default)
    {
        return ApiClient.TestConnection(databaseName, cancellationToken);
    }

    //მონაცემთა ბაზაში არსებული სტატისტიკების დაანგარიშება
    public ValueTask<Result> UpdateStatistics(string databaseName, CancellationToken cancellationToken = default)
    {
        return ApiClient.UpdateStatistics(databaseName, cancellationToken);
    }

    public Task<Result> SetDefaultFolders(string defBackupFolder, string defDataFolder, string defLogFolder,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    //public Task<Result<List<string>>> GetDatabaseConnectionNames(CancellationToken cancellationToken)
    //{
    //    return _databaseApiClient.GetDatabaseConnectionNames(cancellationToken);
    //}

    public Task<Result<List<string>>> GetDatabaseFoldersSetNames(CancellationToken cancellationToken)
    {
        return ApiClient.GetDatabaseFoldersSetNames(cancellationToken);
    }

    public ValueTask<Result> ChangeDatabaseRecoveryModel(string databaseName,
        EDatabaseRecoveryModel databaseRecoveryModel, CancellationToken cancellationToken)
    {
        return ApiClient.ChangeDatabaseRecoveryModel(databaseName, databaseRecoveryModel, cancellationToken);
    }

    //სერვერის მხარეს მონაცემთა ბაზაში ბრძანების გაშვება
    public ValueTask<Result> ExecuteCommand(string executeQueryCommand, string? databaseName = null,
        CancellationToken cancellationToken = default)
    {
        return ApiClient.ExecuteCommand(executeQueryCommand, databaseName, cancellationToken);
    }
}
