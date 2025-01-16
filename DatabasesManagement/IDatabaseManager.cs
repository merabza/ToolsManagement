using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DbTools.Models;
using LanguageExt;
using OneOf;
using SystemToolsShared.Errors;
using WebAgentDatabasesApiContracts.V1.Responses;

namespace DatabasesManagement;

public interface IDatabaseManager
{
    //დამზადდეს ბაზის სარეზერვო ასლი სერვერის მხარეს.
    //ასევე ამ მეთოდის ამოცანაა უზრუნველყოს ბექაპის ჩამოსაქაჩად ხელმისაწვდომ ადგილას მოხვედრა
    ValueTask<OneOf<BackupFileParameters, Err[]>> CreateBackup(string backupBaseName, string dbServerFoldersSetName,
        CancellationToken cancellationToken = default);

    //მონაცემთა ბაზების სიის მიღება სერვერიდან
    Task<OneOf<List<DatabaseInfoModel>, Err[]>> GetDatabaseNames(CancellationToken cancellationToken = default);

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, იმის დასადგენად,
    //მიზნის ბაზა უკვე არსებობს თუ არა, რომ არ მოხდეს ამ ბაზის ისე წაშლა ახლით,
    //რომ არსებულის გადანახვა არ მოხდეს.
    // ReSharper disable once UnusedMember.Global
    Task<OneOf<bool, Err[]>> IsDatabaseExists(string databaseName, CancellationToken cancellationToken = default);

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, დაკოპირებული ბაზის აღსადგენად,
    // ReSharper disable once UnusedMember.Global
    Task<Option<Err[]>> RestoreDatabaseFromBackup(BackupFileParameters backupFileParameters, string databaseName,
        string dbServerFoldersSetName, string? restoreFromFolderPath = null,
        CancellationToken cancellationToken = default);

    //შემოწმდეს არსებული ბაზის მდგომარეობა და საჭიროების შემთხვევაში გამოასწოროს ბაზა
    ValueTask<Option<Err[]>> CheckRepairDatabase(string databaseName, CancellationToken cancellationToken = default);

    //სერვერის მხარეს მონაცემთა ბაზაში ბრძანების გაშვება
    ValueTask<Option<Err[]>> ExecuteCommand(string executeQueryCommand, string? databaseName = null,
        CancellationToken cancellationToken = default);

    //მონაცემთა ბაზების სერვერის შესახებ ზოგადი ინფორმაციის მიღება
    //გამოიყენება ApAgent-ში
    Task<OneOf<DbServerInfo, Err[]>> GetDatabaseServerInfo(CancellationToken cancellationToken = default);

    //გამოიყენება იმის დასადგენად მონაცემთა ბაზის სერვერი ლოკალურია თუ არა
    //DatabaseApiClients-ში არ არის რეალიზებული, რადგან ითვლება,
    //რომ apiClient-ით მხოლოდ მოშორებულ სერვერს ვუკავშირდებით
    //გამოიყენება ApAgent-ში
    Task<OneOf<bool, Err[]>> IsServerLocal(CancellationToken cancellationToken = default);

    //მონაცემთა ბაზაში არსებული პროცედურების რეკომპილირება
    ValueTask<Option<Err[]>> RecompileProcedures(string databaseName, CancellationToken cancellationToken = default);

    Task<Option<Err[]>> TestConnection(string? databaseName, CancellationToken cancellationToken = default);

    //მონაცემთა ბაზაში არსებული სტატისტიკების დაანგარიშება
    ValueTask<Option<Err[]>> UpdateStatistics(string databaseName, CancellationToken cancellationToken = default);

    Task<Option<Err[]>> SetDefaultFolders(string defBackupFolder, string defDataFolder, string defLogFolder,
        CancellationToken cancellationToken = default);

    //Task<OneOf<List<string>, Err[]>> GetDatabaseConnectionNames(CancellationToken cancellationToken = default);

    Task<OneOf<Dictionary<string, DatabaseFoldersSet>, Err[]>> GetDatabaseFoldersSets(
        CancellationToken cancellationToken);

}