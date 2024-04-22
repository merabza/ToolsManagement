using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DbTools.Models;
using LanguageExt;
using LibDatabaseParameters;
using OneOf;
using SystemToolsShared;
using WebAgentProjectsApiContracts.V1.Responses;

namespace DatabasesManagement;

public interface IDatabaseApiClient
{
    //დამზადდეს ბაზის სარეზერვო ასლი სერვერის მხარეს.
    //ასევე ამ მეთოდის ამოცანაა უზრუნველყოს ბექაპის ჩამოსაქაჩად ხელმისაწვდომ ადგილას მოხვედრა
    Task<OneOf<BackupFileParameters, Err[]>> CreateBackup(DatabaseBackupParametersDomain databaseBackupParametersModel,
        string backupBaseName, CancellationToken cancellationToken);

    //მონაცემთა ბაზების სიის მიღება სერვერიდან
    Task<OneOf<List<DatabaseInfoModel>, Err[]>> GetDatabaseNames(CancellationToken cancellationToken);

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, იმის დასადგენად,
    //მიზნის ბაზა უკვე არსებობს თუ არა, რომ არ მოხდეს ამ ბაზის ისე წაშლა ახლით,
    //რომ არსებულის გადანახვა არ მოხდეს.
    // ReSharper disable once UnusedMember.Global
    Task<OneOf<bool, Err[]>> IsDatabaseExists(string databaseName, CancellationToken cancellationToken);

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, დაკოპირებული ბაზის აღსადგენად,
    // ReSharper disable once UnusedMember.Global
    Task<Option<Err[]>> RestoreDatabaseFromBackup(BackupFileParameters backupFileParameters,
        string? destinationDbServerSideDataFolderPath, string? destinationDbServerSideLogFolderPath,
        string databaseName, CancellationToken cancellationToken, string? restoreFromFolderPath = null);

    //შემოწმდეს არსებული ბაზის მდგომარეობა და საჭიროების შემთხვევაში გამოასწოროს ბაზა
    Task<Option<Err[]>> CheckRepairDatabase(string databaseName, CancellationToken cancellationToken);

    //სერვერის მხარეს მონაცემთა ბაზაში ბრძანების გაშვება
    Task<Option<Err[]>> ExecuteCommand(string executeQueryCommand, CancellationToken cancellationToken,
        string? databaseName = null);

    //მონაცემთა ბაზების სერვერის შესახებ ზოგადი ინფორმაციის მიღება
    //გამოიყენება ApAgent-ში
    Task<OneOf<DbServerInfo, Err[]>> GetDatabaseServerInfo(CancellationToken cancellationToken);

    //გამოიყენება იმის დასადგენად მონაცემთა ბაზის სერვერი ლოკალურია თუ არა
    //DatabaseApiClients-ში არ არის რეალიზებული, რადგან ითვლება,
    //რომ apiClient-ით მხოლოდ მოშორებულ სერვერს ვუკავშირდებით
    //გამოიყენება ApAgent-ში
    Task<OneOf<bool, Err[]>> IsServerLocal(CancellationToken cancellationToken);
    //მონაცემთა ბაზაში არსებული პროცედურების რეკომპილირება
    Task<Option<Err[]>> RecompileProcedures(string databaseName, CancellationToken cancellationToken);
    Task<Option<Err[]>> TestConnection(string? databaseName, CancellationToken cancellationToken);
    //მონაცემთა ბაზაში არსებული სტატისტიკების დაანგარიშება
    Task<Option<Err[]>> UpdateStatistics(string databaseName, CancellationToken cancellationToken);
    Task<Option<Err[]>> SetDefaultFolders(string defBackupFolder, string defDataFolder, string defLogFolder,
        CancellationToken cancellationToken);
}