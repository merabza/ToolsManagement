using DbTools.Models;
using LibDatabaseParameters;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebAgentProjectsApiContracts.V1.Responses;

namespace DatabasesManagement;

public interface IDatabaseApiClient
{
    //დამზადდეს ბაზის სარეზერვო ასლი სერვერის მხარეს.
    //ასევე ამ მეთოდის ამოცანაა უზრუნველყოს ბექაპის ჩამოსაქაჩად ხელმისაწვდომ ადგილას მოხვედრა
    Task<BackupFileParameters?> CreateBackup(DatabaseBackupParametersDomain databaseBackupParametersModel,
        string backupBaseName);

    //მონაცემთა ბაზების სიის მიღება სერვერიდან
    Task<List<DatabaseInfoModel>> GetDatabaseNames();

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, იმის დასადგენად,
    //მიზნის ბაზა უკვე არსებობს თუ არა, რომ არ მოხდეს ამ ბაზის ისე წაშლა ახლით,
    //რომ არსებულის გადანახვა არ მოხდეს.
    // ReSharper disable once UnusedMember.Global
    Task<bool> IsDatabaseExists(string databaseName);

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, დაკოპირებული ბაზის აღსადგენად,
    // ReSharper disable once UnusedMember.Global
    Task<bool> RestoreDatabaseFromBackup(BackupFileParameters backupFileParameters, string databaseName,
        string? restoreFromFolderPath = null);

    //შემოწმდეს არსებული ბაზის მდგომარეობა და საჭიროების შემთხვევაში გამოასწოროს ბაზა
    Task<bool> CheckRepairDatabase(string databaseName);

    //სერვერის მხარეს მონაცემთა ბაზაში ბრძანების გაშვება
    Task<bool> ExecuteCommand(string executeQueryCommand, string? databaseName = null);

    //მონაცემთა ბაზების სერვერის შესახებ ზოგადი ინფორმაციის მიღება
    Task<DbServerInfo?> GetDatabaseServerInfo();

    //გამოიყენება იმის დასადგენად მონაცემთა ბაზის სერვერი ლოკალურია თუ არა
    //DatabaseApiClients-ში არ არის რეალიზებული, რადგან ითვლება,
    //რომ apiClient-ით მხოლოდ მოშორებულ სერვერს ვუკავშირდებით
    bool IsServerLocal();

    //მონაცემთა ბაზაში არსებული პროცედურების რეკომპილირება
    Task<bool> RecompileProcedures(string databaseName);

    Task<bool> TestConnection(string? databaseName);

    //მონაცემთა ბაზაში არსებული სტატისტიკების დაანგარიშება
    Task<bool> UpdateStatistics(string databaseName);
}