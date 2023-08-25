using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading;
using Installer.Domain;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SystemToolsShared;
using WebAgentMessagesContracts;

namespace Installer.ServiceInstaller;

public /*open*/ class InstallerBase
{
    protected readonly ILogger Logger;
    public readonly string Runtime;
    private readonly IMessagesDataManager? _messagesDataManager;
    private readonly string? _userName;
    protected readonly bool UseConsole;

    protected InstallerBase(bool useConsole, ILogger logger, string runtime, IMessagesDataManager? messagesDataManager,
        string? userName)
    {
        Logger = logger;
        Runtime = runtime;
        _messagesDataManager = messagesDataManager;
        _userName = userName;
        UseConsole = useConsole;
    }

    public bool RunUpdateSettings(string projectName, string? serviceName, string appSettingsFileName,
        string appSettingsFileBody, string? filesUserName, string? filesUsersGroupName, string installFolder)
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
                Logger.LogWarning("Parameters file is already in latest version and not needs update");
                return true;
            }
        }

        //აქ თუ მოვედით, ან დაინსტალირებული ფაილი არ არსებობს, ან ახალი ფაილისა და დაინსტალირებული ფაილის ვერსიები არ ემთხვევა.
        //კიდევ აქ მოსვლის მიზეზი შეიძლება იყოს, ის რომ ფაილებში არასწორად არის, ან საერთოდ არ არის გაწერილი ვერსიები
        //ამ ბოლო შემთხვევაზე ყურადღებას არ ვამახვილებთ, იმისათვის, რომ შესაძლებელი იყოს ასეთი "არასწორი" პროგრამების პარამეტრები განახლდეს.


        if (!string.IsNullOrWhiteSpace(serviceName))
        {
            //დავადგინოთ არსებობს თუ არა სერვისების სიაში სერვისი სახელით {projectName}
            var serviceExists = IsServiceExists(serviceName);
            Logger.LogInformation(
                serviceExists ? "Service {serviceName} is exists" : "Service {serviceName} does not exists",
                serviceName);

            if (!serviceExists)
            {
                //ეს არის პარამეტრების განახლების პროცესი, ამიტომ თუ პროგრამა სერვისია და ეს სერვისი არ არსებობს განახლება ვერ მოხდება
                //ასეთ შემთხვევაში უნდა გაეშვას უკვე მთლიანი პროგრამის განახლების პროცესი
                Logger.LogError("Service {serviceName} does not exists, cannot update settings file", serviceName);
                return false;
            }

            //თუ სერვისი გაშვებულია უკვე, გავაჩეროთ
            Logger.LogInformation("Try to stop Service {serviceName}", serviceName);
            if (!Stop(serviceName))
            {
                Logger.LogError("Service {serviceName} does not stopped", serviceName);
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
                Logger.LogError("Process {projectName} is running and cannot be updated.", projectName);
                return false;
            }
        }

        //შევეცადოთ პარამეტრების ფაილის წაშლა
        var appSettingsFileDeletedSuccess = true;
        if (File.Exists(appSettingsFileFullPath))
        {
            appSettingsFileDeletedSuccess = false;
            Logger.LogInformation("File {appSettingsFileFullPath} is exists", appSettingsFileFullPath);

            var tryCount = 0;
            while (!appSettingsFileDeletedSuccess && tryCount < 3)
            {
                tryCount++;
                try
                {
                    Logger.LogInformation("Try to delete File {appSettingsFileFullPath} {tryCount}...",
                        appSettingsFileFullPath, tryCount);
                    File.Delete(appSettingsFileFullPath);
                    Logger.LogInformation("File {appSettingsFileFullPath} deleted successfully",
                        appSettingsFileFullPath);
                    appSettingsFileDeletedSuccess = true;
                }
                catch
                {
                    Logger.LogWarning("File {appSettingsFileFullPath} could not deleted on try {tryCount}",
                        appSettingsFileFullPath, tryCount);
                    Logger.LogInformation("waiting for 3 seconds...");
                    Thread.Sleep(3000);
                }
            }
        }

        if (!appSettingsFileDeletedSuccess)
        {
            //თუ მიმდინარე დაინსტალირებული პარამეტრების ფაილის წაშლა ვერ მოხერხდა,
            //მაშინ პარამეტრების ფაილის განახლების პროცესი წყდება
            Logger.LogError("File {appSettingsFileFullPath} can not Deleted", appSettingsFileFullPath);
            return false;
        }


        //შეიქმნას პარამეტრების ფაილი არსებულ ინფორმაციაზე დაყრდნობით
        File.WriteAllText(appSettingsFileFullPath, appSettingsFileBody);
        //შეიცვალოს პარამეტრების ფაილზე უფლებები საჭიროების მიხედვით.
        if (!ChangeOneFileOwner(appSettingsFileFullPath, filesUserName, filesUsersGroupName))
        {
            Logger.LogError("File {appSettingsFileFullPath} owner can not be changed", appSettingsFileFullPath);
            return false;
        }

        //თუ ეს სერვისი არ არის პროცესი დასრულებულია, თანაც წარმატებით
        if (!string.IsNullOrWhiteSpace(serviceName))
            return true;

        //თუ სერვისია, გავუშვათ ეს სერვისი და დავრწმუნდეთ, რომ გაეშვა.
        if (Start(projectName))
            return true;

        //თუ სერვისი არ გაეშვა, ვაბრუნებთ შეტყობინებას
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
            Logger.LogError("Installer install folder {installFolder} does not exists", installFolder);
            return null;
        }

        Logger.LogInformation("Installer install folder is {installFolder}", installFolder);

        var projectInstallFullPath = Path.Combine(installFolder, projectName);
        if (!Directory.Exists(projectInstallFullPath))
        {
            Logger.LogError("Project install folder {projectInstallFullPath} does not exists", projectInstallFullPath);
            return null;
        }

        Logger.LogInformation("Project install folder is {projectInstallFullPath}", projectInstallFullPath);
        return projectInstallFullPath;
    }

    public string? RunUpdateService(string archiveFileName, string projectName, string? serviceName,
        FileNameAndTextContent? appSettingsFile, string serviceUserName, string filesUserName,
        string filesUsersGroupName, string installWorkFolder, string installFolder)
    {
        //დავადგინოთ არსებობს თუ არა {_workFolder} სახელით ქვეფოლდერი სამუშაო ფოლდერში
        //და თუ არ არსებობს, შევქმნათ
        var checkedWorkFolder = FileStat.CreateFolderIfNotExists(installWorkFolder, UseConsole);
        if (checkedWorkFolder == null)
        {
            Logger.LogError("Installer work folder {installWorkFolder} does not created", installWorkFolder);
            return null;
        }

        var checkedInstallFolder = FileStat.CreateFolderIfNotExists(installFolder, UseConsole);
        if (checkedInstallFolder == null)
        {
            Logger.LogError("Installer install folder {installFolder} does not created", installFolder);
            return null;
        }

        Logger.LogInformation("Installer install folder is {checkedInstallFolder}", checkedInstallFolder);

        //გავშალოთ არქივი სამუშაო ფოლდერში, იმისათვის, რომ დავრწმუნდეთ,
        //რომ არქივი დაზიანებული არ არის და ყველა ფაილის გახსნა ხერხდება
        //ZipClassArchiver zipClassArchiver = new ZipClassArchiver(_logger, outputFolderPath, zipFileFullName);
        var folderName = Path.GetFileNameWithoutExtension(archiveFileName);
        var projectFilesFolderFullName = Path.Combine(checkedWorkFolder, folderName);
        var archiveFileFullName = Path.Combine(checkedWorkFolder, archiveFileName);

        if (Directory.Exists(projectFilesFolderFullName))
        {
            Logger.LogInformation("Delete Existing Project files in {projectFilesFolderFullName}",
                projectFilesFolderFullName);
            Directory.Delete(projectFilesFolderFullName, true);
        }

        ZipFile.ExtractToDirectory(archiveFileFullName, projectFilesFolderFullName);

        Logger.LogInformation("Project files is extracted to {projectFilesFolderFullName}", projectFilesFolderFullName);

        //ZipClassArchiver zipClassArchiver = new ZipClassArchiver(_logger);
        //zipClassArchiver.ArchiveToPath(archiveFileFullName, projectFilesFolderFullName);

        //წავშალოთ გახსნილი არქივი, რადგან ის აღარ გვჭირდება
        //(შეიძლება ისე გავაკეთო, რომ არ წავშალო არქივი, რადგან მოქაჩვას შეიძლება დრო სჭირდებოდეს
        //ასეთ შემთხვევაში უნდა შევინარჩუნო არქივების ლიმიტირებული რაოდენობა
        //და ამ რაოდენობაზე მეტი რაც იქნება, უნდა წაიშალოს)
        Logger.LogInformation("Deleting {archiveFileFullName} file...", archiveFileFullName);
        //წაიშალოს ლოკალური ფაილი
        File.Delete(archiveFileFullName);

        //დავადგინოთ პროგრამის ვერსია და დავაბრუნოთ
        var projectMainExeFileName = Path.Combine(projectFilesFolderFullName, $"{serviceName}.dll");
        var version = Assembly.LoadFile(projectMainExeFileName).GetName().Version;
        var assemblyVersion = version?.ToString();

        var serviceExists = false;
        if (!string.IsNullOrWhiteSpace(serviceName))
        {
            //დავადგინოთ არსებობს თუ არა სერვისების სიაში სერვისი სახელით {serviceName}
            serviceExists = IsServiceExists(serviceName);
            Logger.LogInformation(serviceExists
                ? "Service {serviceName} is exists"
                : "Service {serviceName} does not exists", serviceName);


            //თუ სიაში არსებობს დავადგინოთ გაშვებულია თუ არა სერვისი.
            var serviceIsRunning = IsServiceRunning(serviceName);
            Logger.LogInformation(serviceIsRunning
                ? "Service {serviceName} is running"
                : "Service {serviceName} does not running", serviceName);

            if (!serviceExists && serviceIsRunning)
            {
                Logger.LogError("Service {serviceName} does not exists, but process is running", serviceName);
                return null;
            }

            //თუ სერვისი გაშვებულია უკვე, გავაჩეროთ
            Logger.LogInformation("Try to stop Service {serviceName}", serviceName);
            if (!Stop(serviceName))
            {
                Logger.LogError("Service {serviceName} does not stopped", serviceName);
                return null;
            }

            if (IsProcessRunning(serviceName))
            {
                Logger.LogError("Process {serviceName} is running and cannot be updated.", serviceName);
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
            Logger.LogInformation("Folder {projectInstallFullPath} already exists", projectInstallFullPath);

            var tryCount = 0;
            while (!deleteSuccess && tryCount < 3)
            {
                tryCount++;
                try
                {
                    Logger.LogInformation("Try to delete folder {projectInstallFullPath} {tryCount}...",
                        projectInstallFullPath, tryCount);
                    Directory.Delete(projectInstallFullPath, true);
                    Logger.LogInformation("Folder {projectInstallFullPath} deleted successfully",
                        projectInstallFullPath);
                    deleteSuccess = true;
                }
                catch
                {
                    Logger.LogWarning("Folder {projectInstallFullPath} could not deleted on try {tryCount}",
                        projectInstallFullPath, tryCount);
                    Logger.LogInformation("waiting for 3 seconds...");
                    Thread.Sleep(3000);
                }
            }
        }

        if (!deleteSuccess)
        {
            Logger.LogError("folder {projectInstallFullPath} can not Deleted", projectInstallFullPath);
            return null;
        }

        Logger.LogInformation("Install {projectName} files to {projectInstallFullPath}...", projectName,
            projectInstallFullPath);
        //გაშლილი არქივის ფაილები გადავიტანოთ სერვისის ფოლდერში
        Directory.Move(projectFilesFolderFullName, projectInstallFullPath);
        //ჩავაგდოთ პარამეტრების ფაილი ახლადდაინსტალირებულ ფოლდერში
        appSettingsFile?.WriteAllTextToPath(projectInstallFullPath);

        if (!ChangeOwner(projectInstallFullPath, filesUserName, filesUsersGroupName))
        {
            Logger.LogError("folder {projectInstallFullPath} owner can not be changed", projectInstallFullPath);
            return null;
        }


        if (string.IsNullOrWhiteSpace(serviceName))
            return assemblyVersion;

        //თუ სერვისი უკვე დარეგისტრირებულია, შევამოწმოთ სწორად არის თუ არა დარეგისტრირებული.
        if (serviceExists)
            if (!IsServiceRegisteredProperly(projectName, serviceName, serviceUserName, projectInstallFullPath))
                if (RemoveService(serviceName))
                    serviceExists = false;

        //თუ სერვისი არ არის დარეგისტრირებული და პლატფორმა მოითხოვს დარეგისტრირებას, დავარეგისტრიროთ
        if (!serviceExists)
        {
            Logger.LogInformation("registering service {serviceName}...", serviceName);
            if (!RegisterService(projectName, serviceName, serviceUserName, projectInstallFullPath))
            {
                Logger.LogError("cannot register Service {serviceName}", serviceName);
                return null;
            }
        }

        //გავუშვათ სერვისი და დავრწმუნდეთ, რომ გაეშვა.
        if (Start(serviceName))
            return assemblyVersion;

        Logger.LogError("Service {serviceName} can not started", serviceName);
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
            Logger.LogError("Installer work folder {installWorkFolder} does not created", installWorkFolder);
            return null;
        }

        var checkedInstallFolder = FileStat.CreateFolderIfNotExists(installFolder, UseConsole);
        if (checkedInstallFolder == null)
        {
            Logger.LogError("Installer install folder {installFolder} does not created", installFolder);
            return null;
        }

        Logger.LogInformation("Installer install folder is {checkedInstallFolder}", checkedInstallFolder);

        //გავშალოთ არქივი სამუშაო ფოლდერში, იმისათვის, რომ დავრწმუნდეთ,
        //რომ არქივი დაზიანებული არ არის და ყველა ფაილის გახსნა ხერხდება
        //ZipClassArchiver zipClassArchiver = new ZipClassArchiver(_logger, outputFolderPath, zipFileFullName);
        var folderName = Path.GetFileNameWithoutExtension(archiveFileName);
        var projectFilesFolderFullName = Path.Combine(checkedWorkFolder, folderName);
        var archiveFileFullName = Path.Combine(checkedWorkFolder, archiveFileName);

        if (Directory.Exists(projectFilesFolderFullName))
        {
            Logger.LogInformation("Delete Existing Project files in {projectFilesFolderFullName}",
                projectFilesFolderFullName);
            Directory.Delete(projectFilesFolderFullName, true);
        }

        ZipFile.ExtractToDirectory(archiveFileFullName, projectFilesFolderFullName);

        Logger.LogInformation("Project files is extracted to {projectFilesFolderFullName}", projectFilesFolderFullName);

        //ZipClassArchiver zipClassArchiver = new ZipClassArchiver(_logger);
        //zipClassArchiver.ArchiveToPath(archiveFileFullName, projectFilesFolderFullName);

        //წავშალოთ გახსნილი არქივი, რადგან ის აღარ გვჭირდება
        //(შეიძლება ისე გავაკეთო, რომ არ წავშალო არქივი, რადგან მოქაჩვას შეიძლება დრო სჭირდებოდეს
        //ასეთ შემთხვევაში უნდა შევინარჩუნო არქივების ლიმიტირებული რაოდენობა
        //და ამ რაოდენობაზე მეტი რაც იქნება, უნდა წაიშალოს)
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
            Logger.LogInformation("Folder {projectInstallFullPath} already exists", projectInstallFullPath);

            var tryCount = 0;
            while (!deleteSuccess && tryCount < 3)
            {
                tryCount++;
                try
                {
                    Logger.LogInformation("Try to delete folder {projectInstallFullPath} {tryCount}...",
                        projectInstallFullPath, tryCount);
                    Directory.Delete(projectInstallFullPath, true);
                    Logger.LogInformation("Folder {projectInstallFullPath} deleted successfully",
                        projectInstallFullPath);
                    deleteSuccess = true;
                }
                catch
                {
                    Logger.LogWarning("Folder {projectInstallFullPath} could not deleted on try {tryCount}",
                        projectInstallFullPath, tryCount);
                    Logger.LogInformation("waiting for 3 seconds...");
                    Thread.Sleep(3000);
                }
            }
        }

        if (!deleteSuccess)
        {
            Logger.LogError("folder {projectInstallFullPath} can not Deleted", projectInstallFullPath);
            return null;
        }

        Logger.LogInformation("Install {projectName} files to {projectInstallFullPath}...", projectName,
            projectInstallFullPath);
        //გაშლილი არქივის ფაილები გადავიტანოთ სერვისის ფოლდერში
        Directory.Move(projectFilesFolderFullName, projectInstallFullPath);

        if (ChangeOwner(projectInstallFullPath, filesUserName, filesUsersGroupName))
            return assemblyVersion;

        Logger.LogError("folder {projectInstallFullPath} owner can not be changed", projectInstallFullPath);
        return null;
    }

    protected virtual bool IsServiceRegisteredProperly(string projectName, string serviceName, string userName,
        string installFolderPath)
    {
        Logger.LogError("IsServiceRegisteredProperly not implemented");
        return false;
    }

    protected virtual bool ChangeOneFileOwner(string filePath, string? filesUserName, string? filesUsersGroupName)
    {
        Logger.LogError("Change Owner not implemented");
        return false;
    }

    protected virtual bool ChangeOwner(string folderPath, string filesUserName, string filesUsersGroupName)
    {
        Logger.LogError("Change Owner not implemented");
        return false;
    }


    public bool Stop(string serviceName)
    {
        //დავადგინოთ არსებობს თუ არა სერვისების სიაში სერვისი სახელით {serviceName}
        var serviceExists = IsServiceExists(serviceName);
        Logger.LogInformation(serviceExists
            ? "Service {serviceName} is exists"
            : "Service {serviceName} does not exists", serviceName);
        if (!serviceExists)
            return true;

        var serviceIsRunning = IsServiceRunning(serviceName);
        Logger.LogInformation(serviceIsRunning
            ? "Service {serviceName} is running"
            : "Service {serviceName} does not running", serviceName);

        if (!serviceIsRunning)
            return true;

        if (StopService(serviceName))
            return true;

        Logger.LogError("Service {serviceName} can not stopped", serviceName);
        return false;
    }

    public bool Start(string serviceName)
    {
        var serviceIsRunning = IsServiceRunning(serviceName);
        Logger.LogInformation(serviceIsRunning
            ? "Service {serviceName} is running"
            : "Service {serviceName} does not running", serviceName);

        if (serviceIsRunning)
            return true;

        if (StartService(serviceName))
            return true;

        Logger.LogError("Service {serviceName} can not started", serviceName);
        return false;
    }

    public bool RemoveProjectAndService(string projectName, string serviceName, string installFolder)
    {
        _messagesDataManager?.SendMessage(_userName, $"Remove service {serviceName} started...").Wait();
        Logger.LogInformation("Remove service {serviceName} started...", serviceName);

        var serviceExists = IsServiceExists(serviceName);
        if (serviceExists)
        {
            _messagesDataManager?.SendMessage(_userName, $"Service {serviceName} is exists").Wait();
            Logger.LogInformation("Service {serviceName} is exists", serviceName);
        }
        else
        {
            _messagesDataManager?.SendMessage(_userName, $"Service {serviceName} does not exists").Wait();
            Logger.LogInformation("Service {serviceName} does not exists", serviceName);
        }

        var serviceIsRunning = false;
        if (serviceExists)
        {
            serviceIsRunning = IsServiceRunning(serviceName);


            if (serviceIsRunning)
            {
                _messagesDataManager?.SendMessage(_userName, $"Service {serviceName} is running").Wait();
                Logger.LogInformation("Service {serviceName} is running", serviceName);
            }
            else
            {
                _messagesDataManager?.SendMessage(_userName, $"Service {serviceName} does not running").Wait();
                Logger.LogInformation("Service {serviceName} does not running", serviceName);
            }
        }


        if (serviceIsRunning)
            if (!Stop(serviceName))
            {
                _messagesDataManager?.SendMessage(_userName, $"Service {serviceName} can not be stopped").Wait();
                Logger.LogError("Service {serviceName} can not be stopped", serviceName);
                return false;
            }

        if (!serviceExists)
            return RemoveProject(projectName, installFolder);

        if (RemoveService(serviceName))
            return RemoveProject(projectName, installFolder);

        _messagesDataManager?.SendMessage(_userName, $"Service {serviceName} can not be Removed").Wait();
        Logger.LogError("Service {serviceName} can not be Removed", serviceName);
        return false;
    }

    public bool RemoveProject(string projectName, string installFolder)
    {
        _messagesDataManager?.SendMessage(_userName, $"Remove project {projectName} started...").Wait();
        Logger.LogInformation("Remove project {projectName} started...", projectName);
        var checkedInstallFolder = FileStat.CreateFolderIfNotExists(installFolder, UseConsole);
        if (checkedInstallFolder == null)
        {
            _messagesDataManager?.SendMessage(_userName, "Installation folder does not found").Wait();
            Logger.LogError("Installation folder does not found");
            return false;
        }

        //თუ არსებობს, წაიშალოს არსებული ფაილები.
        var projectInstallFullPath = Path.Combine(checkedInstallFolder, projectName);

        _messagesDataManager?.SendMessage(_userName, $"Deleting files {projectName}...").Wait();
        Logger.LogInformation("Deleting files {projectName}...", projectName);

        if (Directory.Exists(projectInstallFullPath))
            Directory.Delete(projectInstallFullPath, true);

        return true;
    }

    protected virtual bool RemoveService(string serviceName)
    {
        return false;
    }

    protected virtual bool StopService(string serviceName)
    {
        return false;
    }

    protected virtual bool StartService(string serviceName)
    {
        return false;
    }

    protected virtual bool RegisterService(string projectName, string serviceName, string serviceUserName,
        string installFolderPath)
    {
        return false;
    }


    protected virtual bool IsServiceExists(string serviceName)
    {
        return false;
    }

    protected virtual bool IsServiceRunning(string serviceName)
    {
        return false;
    }

    private static bool IsProcessRunning(string processName)
    {
        return Process.GetProcessesByName(processName).Length > 1;
    }
}