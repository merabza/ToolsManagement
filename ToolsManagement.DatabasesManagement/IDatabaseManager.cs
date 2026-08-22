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
    ValueTask<OneOf<BackupFileParameters, ErrorOmd[]>> CreateBackup(
        DatabaseBackupParametersDomain databaseBackupParameters, string backupBaseName, string dbServerFoldersSetName,
        CancellationToken cancellationToken = default);

    //ValueTask<OneOf<BackupFileParameters, ErrorOmd[]>> CreateBackup(string backupBaseName,
    //    string dbServerFoldersSetName, CancellationToken cancellationToken = default);

    //მონაცემთა ბაზების სიის მიღება სერვერიდან
    Task<OneOf<List<DatabaseInfoModel>, ErrorOmd[]>> GetDatabaseNames(CancellationToken cancellationToken = default);

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, იმის დასადგენად,
    //მიზნის ბაზა უკვე არსებობს თუ არა, რომ არ მოხდეს ამ ბაზის ისე წაშლა ახლით,
    //რომ არსებულის გადანახვა არ მოხდეს.
    // ReSharper disable once UnusedMember.Global
    Task<OneOf<bool, ErrorOmd[]>> IsDatabaseExists(string databaseName, CancellationToken cancellationToken = default);

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, დაკოპირებული ბაზის აღსადგენად,
    // ReSharper disable once UnusedMember.Global
    Task<Option<ErrorOmd[]>> RestoreDatabaseFromBackup(BackupFileParameters backupFileParameters, string databaseName,
        string dbServerFoldersSetName, EDatabaseRecoveryModel databaseRecoveryModel,
        string? restoreFromFolderPath = null, CancellationToken cancellationToken = default);

    //შემოწმდეს არსებული ბაზის მდგომარეობა და საჭიროების შემთხვევაში გამოასწოროს ბაზა
    ValueTask<Option<ErrorOmd[]>> CheckRepairDatabase(string databaseName,
        CancellationToken cancellationToken = default);

    //სერვერის მხარეს მონაცემთა ბაზაში ბრძანების გაშვება
    ValueTask<Option<ErrorOmd[]>> ExecuteCommand(string executeQueryCommand, string? databaseName = null,
        CancellationToken cancellationToken = default);

    //მონაცემთა ბაზების სერვერის შესახებ ზოგადი ინფორმაციის მიღება
    //გამოიყენება Replicator-ში
    Task<OneOf<DbServerInfo, ErrorOmd[]>> GetDatabaseServerInfo(CancellationToken cancellationToken = default);

    //გამოიყენება იმის დასადგენად მონაცემთა ბაზის სერვერი ლოკალურია თუ არა
    //DatabaseApiClients-ში არ არის რეალიზებული, რადგან ითვლება,
    //რომ apiClient-ით მხოლოდ მოშორებულ სერვერს ვუკავშირდებით
    //გამოიყენება Replicator-ში
    Task<OneOf<bool, ErrorOmd[]>> IsServerLocal(CancellationToken cancellationToken = default);

    //მონაცემთა ბაზაში არსებული პროცედურების რეკომპილირება
    ValueTask<Option<ErrorOmd[]>> RecompileProcedures(string databaseName,
        CancellationToken cancellationToken = default);

    Task<Option<ErrorOmd[]>> TestConnection(string? databaseName, CancellationToken cancellationToken = default);

    //მონაცემთა ბაზაში არსებული სტატისტიკების დაანგარიშება
    ValueTask<Option<ErrorOmd[]>> UpdateStatistics(string databaseName, CancellationToken cancellationToken = default);

    Task<Option<ErrorOmd[]>> SetDefaultFolders(string defBackupFolder, string defDataFolder, string defLogFolder,
        CancellationToken cancellationToken = default);

    //Task<OneOf<List<string>, ErrorOmd[]>> GetDatabaseConnectionNames(CancellationToken cancellationToken = default);

    Task<OneOf<List<string>, ErrorOmd[]>> GetDatabaseFoldersSetNames(CancellationToken cancellationToken);

    ValueTask<Option<ErrorOmd[]>> ChangeDatabaseRecoveryModel(string databaseName,
        EDatabaseRecoveryModel databaseRecoveryModel, CancellationToken cancellationToken);
}
