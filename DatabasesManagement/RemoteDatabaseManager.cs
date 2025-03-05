using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DbTools;
using DbTools.Errors;
using DbTools.Models;
using LanguageExt;
using LibDatabaseParameters;
using Microsoft.Extensions.Logging;
using OneOf;
using SystemToolsShared.Errors;
using WebAgentDatabasesApiContracts;
using WebAgentDatabasesApiContracts.V1.Responses;

namespace DatabasesManagement;

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
    public async ValueTask<OneOf<BackupFileParameters, IEnumerable<Err>>> CreateBackup(
        DatabaseBackupParametersDomain databaseBackupParameters, string backupBaseName, string dbServerFoldersSetName,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(backupBaseName))
            return await ApiClient.CreateBackup(databaseBackupParameters, backupBaseName, dbServerFoldersSetName,
                cancellationToken);

        _logger.LogError(DbClientErrors.DatabaseNameIsNotSpecifiedForBackup.ErrorMessage);
        return new[] { DbClientErrors.DatabaseNameIsNotSpecifiedForBackup };
    }

    //მონაცემთა ბაზების სიის მიღება სერვერიდან
    public Task<OneOf<List<DatabaseInfoModel>, IEnumerable<Err>>> GetDatabaseNames(
        CancellationToken cancellationToken = default)
    {
        return ApiClient.GetDatabaseNames(cancellationToken);
    }

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, იმის დასადგენად,
    //მიზნის ბაზა უკვე არსებობს თუ არა, რომ არ მოხდეს ამ ბაზის ისე წაშლა ახლით,
    //რომ არსებულის გადანახვა არ მოხდეს.
    public Task<OneOf<bool, IEnumerable<Err>>> IsDatabaseExists(string databaseName,
        CancellationToken cancellationToken = default)
    {
        return ApiClient.IsDatabaseExists(databaseName, cancellationToken);
    }

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, დაკოპირებული ბაზის აღსადგენად,
    public Task<Option<IEnumerable<Err>>> RestoreDatabaseFromBackup(BackupFileParameters backupFileParameters,
        string databaseName, string dbServerFoldersSetName, EDatabaseRecoveryModel databaseRecoveryModel,
        string? restoreFromFolderPath = null, CancellationToken cancellationToken = default)
    {
        return ApiClient.RestoreDatabaseFromBackup(backupFileParameters.Prefix, backupFileParameters.Suffix,
            backupFileParameters.Name, backupFileParameters.DateMask, databaseName, dbServerFoldersSetName,
            databaseRecoveryModel, cancellationToken);
    }

    //შემოწმდეს არსებული ბაზის მდგომარეობა და საჭიროების შემთხვევაში გამოასწოროს ბაზა
    public ValueTask<Option<IEnumerable<Err>>> CheckRepairDatabase(string databaseName,
        CancellationToken cancellationToken = default)
    {
        return ApiClient.CheckRepairDatabase(databaseName, cancellationToken);
    }

    public Task<OneOf<DbServerInfo, IEnumerable<Err>>> GetDatabaseServerInfo(
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<OneOf<bool, IEnumerable<Err>>> IsServerLocal(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(false);
    }

    //მონაცემთა ბაზაში არსებული პროცედურების რეკომპილირება
    public ValueTask<Option<IEnumerable<Err>>> RecompileProcedures(string databaseName,
        CancellationToken cancellationToken = default)
    {
        return ApiClient.RecompileProcedures(databaseName, cancellationToken);
    }

    public Task<Option<IEnumerable<Err>>> TestConnection(string? databaseName,
        CancellationToken cancellationToken = default)
    {
        return ApiClient.TestConnection(databaseName, cancellationToken);
    }

    //მონაცემთა ბაზაში არსებული სტატისტიკების დაანგარიშება
    public ValueTask<Option<IEnumerable<Err>>> UpdateStatistics(string databaseName,
        CancellationToken cancellationToken = default)
    {
        return ApiClient.UpdateStatistics(databaseName, cancellationToken);
    }

    public Task<Option<IEnumerable<Err>>> SetDefaultFolders(string defBackupFolder, string defDataFolder,
        string defLogFolder, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    //public Task<OneOf<List<string>, IEnumerable<Err>>> GetDatabaseConnectionNames(CancellationToken cancellationToken)
    //{
    //    return _databaseApiClient.GetDatabaseConnectionNames(cancellationToken);
    //}

    public Task<OneOf<List<string>, IEnumerable<Err>>> GetDatabaseFoldersSetNames(CancellationToken cancellationToken)
    {
        return ApiClient.GetDatabaseFoldersSetNames(cancellationToken);
    }

    public ValueTask<Option<IEnumerable<Err>>> ChangeDatabaseRecoveryModel(string databaseName, EDatabaseRecoveryModel databaseRecoveryModel,
        CancellationToken cancellationToken)
    {
        return ApiClient.ChangeDatabaseRecoveryModel(databaseName, databaseRecoveryModel, cancellationToken);
    }

    //სერვერის მხარეს მონაცემთა ბაზაში ბრძანების გაშვება
    public ValueTask<Option<IEnumerable<Err>>> ExecuteCommand(string executeQueryCommand, string? databaseName = null,
        CancellationToken cancellationToken = default)
    {
        return ApiClient.ExecuteCommand(executeQueryCommand, databaseName, cancellationToken);
    }
}