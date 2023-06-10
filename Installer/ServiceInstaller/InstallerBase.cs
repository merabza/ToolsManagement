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
    protected readonly bool UseConsole;

    protected InstallerBase(bool useConsole, ILogger logger, string runtime)
    {
        Logger = logger;
        Runtime = runtime;
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
            Logger.LogInformation(serviceExists
                ? $"Service {serviceName} is exists"
                : $"Service {serviceName} does not exists");

            if (!serviceExists)
            {
                //ეს არის პარამეტრების განახლების პროცესი, ამიტომ თუ პროგრამა სერვისია და ეს სერვისი არ არსებობს განახლება ვერ მოხდება
                //ასეთ შემთხვევაში უნდა გაეშვას უკვე მთლიანი პროგრამის განახლების პროცესი
                Logger.LogError($"Service {serviceName} does not exists, cannot update settings file");
                return false;
            }

            //თუ სერვისი გაშვებულია უკვე, გავაჩეროთ
            Logger.LogInformation($"Try to stop Service {serviceName}");
            if (!Stop(serviceName))
            {
                Logger.LogError($"Service {serviceName} does not stopped");
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
                Logger.LogError($"Process {projectName} is running and cannot be updated.");
                return false;
            }
        }

        //შევეცადოთ პარამეტრების ფაილის წაშლა
        var appSettingsFileDeletedSuccess = true;
        if (File.Exists(appSettingsFileFullPath))
        {
            appSettingsFileDeletedSuccess = false;
            Logger.LogInformation($"File {appSettingsFileFullPath} is exists");

            var tryCount = 0;
            while (!appSettingsFileDeletedSuccess && tryCount < 3)
            {
                tryCount++;
                try
                {
                    Logger.LogInformation($"Try to delete File {appSettingsFileFullPath} {tryCount}...");
                    File.Delete(appSettingsFileFullPath);
                    Logger.LogInformation($"File {appSettingsFileFullPath} deleted successfully");
                    appSettingsFileDeletedSuccess = true;
                }
                catch
                {
                    Logger.LogWarning($"File {appSettingsFileFullPath} could not deleted on try {tryCount}");
                    Logger.LogInformation("waiting for 3 seconds...");
                    Thread.Sleep(3000);
                }
            }
        }

        if (!appSettingsFileDeletedSuccess)
        {
            //თუ მიმდინარე დაინსტალირებული პარამეტრების ფაილის წაშლა ვერ მოხერხდა,
            //მაშინ პარამეტრების ფაილის განახლების პროცესი წყდება
            Logger.LogError($"File {appSettingsFileFullPath} can not Deleted");
            return false;
        }


        //შეიქმნას პარამეტრების ფაილი არსებულ ინფორმაციაზე დაყრდნობით
        File.WriteAllText(appSettingsFileFullPath, appSettingsFileBody);
        //შეიცვალოს პარამეტრების ფაილზე უფლებები საჭიროების მიხედვით.
        if (!ChangeOneFileOwner(appSettingsFileFullPath, filesUserName, filesUsersGroupName))
        {
            Logger.LogError($"File {appSettingsFileFullPath} owner can not be changed");
            return false;
        }

        //თუ ეს სერვისი არ არის პროცესი დასრულებულია, თანაც წარმატებით
        if (!string.IsNullOrWhiteSpace(serviceName))
            return true;

        //თუ სერვისია, გავუშვათ ეს სერვისი და დავრწმუნდეთ, რომ გაეშვა.
        if (Start(projectName))
            return true;

        //თუ სერვისი არ გაეშვა, ვაბრუნებთ შეტყობინებას
        Logger.LogError($"Service {projectName} can not started");
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
            Logger.LogError($"Installer install folder {installFolder} does not exists");
            return null;
        }

        Logger.LogInformation($"Installer install folder is {installFolder}");

        var projectInstallFullPath = Path.Combine(installFolder, projectName);
        if (!Directory.Exists(projectInstallFullPath))
        {
            Logger.LogError($"Project install folder {projectInstallFullPath} does not exists");
            return null;
        }

        Logger.LogInformation($"Project install folder is {projectInstallFullPath}");
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
            Logger.LogError($"Installer work folder {installWorkFolder} does not created");
            return null;
        }

        var checkedInstallFolder = FileStat.CreateFolderIfNotExists(installFolder, UseConsole);
        if (checkedInstallFolder == null)
        {
            Logger.LogError($"Installer install folder {installFolder} does not created");
            return null;
        }

        Logger.LogInformation($"Installer install folder is {checkedInstallFolder}");

        //გავშალოთ არქივი სამუშაო ფოლდერში, იმისათვის, რომ დავრწმუნდეთ,
        //რომ არქივი დაზიანებული არ არის და ყველა ფაილის გახსნა ხერხდება
        //ZipClassArchiver zipClassArchiver = new ZipClassArchiver(_logger, outputFolderPath, zipFileFullName);
        var folderName = Path.GetFileNameWithoutExtension(archiveFileName);
        var projectFilesFolderFullName = Path.Combine(checkedWorkFolder, folderName);
        var archiveFileFullName = Path.Combine(checkedWorkFolder, archiveFileName);

        if (Directory.Exists(projectFilesFolderFullName))
        {
            Logger.LogInformation($"Delete Existing Project files in {projectFilesFolderFullName}");
            Directory.Delete(projectFilesFolderFullName, true);
        }

        ZipFile.ExtractToDirectory(archiveFileFullName, projectFilesFolderFullName);

        Logger.LogInformation($"Project files is extracted to {projectFilesFolderFullName}");

        //ZipClassArchiver zipClassArchiver = new ZipClassArchiver(_logger);
        //zipClassArchiver.ArchiveToPath(archiveFileFullName, projectFilesFolderFullName);

        //წავშალოთ გახსნილი არქივი, რადგან ის აღარ გვჭირდება
        //(შეიძლება ისე გავაკეთო, რომ არ წავშალო არქივი, რადგან მოქაჩვას შეიძლება დრო სჭირდებოდეს
        //ასეთ შემთხვევაში უნდა შევინარჩუნო არქივების ლიმიტირებული რაოდენობა
        //და ამ რაოდენობაზე მეტი რაც იქნება, უნდა წაიშალოს)
        Logger.LogInformation($"Deleting {archiveFileFullName} file...");
        //წაიშალოს ლოკალური ფაილი
        File.Delete(archiveFileFullName);

        //დავადგინოთ პროგრამის ვერსია და დავაბრუნოთ
        var projectMainExeFileName = Path.Combine(projectFilesFolderFullName, $"{serviceName}.dll");
        var version = Assembly.LoadFile(projectMainExeFileName).GetName().Version;
        var assemblyVersion = version == null ? null : version.ToString();

        var serviceExists = false;
        if (!string.IsNullOrWhiteSpace(serviceName))
        {
            //დავადგინოთ არსებობს თუ არა სერვისების სიაში სერვისი სახელით {serviceName}
            serviceExists = IsServiceExists(serviceName);
            Logger.LogInformation(serviceExists
                ? $"Service {serviceName} is exists"
                : $"Service {serviceName} does not exists");


            //თუ სიაში არსებობს დავადგინოთ გაშვებულია თუ არა სერვისი.
            var serviceIsRunning = IsServiceRunning(serviceName);
            Logger.LogInformation(serviceIsRunning
                ? $"Service {serviceName} is running"
                : $"Service {serviceName} does not running");

            if (!serviceExists && serviceIsRunning)
            {
                Logger.LogError($"Service {serviceName} does not exists, but process is running");
                return null;
            }

            //თუ სერვისი გაშვებულია უკვე, გავაჩეროთ
            Logger.LogInformation($"Try to stop Service {serviceName}");
            if (!Stop(serviceName))
            {
                Logger.LogError($"Service {serviceName} does not stopped");
                return null;
            }

            if (IsProcessRunning(serviceName))
            {
                Logger.LogError($"Process {serviceName} is running and cannot be updated.");
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
            Logger.LogInformation($"Folder {projectInstallFullPath} already exists");

            var tryCount = 0;
            while (!deleteSuccess && tryCount < 3)
            {
                tryCount++;
                try
                {
                    Logger.LogInformation($"Try to delete folder {projectInstallFullPath} {tryCount}...");
                    Directory.Delete(projectInstallFullPath, true);
                    Logger.LogInformation($"Folder {projectInstallFullPath} deleted successfully");
                    deleteSuccess = true;
                }
                catch
                {
                    Logger.LogWarning($"Folder {projectInstallFullPath} could not deleted on try {tryCount}");
                    Logger.LogInformation("waiting for 3 seconds...");
                    Thread.Sleep(3000);
                }
            }
        }

        if (!deleteSuccess)
        {
            Logger.LogError($"folder {projectInstallFullPath} can not Deleted");
            return null;
        }

        Logger.LogInformation($"Install {projectName} files to {projectInstallFullPath}...");
        //გაშლილი არქივის ფაილები გადავიტანოთ სერვისის ფოლდერში
        Directory.Move(projectFilesFolderFullName, projectInstallFullPath);
        //ჩავაგდოთ პარამეტრების ფაილი ახლადდაინსტალირებულ ფოლდერში
        if (appSettingsFile is not null)
            appSettingsFile.WriteAllTextToPath(projectInstallFullPath);

        if (!ChangeOwner(projectInstallFullPath, filesUserName, filesUsersGroupName))
        {
            Logger.LogError($"folder {projectInstallFullPath} owner can not be changed");
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
            Logger.LogInformation($"registering service {serviceName}...");
            if (!RegisterService(projectName, serviceName, serviceUserName, projectInstallFullPath))
            {
                Logger.LogError($"cannot register Service {serviceName}");
                return null;
            }
        }

        //გავუშვათ სერვისი და დავრწმუნდეთ, რომ გაეშვა.
        if (Start(serviceName))
            return assemblyVersion;

        Logger.LogError($"Service {serviceName} can not started");
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
            Logger.LogError($"Installer work folder {installWorkFolder} does not created");
            return null;
        }

        var checkedInstallFolder = FileStat.CreateFolderIfNotExists(installFolder, UseConsole);
        if (checkedInstallFolder == null)
        {
            Logger.LogError($"Installer install folder {installFolder} does not created");
            return null;
        }

        Logger.LogInformation($"Installer install folder is {checkedInstallFolder}");

        //გავშალოთ არქივი სამუშაო ფოლდერში, იმისათვის, რომ დავრწმუნდეთ,
        //რომ არქივი დაზიანებული არ არის და ყველა ფაილის გახსნა ხერხდება
        //ZipClassArchiver zipClassArchiver = new ZipClassArchiver(_logger, outputFolderPath, zipFileFullName);
        var folderName = Path.GetFileNameWithoutExtension(archiveFileName);
        var projectFilesFolderFullName = Path.Combine(checkedWorkFolder, folderName);
        var archiveFileFullName = Path.Combine(checkedWorkFolder, archiveFileName);

        if (Directory.Exists(projectFilesFolderFullName))
        {
            Logger.LogInformation($"Delete Existing Project files in {projectFilesFolderFullName}");
            Directory.Delete(projectFilesFolderFullName, true);
        }

        ZipFile.ExtractToDirectory(archiveFileFullName, projectFilesFolderFullName);

        Logger.LogInformation($"Project files is extracted to {projectFilesFolderFullName}");

        //ZipClassArchiver zipClassArchiver = new ZipClassArchiver(_logger);
        //zipClassArchiver.ArchiveToPath(archiveFileFullName, projectFilesFolderFullName);

        //წავშალოთ გახსნილი არქივი, რადგან ის აღარ გვჭირდება
        //(შეიძლება ისე გავაკეთო, რომ არ წავშალო არქივი, რადგან მოქაჩვას შეიძლება დრო სჭირდებოდეს
        //ასეთ შემთხვევაში უნდა შევინარჩუნო არქივების ლიმიტირებული რაოდენობა
        //და ამ რაოდენობაზე მეტი რაც იქნება, უნდა წაიშალოს)
        Logger.LogInformation($"Deleting {archiveFileFullName} file...");
        //წაიშალოს ლოკალური ფაილი
        File.Delete(archiveFileFullName);

        //დავადგინოთ პროგრამის ვერსია და დავაბრუნოთ
        var projectMainExeFileName = Path.Combine(projectFilesFolderFullName, $"{projectName}.dll");
        var version = Assembly.LoadFile(projectMainExeFileName).GetName().Version;
        var assemblyVersion = version == null ? null : version.ToString();


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
            Logger.LogInformation($"Folder {projectInstallFullPath} already exists");

            var tryCount = 0;
            while (!deleteSuccess && tryCount < 3)
            {
                tryCount++;
                try
                {
                    Logger.LogInformation($"Try to delete folder {projectInstallFullPath} {tryCount}...");
                    Directory.Delete(projectInstallFullPath, true);
                    Logger.LogInformation($"Folder {projectInstallFullPath} deleted successfully");
                    deleteSuccess = true;
                }
                catch
                {
                    Logger.LogWarning($"Folder {projectInstallFullPath} could not deleted on try {tryCount}");
                    Logger.LogInformation("waiting for 3 seconds...");
                    Thread.Sleep(3000);
                }
            }
        }

        if (!deleteSuccess)
        {
            Logger.LogError($"folder {projectInstallFullPath} can not Deleted");
            return null;
        }

        Logger.LogInformation($"Install {projectName} files to {projectInstallFullPath}...");
        //გაშლილი არქივის ფაილები გადავიტანოთ სერვისის ფოლდერში
        Directory.Move(projectFilesFolderFullName, projectInstallFullPath);

        if (!ChangeOwner(projectInstallFullPath, filesUserName, filesUsersGroupName))
        {
            Logger.LogError($"folder {projectInstallFullPath} owner can not be changed");
            return null;
        }


        return assemblyVersion;
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
            ? $"Service {serviceName} is exists"
            : $"Service {serviceName} does not exists");
        if (!serviceExists)
            return true;

        var serviceIsRunning = IsServiceRunning(serviceName);
        Logger.LogInformation(serviceIsRunning
            ? $"Service {serviceName} is running"
            : $"Service {serviceName} does not running");

        if (!serviceIsRunning)
            return true;

        if (StopService(serviceName))
            return true;

        Logger.LogError($"Service {serviceName} can not stopped");
        return false;
    }

    public bool Start(string serviceName)
    {
        var serviceIsRunning = IsServiceRunning(serviceName);
        Logger.LogInformation(serviceIsRunning
            ? $"Service {serviceName} is running"
            : $"Service {serviceName} does not running");

        if (serviceIsRunning)
            return true;

        if (StartService(serviceName))
            return true;

        Logger.LogError($"Service {serviceName} can not started");
        return false;
    }

    public bool RemoveProjectAndService(string projectName, string serviceName, string installFolder)
    {
        Logger.LogInformation($"Remove service {serviceName} started...");

        var serviceExists = IsServiceExists(serviceName);
        Logger.LogInformation(serviceExists
            ? $"Service {serviceName} is exists"
            : $"Service {serviceName} does not exists");

        var serviceIsRunning = false;
        if (serviceExists)
        {
            serviceIsRunning = IsServiceRunning(serviceName);
            Logger.LogInformation(serviceIsRunning
                ? $"Service {serviceName} is running"
                : $"Service {serviceName} does not running");
        }


        if (serviceIsRunning)
            if (!Stop(serviceName))
            {
                Logger.LogError($"Service {serviceName} can not be stopped");
                return false;
            }

        if (serviceExists)
            if (!RemoveService(serviceName))
            {
                Logger.LogError($"Service {serviceName} can not be Removed");
                return false;
            }

        return RemoveProject(projectName, installFolder);
    }

    public bool RemoveProject(string projectName, string installFolder)
    {
        Logger.LogInformation($"Remove project {projectName} started...");
        var checkedInstallFolder = FileStat.CreateFolderIfNotExists(installFolder, UseConsole);
        if (checkedInstallFolder == null)
        {
            Logger.LogError("Installation folder not found");
            return false;
        }

        //თუ არსებობს, წაიშალოს არსებული ფაილები.
        var projectInstallFullPath = Path.Combine(checkedInstallFolder, projectName);

        Logger.LogInformation($"Deleting files {projectName}...");

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

    private bool IsProcessRunning(string processName)
    {
        return Process.GetProcessesByName(processName).Length > 1;
    }
}