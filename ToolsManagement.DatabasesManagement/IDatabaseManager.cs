using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DatabaseTools.DbTools;
using DatabaseTools.DbTools.Models;
using ParametersManagement.LibDatabaseParameters;
using SystemTools.SharedKernel;
using WebAgentContracts.WebAgentDatabasesApiContracts.V1.Responses;

namespace ToolsManagement.DatabasesManagement;

public interface IDatabaseManager
{
    //დამზადდეს ბაზის სარეზერვო ასლი სერვერის მხარეს.
    //ასევე ამ მეთოდის ამოცანაა უზრუნველყოს ბექაპის ჩამოსაქაჩად ხელმისაწვდომ ადგილას მოხვედრა
    ValueTask<Result<BackupFileParameters>> CreateBackup(DatabaseBackupParametersDomain databaseBackupParameters,
        string backupBaseName, string dbServerFoldersSetName, CancellationToken cancellationToken = default);

    //ValueTask<Result<BackupFileParameters>> CreateBackup(string backupBaseName,
    //    string dbServerFoldersSetName, CancellationToken cancellationToken = default);

    //მონაცემთა ბაზების სიის მიღება სერვერიდან
    Task<Result<List<DatabaseInfoModel>>> GetDatabaseNames(CancellationToken cancellationToken = default);

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, იმის დასადგენად,
    //მიზნის ბაზა უკვე არსებობს თუ არა, რომ არ მოხდეს ამ ბაზის ისე წაშლა ახლით,
    //რომ არსებულის გადანახვა არ მოხდეს.
    // ReSharper disable once UnusedMember.Global
    Task<Result<bool>> IsDatabaseExists(string databaseName, CancellationToken cancellationToken = default);

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, დაკოპირებული ბაზის აღსადგენად,
    // ReSharper disable once UnusedMember.Global
    Task<Result> RestoreDatabaseFromBackup(BackupFileParameters backupFileParameters, string databaseName,
        string dbServerFoldersSetName, EDatabaseRecoveryModel databaseRecoveryModel,
        string? restoreFromFolderPath = null, CancellationToken cancellationToken = default);

    //შემოწმდეს არსებული ბაზის მდგომარეობა და საჭიროების შემთხვევაში გამოასწოროს ბაზა
    ValueTask<Result> CheckRepairDatabase(string databaseName, CancellationToken cancellationToken = default);

    //სერვერის მხარეს მონაცემთა ბაზაში ბრძანების გაშვება
    ValueTask<Result> ExecuteCommand(string executeQueryCommand, string? databaseName = null,
        CancellationToken cancellationToken = default);

    //მონაცემთა ბაზების სერვერის შესახებ ზოგადი ინფორმაციის მიღება
    //გამოიყენება Replicator-ში
    Task<Result<DbServerInfo>> GetDatabaseServerInfo(CancellationToken cancellationToken = default);

    //გამოიყენება იმის დასადგენად მონაცემთა ბაზის სერვერი ლოკალურია თუ არა
    //DatabaseApiClients-ში არ არის რეალიზებული, რადგან ითვლება,
    //რომ apiClient-ით მხოლოდ მოშორებულ სერვერს ვუკავშირდებით
    //გამოიყენება Replicator-ში
    Task<Result<bool>> IsServerLocal(CancellationToken cancellationToken = default);

    //მონაცემთა ბაზაში არსებული პროცედურების რეკომპილირება
    ValueTask<Result> RecompileProcedures(string databaseName, CancellationToken cancellationToken = default);

    Task<Result> TestConnection(string? databaseName, CancellationToken cancellationToken = default);

    //მონაცემთა ბაზაში არსებული სტატისტიკების დაანგარიშება
    ValueTask<Result> UpdateStatistics(string databaseName, CancellationToken cancellationToken = default);

    Task<Result> SetDefaultFolders(string defBackupFolder, string defDataFolder, string defLogFolder,
        CancellationToken cancellationToken = default);

    //Task<Result<List<string>>> GetDatabaseConnectionNames(CancellationToken cancellationToken = default);

    Task<Result<List<string>>> GetDatabaseFoldersSetNames(CancellationToken cancellationToken);

    ValueTask<Result> ChangeDatabaseRecoveryModel(string databaseName, EDatabaseRecoveryModel databaseRecoveryModel,
        CancellationToken cancellationToken);
}
