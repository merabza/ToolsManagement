//using System.Collections.Generic;
//using System.Threading.Tasks;
//using DbTools.Models;
//using LibDatabaseParameters;
//using Microsoft.Extensions.Logging;
//using OneOf;
//using SystemToolsShared;
//using WebAgentProjectsApiContracts.V1.Responses;

//namespace DatabasesManagement;

//public /*open*/ class DatabaseManagementClient
//{
//    protected readonly ILogger Logger;
//    protected readonly bool UseConsole;

//    protected DatabaseManagementClient(bool useConsole, ILogger logger)
//    {
//        UseConsole = useConsole;
//        Logger = logger;
//    }

//    //შემოწმდეს არსებული ბაზის მდგომარეობა და საჭიროების შემთხვევაში გამოასწოროს ბაზა
//    public virtual Task<bool> CheckRepairDatabase(string databaseName)
//    {
//        return Task.FromResult(false);
//    }

//    //დამზადდეს ბაზის სარეზერვო ასლი სერვერის მხარეს.
//    //ასევე ამ მეთოდის ამოცანაა უზრუნველყოს ბექაპის ჩამოსაქაჩად ხელმისაწვდომ ადგილას მოხვედრა
//    public virtual Task<BackupFileParameters?> CreateBackup(
//        DatabaseBackupParametersDomain databaseBackupParametersModel, string backupBaseName)
//    {
//        return Task.FromResult<BackupFileParameters?>(null);
//    }

//    //სერვერის მხარეს მონაცემთა ბაზაში ბრძანების გაშვება
//    public virtual Task<bool> ExecuteCommand(string executeQueryCommand, string? databaseName = null)
//    {
//        return Task.FromResult(false);
//    }

//    //მონაცემთა ბაზების სერვერის შესახებ ზოგადი ინფორმაციის მიღება
//    public virtual Task<DbServerInfo?> GetDatabaseServerInfo()
//    {
//        return Task.FromResult<DbServerInfo?>(null);
//    }

//    //მონაცემთა ბაზების სიის მიღება სერვერიდან
//    public virtual Task<List<DatabaseInfoModel>> GetDatabaseNames()
//    {
//        return Task.FromResult(new List<DatabaseInfoModel>());
//    }

//    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, იმის დასადგენად,
//    //მიზნის ბაზა უკვე არსებობს თუ არა, რომ არ მოხდეს ამ ბაზის ისე წაშლა ახლით,
//    //რომ არსებულის გადანახვა არ მოხდეს.
//    // ReSharper disable once UnusedMember.Global
//    public virtual async Task<OneOf<bool, IEnumerable<Err>>> IsDatabaseExists(string databaseName)
//    {
//        return await Task.FromResult(false);
//    }

//    //გამოიყენება იმის დასადგენად მონაცემთა ბაზის სერვერი ლოკალურია თუ არა
//    //DatabaseApiClients-ში არ არის რეალიზებული, რადგან ითვლება,
//    //რომ apiClient-ით მხოლოდ მოშორებულ სერვერს ვუკავშირდებით
//    public virtual bool IsServerLocal()
//    {
//        return false;
//    }


//    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, დაკოპირებული ბაზის აღსადგენად,
//    // ReSharper disable once UnusedMember.Global
//    public virtual Task<bool> RestoreDatabaseFromBackup(BackupFileParameters backupFileParameters, string databaseName,
//        string? restoreFromFolderPath = null)
//    {
//        return Task.FromResult(false);
//    }

//    //მონაცემთა ბაზაში არსებული პროცედურების რეკომპილირება
//    public virtual Task<bool> RecompileProcedures(string databaseName)
//    {
//        return Task.FromResult(false);
//    }

//    public virtual Task<bool> TestConnection(string? databaseName)
//    {
//        return Task.FromResult(false);
//    }

//    //მონაცემთა ბაზაში არსებული სტატისტიკების დაანგარიშება
//    public virtual Task<bool> UpdateStatistics(string databaseName)
//    {
//        return Task.FromResult(false);
//    }
//}

