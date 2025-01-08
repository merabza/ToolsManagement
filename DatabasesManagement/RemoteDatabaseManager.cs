using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DbTools.Errors;
using DbTools.Models;
using LanguageExt;
using Microsoft.Extensions.Logging;
using OneOf;
using SystemToolsShared.Errors;
using WebAgentDatabasesApiContracts;
using WebAgentDatabasesApiContracts.V1.Responses;

namespace DatabasesManagement;

public sealed class RemoteDatabaseManager : IDatabaseManager
{
    private readonly DatabaseApiClient _databaseApiClient;
    private readonly ILogger _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public RemoteDatabaseManager(ILogger logger, DatabaseApiClient databaseApiClient)
    {
        _logger = logger;
        _databaseApiClient = databaseApiClient;
    }

    //დამზადდეს ბაზის სარეზერვო ასლი სერვერის მხარეს.
    //ასევე ამ მეთოდის ამოცანაა უზრუნველყოს ბექაპის ჩამოსაქაჩად ხელმისაწვდომ ადგილას მოხვედრა
    public async ValueTask<OneOf<BackupFileParameters, Err[]>> CreateBackup(string backupBaseName,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(backupBaseName))
            return await _databaseApiClient.CreateBackup(backupBaseName, cancellationToken);

        _logger.LogError(DbClientErrors.DatabaseNameIsNotSpecifiedForBackup.ErrorMessage);
        return new[] { DbClientErrors.DatabaseNameIsNotSpecifiedForBackup };
    }

    //მონაცემთა ბაზების სიის მიღება სერვერიდან
    public Task<OneOf<List<DatabaseInfoModel>, Err[]>> GetDatabaseNames(CancellationToken cancellationToken = default)
    {
        return _databaseApiClient.GetDatabaseNames(cancellationToken);
    }

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, იმის დასადგენად,
    //მიზნის ბაზა უკვე არსებობს თუ არა, რომ არ მოხდეს ამ ბაზის ისე წაშლა ახლით,
    //რომ არსებულის გადანახვა არ მოხდეს.
    public Task<OneOf<bool, Err[]>> IsDatabaseExists(string databaseName, CancellationToken cancellationToken = default)
    {
        return _databaseApiClient.IsDatabaseExists(databaseName, cancellationToken);
    }

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, დაკოპირებული ბაზის აღსადგენად,
    public Task<Option<Err[]>> RestoreDatabaseFromBackup(BackupFileParameters backupFileParameters,
        //string? destinationDbServerSideDataFolderPath, string? destinationDbServerSideLogFolderPath,
        string databaseName, string? restoreFromFolderPath = null, CancellationToken cancellationToken = default)
    {
        return _databaseApiClient.RestoreDatabaseFromBackup(backupFileParameters.Prefix, backupFileParameters.Suffix,
            backupFileParameters.Name, backupFileParameters.DateMask,
            //destinationDbServerSideDataFolderPath,
            //destinationDbServerSideLogFolderPath, 
            databaseName, cancellationToken);
    }

    //შემოწმდეს არსებული ბაზის მდგომარეობა და საჭიროების შემთხვევაში გამოასწოროს ბაზა
    public ValueTask<Option<Err[]>> CheckRepairDatabase(string databaseName,
        CancellationToken cancellationToken = default)
    {
        return _databaseApiClient.CheckRepairDatabase(databaseName, cancellationToken);
    }

    public Task<OneOf<DbServerInfo, Err[]>> GetDatabaseServerInfo(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<OneOf<bool, Err[]>> IsServerLocal(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(false);
    }

    //მონაცემთა ბაზაში არსებული პროცედურების რეკომპილირება
    public ValueTask<Option<Err[]>> RecompileProcedures(string databaseName,
        CancellationToken cancellationToken = default)
    {
        return _databaseApiClient.RecompileProcedures(databaseName, cancellationToken);
    }

    public Task<Option<Err[]>> TestConnection(string? databaseName, CancellationToken cancellationToken = default)
    {
        return _databaseApiClient.TestConnection(databaseName, cancellationToken);
    }

    //მონაცემთა ბაზაში არსებული სტატისტიკების დაანგარიშება
    public ValueTask<Option<Err[]>> UpdateStatistics(string databaseName, CancellationToken cancellationToken = default)
    {
        return _databaseApiClient.UpdateStatistics(databaseName, cancellationToken);
    }

    public Task<Option<Err[]>> SetDefaultFolders(string defBackupFolder, string defDataFolder, string defLogFolder,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<OneOf<Dictionary<string, DatabaseFoldersSet>, Err[]>> GetDatabaseFoldersSets(
        CancellationToken cancellationToken)
    {
        return _databaseApiClient.GetDatabaseFoldersSets(cancellationToken);
    }

    //სერვერის მხარეს მონაცემთა ბაზაში ბრძანების გაშვება
    public ValueTask<Option<Err[]>> ExecuteCommand(string executeQueryCommand, string? databaseName = null,
        CancellationToken cancellationToken = default)
    {
        return _databaseApiClient.ExecuteCommand(executeQueryCommand, databaseName, cancellationToken);
    }
}