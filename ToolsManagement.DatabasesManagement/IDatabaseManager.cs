using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DatabaseTools.DbTools;
using DatabaseTools.DbTools.Models;
using LanguageExt;
using OneOf;
using ParametersManagement.LibDatabaseParameters;
using SystemTools.SystemToolsShared.Errors;
using WebAgentContracts.WebAgentDatabasesApiContracts.V1.Responses;

namespace ToolsManagement.DatabasesManagement;

public interface IDatabaseManager
{
    //დამზადდეს ბაზის სარეზერვო ასლი სერვერის მხარეს.
    //ასევე ამ მეთოდის ამოცანაა უზრუნველყოს ბექაპის ჩამოსაქაჩად ხელმისაწვდომ ადგილას მოხვედრა
    ValueTask<OneOf<BackupFileParameters, Error[]>> CreateBackup(
        DatabaseBackupParametersDomain databaseBackupParameters, string backupBaseName, string dbServerFoldersSetName,
        CancellationToken cancellationToken = default);

    //ValueTask<OneOf<BackupFileParameters, Err[]>> CreateBackup(string backupBaseName,
    //    string dbServerFoldersSetName, CancellationToken cancellationToken = default);

    //მონაცემთა ბაზების სიის მიღება სერვერიდან
    Task<OneOf<List<DatabaseInfoModel>, Error[]>> GetDatabaseNames(CancellationToken cancellationToken = default);

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, იმის დასადგენად,
    //მიზნის ბაზა უკვე არსებობს თუ არა, რომ არ მოხდეს ამ ბაზის ისე წაშლა ახლით,
    //რომ არსებულის გადანახვა არ მოხდეს.
    // ReSharper disable once UnusedMember.Global
    Task<OneOf<bool, Error[]>> IsDatabaseExists(string databaseName, CancellationToken cancellationToken = default);

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, დაკოპირებული ბაზის აღსადგენად,
    // ReSharper disable once UnusedMember.Global
    Task<Option<Error[]>> RestoreDatabaseFromBackup(BackupFileParameters backupFileParameters, string databaseName,
        string dbServerFoldersSetName, EDatabaseRecoveryModel databaseRecoveryModel,
        string? restoreFromFolderPath = null, CancellationToken cancellationToken = default);

    //შემოწმდეს არსებული ბაზის მდგომარეობა და საჭიროების შემთხვევაში გამოასწოროს ბაზა
    ValueTask<Option<Error[]>> CheckRepairDatabase(string databaseName, CancellationToken cancellationToken = default);

    //სერვერის მხარეს მონაცემთა ბაზაში ბრძანების გაშვება
    ValueTask<Option<Error[]>> ExecuteCommand(string executeQueryCommand, string? databaseName = null,
        CancellationToken cancellationToken = default);

    //მონაცემთა ბაზების სერვერის შესახებ ზოგადი ინფორმაციის მიღება
    //გამოიყენება ApAgent-ში
    Task<OneOf<DbServerInfo, Error[]>> GetDatabaseServerInfo(CancellationToken cancellationToken = default);

    //გამოიყენება იმის დასადგენად მონაცემთა ბაზის სერვერი ლოკალურია თუ არა
    //DatabaseApiClients-ში არ არის რეალიზებული, რადგან ითვლება,
    //რომ apiClient-ით მხოლოდ მოშორებულ სერვერს ვუკავშირდებით
    //გამოიყენება ApAgent-ში
    Task<OneOf<bool, Error[]>> IsServerLocal(CancellationToken cancellationToken = default);

    //მონაცემთა ბაზაში არსებული პროცედურების რეკომპილირება
    ValueTask<Option<Error[]>> RecompileProcedures(string databaseName, CancellationToken cancellationToken = default);

    Task<Option<Error[]>> TestConnection(string? databaseName, CancellationToken cancellationToken = default);

    //მონაცემთა ბაზაში არსებული სტატისტიკების დაანგარიშება
    ValueTask<Option<Error[]>> UpdateStatistics(string databaseName, CancellationToken cancellationToken = default);

    Task<Option<Error[]>> SetDefaultFolders(string defBackupFolder, string defDataFolder, string defLogFolder,
        CancellationToken cancellationToken = default);

    //Task<OneOf<List<string>, Err[]>> GetDatabaseConnectionNames(CancellationToken cancellationToken = default);

    Task<OneOf<List<string>, Error[]>> GetDatabaseFoldersSetNames(CancellationToken cancellationToken);

    ValueTask<Option<Error[]>> ChangeDatabaseRecoveryModel(string databaseName,
        EDatabaseRecoveryModel databaseRecoveryModel, CancellationToken cancellationToken);
}
