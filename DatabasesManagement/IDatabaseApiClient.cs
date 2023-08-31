using DbTools.Models;
using LibDatabaseParameters;
using OneOf;
using System.Collections.Generic;
using System.Threading.Tasks;
using SystemToolsShared;
using WebAgentProjectsApiContracts.V1.Responses;

namespace DatabasesManagement;

public interface IDatabaseApiClient
{
    //შემოწმდეს არსებული ბაზის მდგომარეობა და საჭიროების შემთხვევაში გამოასწოროს ბაზა
    Task<bool> CheckRepairDatabase(string databaseName);

    //დამზადდეს ბაზის სარეზერვო ასლი სერვერის მხარეს.
    //ასევე ამ მეთოდის ამოცანაა უზრუნველყოს ბექაპის ჩამოსაქაჩად ხელმისაწვდომ ადგილას მოხვედრა
    Task<BackupFileParameters?> CreateBackup(DatabaseBackupParametersDomain databaseBackupParametersModel,
        string backupBaseName);

    //სერვერის მხარეს მონაცემთა ბაზაში ბრძანების გაშვება
    Task<bool> ExecuteCommand(string executeQueryCommand, string? databaseName = null);

    //მონაცემთა ბაზების სერვერის შესახებ ზოგადი ინფორმაციის მიღება
    Task<DbServerInfo?> GetDatabaseServerInfo();

    //მონაცემთა ბაზების სიის მიღება სერვერიდან
    Task<List<DatabaseInfoModel>> GetDatabaseNames();

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, იმის დასადგენად,
    //მიზნის ბაზა უკვე არსებობს თუ არა, რომ არ მოხდეს ამ ბაზის ისე წაშლა ახლით,
    //რომ არსებულის გადანახვა არ მოხდეს.
    // ReSharper disable once UnusedMember.Global
    Task<bool> IsDatabaseExists(string databaseName);

    //გამოიყენება იმის დასადგენად მონაცემთა ბაზის სერვერი ლოკალურია თუ არა
    //DatabaseApiClients-ში არ არის რეალიზებული, რადგან ითვლება,
    //რომ apiClient-ით მხოლოდ მოშორებულ სერვერს ვუკავშირდებით
    bool IsServerLocal();

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, დაკოპირებული ბაზის აღსადგენად,
    // ReSharper disable once UnusedMember.Global
    Task<bool> RestoreDatabaseFromBackup(BackupFileParameters backupFileParameters, string databaseName,
        string? restoreFromFolderPath = null);

    //მონაცემთა ბაზაში არსებული პროცედურების რეკომპილირება
    Task<bool> RecompileProcedures(string databaseName);

    Task<bool> TestConnection(string? databaseName);

    //მონაცემთა ბაზაში არსებული სტატისტიკების დაანგარიშება
    Task<bool> UpdateStatistics(string databaseName);
}