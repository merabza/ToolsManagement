using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading;
using Installer.Domain;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SystemToolsShared;

namespace Installer.ServiceInstaller;

public /*open*/ class InstallerBase
{
    protected readonly ILogger Logger;
    public readonly string Runtime;
    protected readonly IMessagesDataManager? MessagesDataManager;
    protected readonly string? UserName;
    protected readonly bool UseConsole;

    protected InstallerBase(bool useConsole, ILogger logger, string runtime, IMessagesDataManager? messagesDataManager,
        string? userName)
    {
        Logger = logger;
        Runtime = runtime;
        MessagesDataManager = messagesDataManager;
        UserName = userName;
        UseConsole = useConsole;
    }

    private string GetServiceEnvName(string? serviceName, string environmentName)
    {
        return $"{serviceName}_{environmentName}";
    }


    public bool RunUpdateSettings(string projectName, string? serviceName, string environmentName,
        string appSettingsFileName, string appSettingsFileBody, string? filesUserName, string? filesUsersGroupName,
        string installFolder)
    {
        var projectInstallFullPath = CheckBeforeStartUpdate(projectName, installFolder);
        if (projectInstallFullPath == null)
            return false;

        //დავადგინოთ დაინსტალირებული პარამეტრების ფაილის სრული გზა
        var appSettingsFileFullPath = Path.Combine(projectInstallFullPath, appSettingsFileName);

        //დავადგინოთ ფაილსაცავიდან მიღებული ბოლო პარამეტრების ფაილის ვერსია.
        var latestAppSettingsVersion = GetParametersVersion(appSettingsFileBody);
        //თუ დაინსტალირებული ფაილი არსებობს, დავადგინოთ მისი ვერსია
        if (!string.IsNullOrWhiteSpace(latestAppSettingsVersion) && File.Exists(appSettingsFileFullPath))
        {
            //დავადგინოთ დაინსტალირებული პარამეტრების ფაილის ვერსია.
            var currentAppSettingsFileBody = File.ReadAllText(appSettingsFileFullPath);
            var currentAppSettingsVersion = GetParametersVersion(currentAppSettingsFileBody);
            //თუ ვერსიები ემთხვევა დაინსტალირება აღარ გრძელდება, რადგან ისედაც ბოლო ვერსია აყენია
            if (!string.IsNullOrWhiteSpace(currentAppSettingsVersion) &&
                latestAppSettingsVersion == currentAppSettingsVersion)
            {
                MessagesDataManager?.SendMessage(UserName,
                    "Parameters file is already in latest version and not needs update").Wait();
                Logger.LogWarning("Parameters file is already in latest version and not needs update");
                return true;
            }
        }

        //აქ თუ მოვედით, ან დაინსტალირებული ფაილი არ არსებობს, ან ახალი ფაილისა და დაინსტალირებული ფაილის ვერსიები არ ემთხვევა.
        //კიდევ აქ მოსვლის მიზეზი შეიძლება იყოს, ის რომ ფაილებში არასწორად არის, ან საერთოდ არ არის გაწერილი ვერსიები
        //ამ ბოლო შემთხვევაზე ყურადღებას არ ვამახვილებთ, იმისათვის, რომ შესაძლებელი იყოს ასეთი "არასწორი" პროგრამების პარამეტრები განახლდეს.

        var serviceEnvName = GetServiceEnvName(serviceName, environmentName);

        if (!string.IsNullOrWhiteSpace(serviceName))
        {
            //დავადგინოთ არსებობს თუ არა სერვისების სიაში სერვისი სახელით {projectName}
            var serviceExists = IsServiceExists(serviceEnvName);
            if (serviceExists)
            {
                MessagesDataManager?.SendMessage(UserName, $"Service {serviceEnvName} is exists").Wait();
                Logger.LogInformation("Service {serviceEnvName} is exists", serviceEnvName);
            }
            else
            {
                MessagesDataManager?.SendMessage(UserName, $"Service {serviceEnvName} does not exists").Wait();
                Logger.LogInformation("Service {serviceEnvName} does not exists", serviceEnvName);
            }

            if (!serviceExists)
            {
                //ეს არის პარამეტრების განახლების პროცესი, ამიტომ თუ პროგრამა სერვისია და ეს სერვისი არ არსებობს განახლება ვერ მოხდება
                //ასეთ შემთხვევაში უნდა გაეშვას უკვე მთლიანი პროგრამის განახლების პროცესი
                MessagesDataManager?.SendMessage(UserName,
                    $"Service {serviceEnvName} does not exists, cannot update settings file").Wait();
                Logger.LogError("Service {serviceEnvName} does not exists, cannot update settings file",
                    serviceEnvName);
                return false;
            }

            //თუ სერვისი გაშვებულია უკვე, გავაჩეროთ
            MessagesDataManager?.SendMessage(UserName, $"Try to stop Service {serviceEnvName}").Wait();
            Logger.LogInformation("Try to stop Service {serviceEnvName}", serviceEnvName);
            if (!Stop(serviceEnvName))
            {
                MessagesDataManager?.SendMessage(UserName, $"Service {serviceEnvName} does not stopped").Wait();
                Logger.LogError("Service {serviceEnvName} does not stopped", serviceEnvName);
                return false;
            }
        }
        else
        {
            if (IsProcessRunning(projectName))
            {
                //თუ სერვისი არ არის და პროგრამა მაინც გაშვებულია,
                //ასეთ შემთხვევაში პარამეტრების ფაილს ვერ გავაახლებთ,
                //რადგან გაშვებული პროგრამა ვერ მიხვდება, რომ ახალი პარამეტრები უნდა გამოიყენოს.
                //ასეთ შემთხვევაში ჯერ უნდა გაჩერდეს პროგრამა და მერე უნდა განახლდეს პარამეტრები.
                MessagesDataManager?.SendMessage(UserName, $"Process {projectName} is running and cannot be updated.")
                    .Wait();
                Logger.LogError("Process {projectName} is running and cannot be updated.", projectName);
                return false;
            }
        }

        //შევეცადოთ პარამეტრების ფაილის წაშლა
        var appSettingsFileDeletedSuccess = true;
        if (File.Exists(appSettingsFileFullPath))
        {
            appSettingsFileDeletedSuccess = false;
            MessagesDataManager?.SendMessage(UserName, $"File {appSettingsFileFullPath} is exists").Wait();
            Logger.LogInformation("File {appSettingsFileFullPath} is exists", appSettingsFileFullPath);

            var tryCount = 0;
            while (!appSettingsFileDeletedSuccess && tryCount < 10)
            {
                tryCount++;
                try
                {
                    MessagesDataManager
                        ?.SendMessage(UserName, $"Try to delete File {appSettingsFileFullPath} {tryCount}...").Wait();
                    Logger.LogInformation("Try to delete File {appSettingsFileFullPath} {tryCount}...",
                        appSettingsFileFullPath, tryCount);
                    File.Delete(appSettingsFileFullPath);
                    MessagesDataManager?.SendMessage(UserName, $"File {appSettingsFileFullPath} deleted successfully")
                        .Wait();
                    Logger.LogInformation("File {appSettingsFileFullPath} deleted successfully",
                        appSettingsFileFullPath);
                    appSettingsFileDeletedSuccess = true;
                }
                catch
                {
                    MessagesDataManager?.SendMessage(UserName,
                        $"File {appSettingsFileFullPath} could not deleted on try {tryCount}").Wait();
                    Logger.LogWarning("File {appSettingsFileFullPath} could not deleted on try {tryCount}",
                        appSettingsFileFullPath, tryCount);
                    MessagesDataManager?.SendMessage(UserName, "waiting for 3 seconds...").Wait();
                    Logger.LogInformation("waiting for 3 seconds...");
                    Thread.Sleep(3000);
                }
            }
        }

        if (!appSettingsFileDeletedSuccess)
        {
            //თუ მიმდინარე დაინსტალირებული პარამეტრების ფაილის წაშლა ვერ მოხერხდა,
            //მაშინ პარამეტრების ფაილის განახლების პროცესი წყდება
            MessagesDataManager?.SendMessage(UserName, $"File {appSettingsFileFullPath} can not Deleted").Wait();
            Logger.LogError("File {appSettingsFileFullPath} can not Deleted", appSettingsFileFullPath);
            return false;
        }


        //შეიქმნას პარამეტრების ფაილი არსებულ ინფორმაციაზე დაყრდნობით
        File.WriteAllText(appSettingsFileFullPath, appSettingsFileBody);
        //შეიცვალოს პარამეტრების ფაილზე უფლებები საჭიროების მიხედვით.
        if (!ChangeOneFileOwner(appSettingsFileFullPath, filesUserName, filesUsersGroupName))
        {
            MessagesDataManager?.SendMessage(UserName, $"File {appSettingsFileFullPath} owner can not be changed")
                .Wait();
            Logger.LogError("File {appSettingsFileFullPath} owner can not be changed", appSettingsFileFullPath);
            return false;
        }

        //თუ ეს სერვისი არ არის პროცესი დასრულებულია, თანაც წარმატებით
        if (!string.IsNullOrWhiteSpace(serviceName))
            return true;

        //თუ სერვისია, გავუშვათ ეს სერვისი და დავრწმუნდეთ, რომ გაეშვა.
        if (Start(serviceEnvName))
            return true;

        //თუ სერვისი არ გაეშვა, ვაბრუნებთ შეტყობინებას
        MessagesDataManager?.SendMessage(UserName, $"Service {projectName} can not started").Wait();
        Logger.LogError("Service {projectName} can not started", projectName);
        return false;
    }

    private static string? GetParametersVersion(string appSettingsFileBody)
    {
        var latestAppSetJObject = JObject.Parse(appSettingsFileBody);
        var latestAppSettingsVersion =
            latestAppSetJObject["VersionInfo"]?["AppSettingsVersion"]?.Value<string>();
        return latestAppSettingsVersion;
    }

    private string? CheckBeforeStartUpdate(string projectName, string installFolder)
    {
        if (!Directory.Exists(installFolder))
        {
            MessagesDataManager?.SendMessage(UserName, $"Installer install folder {installFolder} does not exists")
                .Wait();
            Logger.LogError("Installer install folder {installFolder} does not exists", installFolder);
            return null;
        }

        MessagesDataManager?.SendMessage(UserName, $"Installer install folder is {installFolder}").Wait();
        Logger.LogInformation("Installer install folder is {installFolder}", installFolder);

        var projectInstallFullPath = Path.Combine(installFolder, projectName);
        if (!Directory.Exists(projectInstallFullPath))
        {
            MessagesDataManager
                ?.SendMessage(UserName, $"Project install folder {projectInstallFullPath} does not exists").Wait();
            Logger.LogError("Project install folder {projectInstallFullPath} does not exists", projectInstallFullPath);
            return null;
        }

        MessagesDataManager?.SendMessage(UserName, $"Project install folder is {projectInstallFullPath}").Wait();
        Logger.LogInformation("Project install folder is {projectInstallFullPath}", projectInstallFullPath);
        return projectInstallFullPath;
    }

    public string? RunUpdateService(string archiveFileName, string projectName, string? serviceName,
        string environmentName, FileNameAndTextContent? appSettingsFile, string serviceUserName, string filesUserName,
        string filesUsersGroupName, string installWorkFolder, string installFolder)
    {
        //დავადგინოთ არსებობს თუ არა {_workFolder} სახელით ქვეფოლდერი სამუშაო ფოლდერში
        //და თუ არ არსებობს, შევქმნათ
        var checkedWorkFolder = FileStat.CreateFolderIfNotExists(installWorkFolder, UseConsole);
        if (checkedWorkFolder == null)
        {
            MessagesDataManager?.SendMessage(UserName, $"Installer work folder {installWorkFolder} does not created")
                .Wait();
            Logger.LogError("Installer work folder {installWorkFolder} does not created", installWorkFolder);
            return null;
        }

        var checkedInstallFolder = FileStat.CreateFolderIfNotExists(installFolder, UseConsole);
        if (checkedInstallFolder == null)
        {
            MessagesDataManager?.SendMessage(UserName, $"Installer install folder {installFolder} does not created")
                .Wait();
            Logger.LogError("Installer install folder {installFolder} does not created", installFolder);
            return null;
        }

        MessagesDataManager?.SendMessage(UserName, $"Installer install folder is {checkedInstallFolder}").Wait();
        Logger.LogInformation("Installer install folder is {checkedInstallFolder}", checkedInstallFolder);

        //გავშალოთ არქივი სამუშაო ფოლდერში, იმისათვის, რომ დავრწმუნდეთ,
        //რომ არქივი დაზიანებული არ არის და ყველა ფაილის გახსნა ხერხდება
        //ZipClassArchiver zipClassArchiver = new ZipClassArchiver(_logger, outputFolderPath, zipFileFullName);
        var folderName = Path.GetFileNameWithoutExtension(archiveFileName);
        var projectFilesFolderFullName = Path.Combine(checkedWorkFolder, folderName);
        var archiveFileFullName = Path.Combine(checkedWorkFolder, archiveFileName);

        if (Directory.Exists(projectFilesFolderFullName))
        {
            MessagesDataManager
                ?.SendMessage(UserName, $"Delete Existing Project files in {projectFilesFolderFullName}").Wait();
            Logger.LogInformation("Delete Existing Project files in {projectFilesFolderFullName}",
                projectFilesFolderFullName);
            Directory.Delete(projectFilesFolderFullName, true);
        }

        ZipFile.ExtractToDirectory(archiveFileFullName, projectFilesFolderFullName);

        MessagesDataManager?.SendMessage(UserName, $"Project files is extracted to {projectFilesFolderFullName}")
            .Wait();
        Logger.LogInformation("Project files is extracted to {projectFilesFolderFullName}", projectFilesFolderFullName);

        //ZipClassArchiver zipClassArchiver = new ZipClassArchiver(_logger);
        //zipClassArchiver.ArchiveToPath(archiveFileFullName, projectFilesFolderFullName);

        //წავშალოთ გახსნილი არქივი, რადგან ის აღარ გვჭირდება
        //(შეიძლება ისე გავაკეთო, რომ არ წავშალო არქივი, რადგან მოქაჩვას შეიძლება დრო სჭირდებოდეს
        //ასეთ შემთხვევაში უნდა შევინარჩუნო არქივების ლიმიტირებული რაოდენობა
        //და ამ რაოდენობაზე მეტი რაც იქნება, უნდა წაიშალოს)
        MessagesDataManager?.SendMessage(UserName, $"Deleting {archiveFileFullName} file...").Wait();
        Logger.LogInformation("Deleting {archiveFileFullName} file...", archiveFileFullName);
        //წაიშალოს ლოკალური ფაილი
        File.Delete(archiveFileFullName);

        //დავადგინოთ პროგრამის ვერსია და დავაბრუნოთ
        var projectMainExeFileName = Path.Combine(projectFilesFolderFullName, $"{projectName}.dll");
        var version = Assembly.LoadFile(projectMainExeFileName).GetName().Version;
        var assemblyVersion = version?.ToString();


        var serviceEnvName = GetServiceEnvName(serviceName, environmentName);
        var serviceExists = false;
        if (!string.IsNullOrWhiteSpace(serviceName))
        {
            //დავადგინოთ არსებობს თუ არა სერვისების სიაში სერვისი სახელით {serviceEnvName}
            serviceExists = IsServiceExists(serviceEnvName);
            if (serviceExists)
            {
                MessagesDataManager?.SendMessage(UserName, $"Service {serviceEnvName} is exists").Wait();
                Logger.LogInformation("Service {serviceEnvName} is exists", serviceEnvName);
            }
            else
            {
                MessagesDataManager?.SendMessage(UserName, $"Service {serviceEnvName} does not exists").Wait();
                Logger.LogInformation("Service {serviceEnvName} does not exists", serviceEnvName);
            }

            //თუ სიაში არსებობს დავადგინოთ გაშვებულია თუ არა სერვისი.
            var serviceIsRunning = IsServiceRunning(serviceEnvName);
            if (serviceIsRunning)
            {
                MessagesDataManager?.SendMessage(UserName, $"Service {serviceEnvName} is running").Wait();
                Logger.LogInformation("Service {serviceEnvName} is running", serviceEnvName);
            }
            else
            {
                MessagesDataManager?.SendMessage(UserName, $"Service {serviceEnvName} does not running").Wait();
                Logger.LogInformation("Service {serviceEnvName} does not running", serviceEnvName);
            }

            if (!serviceExists && serviceIsRunning)
            {
                MessagesDataManager
                    ?.SendMessage(UserName, $"Service {serviceEnvName} does not exists, but process is running").Wait();
                Logger.LogError("Service {serviceEnvName} does not exists, but process is running", serviceEnvName);
                return null;
            }

            //თუ სერვისი გაშვებულია უკვე, გავაჩეროთ
            MessagesDataManager?.SendMessage(UserName, $"Try to stop Service {serviceEnvName}").Wait();
            Logger.LogInformation("Try to stop Service {serviceEnvName}", serviceEnvName);
            if (!Stop(serviceEnvName))
            {
                MessagesDataManager?.SendMessage(UserName, $"Service {serviceEnvName} does not stopped").Wait();
                Logger.LogError("Service {serviceEnvName} does not stopped", serviceEnvName);
                return null;
            }

            if (IsProcessRunning(serviceEnvName))
            {
                MessagesDataManager
                    ?.SendMessage(UserName, $"Process {serviceEnvName} is running and cannot be updated.")
                    .Wait();
                Logger.LogError("Process {serviceEnvName} is running and cannot be updated.", serviceEnvName);
                return null;
            }
        }

        //თუ არსებობს, წაიშალოს არსებული ფაილები.
        //თუ არსებობს, დავაარქივოთ და გადავინახოთ პროგრამის მიმდინარე ფაილები
        //(ეს კეთდება იმისათვის, რომ შესაძლებელი იყოს წინა ვერსიაზე სწრაფად დაბრუნება)
        //რადგან გადანახვა ხდება, ზედმეტი ფაილები რომ არ დაგროვდეს, წავშალოთ წინა გადანახულები,
        //ოღონდ არ წავშალოთ ბოლო რამდენიმე. (რაოდენობა პარამეტრებით უნდა იყოს განსაზღვრული)
        var deleteSuccess = true;
        var projectInstallFullPath = Path.Combine(checkedInstallFolder, projectName);

        if (Directory.Exists(projectInstallFullPath))
        {
            deleteSuccess = false;
            MessagesDataManager?.SendMessage(UserName, $"Folder {projectInstallFullPath} already exists").Wait();
            Logger.LogInformation("Folder {projectInstallFullPath} already exists", projectInstallFullPath);

            var tryCount = 0;
            while (!deleteSuccess && tryCount < 10)
            {
                tryCount++;
                try
                {
                    MessagesDataManager
                        ?.SendMessage(UserName, $"Try to delete folder {projectInstallFullPath} {tryCount}...").Wait();
                    Logger.LogInformation("Try to delete folder {projectInstallFullPath} {tryCount}...",
                        projectInstallFullPath, tryCount);
                    Directory.Delete(projectInstallFullPath, true);
                    MessagesDataManager
                        ?.SendMessage(UserName, $"Folder {projectInstallFullPath} deleted successfully").Wait();
                    Logger.LogInformation("Folder {projectInstallFullPath} deleted successfully",
                        projectInstallFullPath);
                    deleteSuccess = true;
                }
                catch
                {
                    MessagesDataManager?.SendMessage(UserName,
                        $"Folder {projectInstallFullPath} could not deleted on try {tryCount}").Wait();
                    Logger.LogWarning("Folder {projectInstallFullPath} could not deleted on try {tryCount}",
                        projectInstallFullPath, tryCount);
                    MessagesDataManager?.SendMessage(UserName, "waiting for 3 seconds...").Wait();
                    Logger.LogInformation("waiting for 3 seconds...");
                    Thread.Sleep(3000);
                }
            }
        }

        if (!deleteSuccess)
        {
            MessagesDataManager?.SendMessage(UserName, $"folder {projectInstallFullPath} can not Deleted").Wait();
            Logger.LogError("folder {projectInstallFullPath} can not Deleted", projectInstallFullPath);
            return null;
        }

        MessagesDataManager?.SendMessage(UserName, $"Install {projectName} files to {projectInstallFullPath}...")
            .Wait();
        Logger.LogInformation("Install {projectName} files to {projectInstallFullPath}...", projectName,
            projectInstallFullPath);
        //გაშლილი არქივის ფაილები გადავიტანოთ სერვისის ფოლდერში
        Directory.Move(projectFilesFolderFullName, projectInstallFullPath);
        //ჩავაგდოთ პარამეტრების ფაილი ახლადდაინსტალირებულ ფოლდერში
        appSettingsFile?.WriteAllTextToPath(projectInstallFullPath);

        if (!ChangeOwner(projectInstallFullPath, filesUserName, filesUsersGroupName))
        {
            MessagesDataManager?.SendMessage(UserName, $"folder {projectInstallFullPath} owner can not be changed")
                .Wait();
            Logger.LogError("folder {projectInstallFullPath} owner can not be changed", projectInstallFullPath);
            return null;
        }


        if (string.IsNullOrWhiteSpace(serviceName))
            return assemblyVersion;

        //თუ სერვისი უკვე დარეგისტრირებულია, შევამოწმოთ სწორად არის თუ არა დარეგისტრირებული.
        if (serviceExists)
            if (!IsServiceRegisteredProperly(projectName, serviceEnvName, serviceUserName, projectInstallFullPath))
                if (RemoveService(serviceEnvName))
                    serviceExists = false;

        //თუ სერვისი არ არის დარეგისტრირებული და პლატფორმა მოითხოვს დარეგისტრირებას, დავარეგისტრიროთ
        if (!serviceExists)
        {
            MessagesDataManager?.SendMessage(UserName, $"registering service {serviceEnvName}...").Wait();
            Logger.LogInformation("registering service {serviceEnvName}...", serviceEnvName);
            if (!RegisterService(projectName, serviceEnvName, serviceUserName, projectInstallFullPath))
            {
                MessagesDataManager?.SendMessage(UserName, $"cannot register Service {serviceEnvName}").Wait();
                Logger.LogError("cannot register Service {serviceEnvName}", serviceEnvName);
                return null;
            }
        }

        //გავუშვათ სერვისი და დავრწმუნდეთ, რომ გაეშვა.
        if (Start(serviceEnvName))
            return assemblyVersion;

        MessagesDataManager?.SendMessage(UserName, $"Service {serviceEnvName} can not started").Wait();
        Logger.LogError("Service {serviceEnvName} can not started", serviceEnvName);
        return null;
    }

    public string? RunUpdateApplication(string archiveFileName, string projectName, string filesUserName,
        string filesUsersGroupName, string installWorkFolder, string installFolder)
    {
        //დავადგინოთ არსებობს თუ არა {_workFolder} სახელით ქვეფოლდერი სამუშაო ფოლდერში
        //და თუ არ არსებობს, შევქმნათ
        var checkedWorkFolder = FileStat.CreateFolderIfNotExists(installWorkFolder, UseConsole);
        if (checkedWorkFolder == null)
        {
            MessagesDataManager?.SendMessage(UserName, $"Installer work folder {installWorkFolder} does not created")
                .Wait();
            Logger.LogError("Installer work folder {installWorkFolder} does not created", installWorkFolder);
            return null;
        }

        var checkedInstallFolder = FileStat.CreateFolderIfNotExists(installFolder, UseConsole);
        if (checkedInstallFolder == null)
        {
            MessagesDataManager?.SendMessage(UserName, $"Installer install folder {installFolder} does not created")
                .Wait();
            Logger.LogError("Installer install folder {installFolder} does not created", installFolder);
            return null;
        }

        MessagesDataManager?.SendMessage(UserName, $"Installer install folder is {checkedInstallFolder}").Wait();
        Logger.LogInformation("Installer install folder is {checkedInstallFolder}", checkedInstallFolder);

        //გავშალოთ არქივი სამუშაო ფოლდერში, იმისათვის, რომ დავრწმუნდეთ,
        //რომ არქივი დაზიანებული არ არის და ყველა ფაილის გახსნა ხერხდება
        //ZipClassArchiver zipClassArchiver = new ZipClassArchiver(_logger, outputFolderPath, zipFileFullName);
        var folderName = Path.GetFileNameWithoutExtension(archiveFileName);
        var projectFilesFolderFullName = Path.Combine(checkedWorkFolder, folderName);
        var archiveFileFullName = Path.Combine(checkedWorkFolder, archiveFileName);

        if (Directory.Exists(projectFilesFolderFullName))
        {
            MessagesDataManager
                ?.SendMessage(UserName, $"Delete Existing Project files in {projectFilesFolderFullName}").Wait();
            Logger.LogInformation("Delete Existing Project files in {projectFilesFolderFullName}",
                projectFilesFolderFullName);
            Directory.Delete(projectFilesFolderFullName, true);
        }

        ZipFile.ExtractToDirectory(archiveFileFullName, projectFilesFolderFullName);

        MessagesDataManager?.SendMessage(UserName, $"Project files is extracted to {projectFilesFolderFullName}")
            .Wait();
        Logger.LogInformation("Project files is extracted to {projectFilesFolderFullName}", projectFilesFolderFullName);

        //ZipClassArchiver zipClassArchiver = new ZipClassArchiver(_logger);
        //zipClassArchiver.ArchiveToPath(archiveFileFullName, projectFilesFolderFullName);

        //წავშალოთ გახსნილი არქივი, რადგან ის აღარ გვჭირდება
        //(შეიძლება ისე გავაკეთო, რომ არ წავშალო არქივი, რადგან მოქაჩვას შეიძლება დრო სჭირდებოდეს
        //ასეთ შემთხვევაში უნდა შევინარჩუნო არქივების ლიმიტირებული რაოდენობა
        //და ამ რაოდენობაზე მეტი რაც იქნება, უნდა წაიშალოს)
        MessagesDataManager?.SendMessage(UserName, $"Deleting {archiveFileFullName} file...").Wait();
        Logger.LogInformation("Deleting {archiveFileFullName} file...", archiveFileFullName);
        //წაიშალოს ლოკალური ფაილი
        File.Delete(archiveFileFullName);

        //დავადგინოთ პროგრამის ვერსია და დავაბრუნოთ
        var projectMainExeFileName = Path.Combine(projectFilesFolderFullName, $"{projectName}.dll");
        var version = Assembly.LoadFile(projectMainExeFileName).GetName().Version;
        var assemblyVersion = version?.ToString();


        //თუ არსებობს, წაიშალოს არსებული ფაილები.
        //თუ არსებობს, დავაარქივოთ და გადავინახოთ პროგრამის მიმდინარე ფაილები
        //(ეს კეთდება იმისათვის, რომ შესაძლებელი იყოს წინა ვერსიაზე სწრაფად დაბრუნება)
        //რადგან გადანახვა ხდება, ზედმეტი ფაილები რომ არ დაგროვდეს, წავშალოთ წინა გადანახულები,
        //ოღონდ არ წავშალოთ ბოლო რამდენიმე. (რაოდენობა პარამეტრებით უნდა იყოს განსაზღვრული)
        var deleteSuccess = true;
        var projectInstallFullPath = Path.Combine(checkedInstallFolder, projectName);

        if (Directory.Exists(projectInstallFullPath))
        {
            deleteSuccess = false;
            MessagesDataManager?.SendMessage(UserName, $"Folder {projectInstallFullPath} already exists").Wait();
            Logger.LogInformation("Folder {projectInstallFullPath} already exists", projectInstallFullPath);

            var tryCount = 0;
            while (!deleteSuccess && tryCount < 10)
            {
                tryCount++;
                try
                {
                    MessagesDataManager
                        ?.SendMessage(UserName, $"Try to delete folder {projectInstallFullPath} {tryCount}...").Wait();
                    Logger.LogInformation("Try to delete folder {projectInstallFullPath} {tryCount}...",
                        projectInstallFullPath, tryCount);
                    Directory.Delete(projectInstallFullPath, true);
                    MessagesDataManager
                        ?.SendMessage(UserName, $"Folder {projectInstallFullPath} deleted successfully").Wait();
                    Logger.LogInformation("Folder {projectInstallFullPath} deleted successfully",
                        projectInstallFullPath);
                    deleteSuccess = true;
                }
                catch
                {
                    MessagesDataManager?.SendMessage(UserName,
                        $"Folder {projectInstallFullPath} could not deleted on try {tryCount}").Wait();
                    Logger.LogWarning("Folder {projectInstallFullPath} could not deleted on try {tryCount}",
                        projectInstallFullPath, tryCount);
                    MessagesDataManager?.SendMessage(UserName, "waiting for 3 seconds...").Wait();
                    Logger.LogInformation("waiting for 3 seconds...");
                    Thread.Sleep(3000);
                }
            }
        }

        if (!deleteSuccess)
        {
            MessagesDataManager?.SendMessage(UserName, $"folder {projectInstallFullPath} can not Deleted").Wait();
            Logger.LogError("folder {projectInstallFullPath} can not Deleted", projectInstallFullPath);
            return null;
        }

        MessagesDataManager?.SendMessage(UserName, $"Install {projectName} files to {projectInstallFullPath}...")
            .Wait();
        Logger.LogInformation("Install {projectName} files to {projectInstallFullPath}...", projectName,
            projectInstallFullPath);
        //გაშლილი არქივის ფაილები გადავიტანოთ სერვისის ფოლდერში
        Directory.Move(projectFilesFolderFullName, projectInstallFullPath);

        if (ChangeOwner(projectInstallFullPath, filesUserName, filesUsersGroupName))
            return assemblyVersion;

        MessagesDataManager?.SendMessage(UserName, $"folder {projectInstallFullPath} owner can not be changed")
            .Wait();
        Logger.LogError("folder {projectInstallFullPath} owner can not be changed", projectInstallFullPath);
        return null;
    }

    protected virtual bool IsServiceRegisteredProperly(string projectName, string serviceEnvName, string userName,
        string installFolderPath)
    {
        MessagesDataManager?.SendMessage(UserName, "IsServiceRegisteredProperly not implemented").Wait();
        Logger.LogError("IsServiceRegisteredProperly not implemented");
        return false;
    }

    protected virtual bool ChangeOneFileOwner(string filePath, string? filesUserName, string? filesUsersGroupName)
    {
        MessagesDataManager?.SendMessage(UserName, "Change Owner not implemented").Wait();
        Logger.LogError("Change Owner not implemented");
        return false;
    }

    protected virtual bool ChangeOwner(string folderPath, string filesUserName, string filesUsersGroupName)
    {
        MessagesDataManager?.SendMessage(UserName, "Change Owner not implemented").Wait();
        Logger.LogError("Change Owner not implemented");
        return false;
    }

    public bool Stop(string? serviceName, string environmentName)
    {
        return Stop(GetServiceEnvName(serviceName, environmentName));
    }

    public bool Stop(string serviceEnvName)
    {
        //დავადგინოთ არსებობს თუ არა სერვისების სიაში სერვისი სახელით {serviceEnvName}
        var serviceExists = IsServiceExists(serviceEnvName);
        if (serviceExists)
        {
            MessagesDataManager?.SendMessage(UserName, $"Service {serviceEnvName} is exists").Wait();
            Logger.LogInformation("Service {serviceEnvName} is exists", serviceEnvName);
        }
        else
        {
            MessagesDataManager?.SendMessage(UserName, $"Service {serviceEnvName} does not exists").Wait();
            Logger.LogInformation("Service {serviceEnvName} does not exists", serviceEnvName);
            return true;
        }

        var serviceIsRunning = IsServiceRunning(serviceEnvName);
        if (serviceIsRunning)
        {
            MessagesDataManager?.SendMessage(UserName, $"Service {serviceEnvName} is running").Wait();
            Logger.LogInformation("Service {serviceEnvName} is running", serviceEnvName);
        }
        else
        {
            MessagesDataManager?.SendMessage(UserName, $"Service {serviceEnvName} does not running").Wait();
            Logger.LogInformation("Service {serviceEnvName} does not running", serviceEnvName);
            return true;
        }

        if (StopService(serviceEnvName))
            return true;

        MessagesDataManager?.SendMessage(UserName, $"Service {serviceEnvName} can not stopped").Wait();
        Logger.LogError("Service {serviceEnvName} can not stopped", serviceEnvName);
        return false;
    }

    public bool Start(string? serviceName, string environmentName)
    {
        return Start(GetServiceEnvName(serviceName, environmentName));
    }

    private bool Start(string serviceEnvName)
    {
        var serviceIsRunning = IsServiceRunning(serviceEnvName);
        if (serviceIsRunning)
        {
            MessagesDataManager?.SendMessage(UserName, $"Service {serviceEnvName} is running").Wait();
            Logger.LogInformation("Service {serviceEnvName} is running", serviceEnvName);
            return true;
        }

        MessagesDataManager?.SendMessage(UserName, $"Service {serviceEnvName} does not running").Wait();
        Logger.LogInformation("Service {serviceEnvName} does not running", serviceEnvName);

        if (StartService(serviceEnvName))
            return true;

        MessagesDataManager?.SendMessage(UserName, $"Service {serviceEnvName} can not started").Wait();
        Logger.LogError("Service {serviceEnvName} can not started", serviceEnvName);
        return false;
    }

    public bool RemoveProjectAndService(string projectName, string serviceName, string environmentName,
        string installFolder)
    {
        var serviceEnvName = GetServiceEnvName(serviceName, environmentName);

        MessagesDataManager?.SendMessage(UserName, $"Remove service {serviceEnvName} started...").Wait();
        Logger.LogInformation("Remove service {serviceEnvName} started...", serviceEnvName);

        var serviceExists = IsServiceExists(serviceEnvName);
        if (serviceExists)
        {
            MessagesDataManager?.SendMessage(UserName, $"Service {serviceEnvName} is exists").Wait();
            Logger.LogInformation("Service {serviceEnvName} is exists", serviceEnvName);
        }
        else
        {
            MessagesDataManager?.SendMessage(UserName, $"Service {serviceEnvName} does not exists").Wait();
            Logger.LogInformation("Service {serviceEnvName} does not exists", serviceEnvName);
        }

        var serviceIsRunning = false;
        if (serviceExists)
        {
            serviceIsRunning = IsServiceRunning(serviceEnvName);


            if (serviceIsRunning)
            {
                MessagesDataManager?.SendMessage(UserName, $"Service {serviceEnvName} is running").Wait();
                Logger.LogInformation("Service {serviceEnvName} is running", serviceEnvName);
            }
            else
            {
                MessagesDataManager?.SendMessage(UserName, $"Service {serviceEnvName} does not running").Wait();
                Logger.LogInformation("Service {serviceEnvName} does not running", serviceEnvName);
            }
        }


        if (serviceIsRunning)
            if (!Stop(serviceEnvName))
            {
                MessagesDataManager?.SendMessage(UserName, $"Service {serviceEnvName} can not be stopped").Wait();
                Logger.LogError("Service {serviceEnvName} can not be stopped", serviceEnvName);
                return false;
            }

        if (!serviceExists)
            return RemoveProject(projectName, installFolder);

        if (RemoveService(serviceEnvName))
            return RemoveProject(projectName, installFolder);

        MessagesDataManager?.SendMessage(UserName, $"Service {serviceEnvName} can not be Removed").Wait();
        Logger.LogError("Service {serviceEnvName} can not be Removed", serviceEnvName);
        return false;
    }

    public bool RemoveProject(string projectName, string installFolder)
    {
        MessagesDataManager?.SendMessage(UserName, $"Remove project {projectName} started...").Wait();
        Logger.LogInformation("Remove project {projectName} started...", projectName);
        var checkedInstallFolder = FileStat.CreateFolderIfNotExists(installFolder, UseConsole);
        if (checkedInstallFolder == null)
        {
            MessagesDataManager?.SendMessage(UserName, "Installation folder does not found").Wait();
            Logger.LogError("Installation folder does not found");
            return false;
        }

        //თუ არსებობს, წაიშალოს არსებული ფაილები.
        var projectInstallFullPath = Path.Combine(checkedInstallFolder, projectName);

        MessagesDataManager?.SendMessage(UserName, $"Deleting files {projectName}...").Wait();
        Logger.LogInformation("Deleting files {projectName}...", projectName);

        if (Directory.Exists(projectInstallFullPath))
            Directory.Delete(projectInstallFullPath, true);

        return true;
    }

    protected virtual bool RemoveService(string serviceEnvName)
    {
        return false;
    }

    protected virtual bool StopService(string serviceEnvName)
    {
        return false;
    }

    protected virtual bool StartService(string serviceEnvName)
    {
        return false;
    }

    protected virtual bool RegisterService(string projectName, string serviceEnvName, string serviceUserName,
        string installFolderPath)
    {
        return false;
    }


    protected virtual bool IsServiceExists(string serviceEnvName)
    {
        return false;
    }

    protected virtual bool IsServiceRunning(string serviceEnvName)
    {
        return false;
    }

    private static bool IsProcessRunning(string processName)
    {
        return Process.GetProcessesByName(processName).Length > 1;
    }
}