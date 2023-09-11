using DbTools.Models;
using LibDatabaseParameters;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WebAgentProjectsApiContracts.V1.Responses;

namespace DatabasesManagement;

public interface IDatabaseApiClient
{
    //დამზადდეს ბაზის სარეზერვო ასლი სერვერის მხარეს.
    //ასევე ამ მეთოდის ამოცანაა უზრუნველყოს ბექაპის ჩამოსაქაჩად ხელმისაწვდომ ადგილას მოხვედრა
    Task<BackupFileParameters?> CreateBackup(DatabaseBackupParametersDomain databaseBackupParametersModel,
        string backupBaseName, CancellationToken cancellationToken);

    //მონაცემთა ბაზების სიის მიღება სერვერიდან
    Task<List<DatabaseInfoModel>> GetDatabaseNames(CancellationToken cancellationToken);

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, იმის დასადგენად,
    //მიზნის ბაზა უკვე არსებობს თუ არა, რომ არ მოხდეს ამ ბაზის ისე წაშლა ახლით,
    //რომ არსებულის გადანახვა არ მოხდეს.
    // ReSharper disable once UnusedMember.Global
    Task<bool> IsDatabaseExists(string databaseName, CancellationToken cancellationToken);

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, დაკოპირებული ბაზის აღსადგენად,
    // ReSharper disable once UnusedMember.Global
    Task<bool> RestoreDatabaseFromBackup(BackupFileParameters backupFileParameters, string databaseName,
        CancellationToken cancellationToken, string? restoreFromFolderPath = null);

    //შემოწმდეს არსებული ბაზის მდგომარეობა და საჭიროების შემთხვევაში გამოასწოროს ბაზა
    Task<bool> CheckRepairDatabase(string databaseName, CancellationToken cancellationToken);

    //სერვერის მხარეს მონაცემთა ბაზაში ბრძანების გაშვება
    Task<bool> ExecuteCommand(string executeQueryCommand, CancellationToken cancellationToken,
        string? databaseName = null);

    //მონაცემთა ბაზების სერვერის შესახებ ზოგადი ინფორმაციის მიღება
    Task<DbServerInfo?> GetDatabaseServerInfo(CancellationToken cancellationToken);

    //გამოიყენება იმის დასადგენად მონაცემთა ბაზის სერვერი ლოკალურია თუ არა
    //DatabaseApiClients-ში არ არის რეალიზებული, რადგან ითვლება,
    //რომ apiClient-ით მხოლოდ მოშორებულ სერვერს ვუკავშირდებით
    bool IsServerLocal();

    //მონაცემთა ბაზაში არსებული პროცედურების რეკომპილირება
    Task<bool> RecompileProcedures(string databaseName, CancellationToken cancellationToken);

    Task<bool> TestConnection(string? databaseName, CancellationToken cancellationToken);

    //მონაცემთა ბაზაში არსებული სტატისტიკების დაანგარიშება
    Task<bool> UpdateStatistics(string databaseName, CancellationToken cancellationToken);
}