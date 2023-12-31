using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Installer.Domain;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OneOf;
using SystemToolsShared;
// ReSharper disable ConvertToPrimaryConstructor

namespace Installer.ServiceInstaller;

public /*open*/ abstract class InstallerBase
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

    protected abstract Task<OneOf<bool, Err[]>> IsServiceRegisteredProperly(string projectName, string serviceEnvName,
        string userName, string installFolderPath, string? serviceDescriptionSignature, string? projectDescription,
        CancellationToken cancellationToken);

    protected abstract Task<Option<Err[]>> ChangeOneFileOwner(string filePath, string? filesUserName,
        string? filesUsersGroupName, CancellationToken cancellationToken);

    protected abstract Task<Option<Err[]>> ChangeOwner(string folderPath, string filesUserName,
        string filesUsersGroupName, CancellationToken cancellationToken);

    protected abstract Option<Err[]> RemoveService(string serviceEnvName);

    protected abstract Task<Option<Err[]>> StopService(string serviceEnvName, CancellationToken cancellationToken);

    protected abstract Task<Option<Err[]>> StartService(string serviceEnvName, CancellationToken cancellationToken);

    protected abstract Task<Option<Err[]>> RegisterService(string projectName, string serviceEnvName,
        string serviceUserName,
        string installFolderPath, string? serviceDescriptionSignature, string? projectDescription,
        CancellationToken cancellationToken);

    protected abstract bool IsServiceExists(string serviceEnvName);

    protected abstract bool IsServiceRunning(string serviceEnvName);

    private static string GetServiceEnvName(string? serviceName, string environmentName)
    {
        return $"{serviceName}{environmentName}";
    }

    private static string? GetParametersVersion(string appSettingsFileBody)
    {
        var latestAppSetJObject = JObject.Parse(appSettingsFileBody);
        var latestAppSettingsVersion =
            latestAppSetJObject["VersionInfo"]?["AppSettingsVersion"]?.Value<string>();
        return latestAppSettingsVersion;
    }

    private static bool IsProcessRunning(string processName)
    {
        return Process.GetProcessesByName(processName).Length > 1;
    }

    public async Task<Option<Err[]>> RunUpdateSettings(string projectName, string? serviceName, string environmentName,
        string appSettingsFileName, string appSettingsFileBody, string? filesUserName, string? filesUsersGroupName,
        string installFolder, CancellationToken cancellationToken)
    {
        var checkBeforeStartUpdateResult =
            await CheckBeforeStartUpdate(projectName, installFolder, environmentName, cancellationToken);

        if (checkBeforeStartUpdateResult.IsT1)
            return checkBeforeStartUpdateResult.AsT1;
        var projectInstallFullPath = checkBeforeStartUpdateResult.AsT0;

        //დავადგინოთ დაინსტალირებული პარამეტრების ფაილის სრული გზა
        var appSettingsFileFullPath = Path.Combine(projectInstallFullPath, appSettingsFileName);

        //დავადგინოთ ფაილსაცავიდან მიღებული ბოლო პარამეტრების ფაილის ვერსია.
        var latestAppSettingsVersion = GetParametersVersion(appSettingsFileBody);
        //თუ დაინსტალირებული ფაილი არსებობს, დავადგინოთ მისი ვერსია
        if (!string.IsNullOrWhiteSpace(latestAppSettingsVersion) && File.Exists(appSettingsFileFullPath))
        {
            //დავადგინოთ დაინსტალირებული პარამეტრების ფაილის ვერსია.
            var currentAppSettingsFileBody = await File.ReadAllTextAsync(appSettingsFileFullPath, cancellationToken);
            var currentAppSettingsVersion = GetParametersVersion(currentAppSettingsFileBody);
            //თუ ვერსიები ემთხვევა დაინსტალირება აღარ გრძელდება, რადგან ისედაც ბოლო ვერსია აყენია
            if (!string.IsNullOrWhiteSpace(currentAppSettingsVersion) &&
                latestAppSettingsVersion == currentAppSettingsVersion)
            {
                if (MessagesDataManager is not null)
                    await MessagesDataManager.SendMessage(UserName,
                        "Parameters file is already in latest version and not needs update", cancellationToken);
                Logger.LogWarning("Parameters file is already in latest version and not needs update");
                return null;
            }
        }

        //აქ თუ მოვედით, ან დაინსტალირებული ფაილი არ არსებობს, ან ახალი ფაილისა და დაინსტალირებული ფაილის ვერსიები არ ემთხვევა.
        //კიდევ აქ მოსვლის მიზეზი შეიძლება იყოს, ის რომ ფაილებში არასწორად არის, ან საერთოდ არ არის გაწერილი ვერსიები
        //ამ ბოლო შემთხვევაზე ყურადღებას არ ვამახვილებთ, იმისათვის, რომ შესაძლებელი იყოს ასეთი "არასწორი" პროგრამების პარამეტრები განახლდეს.

        var serviceEnvName = GetServiceEnvName(serviceName, environmentName);

        if (!string.IsNullOrWhiteSpace(serviceEnvName))
        {
            //დავადგინოთ არსებობს თუ არა სერვისების სიაში სერვისი სახელით {projectName}
            var serviceExists = IsServiceExists(serviceEnvName);
            if (serviceExists)
            {
                if (MessagesDataManager is not null)
                    await MessagesDataManager.SendMessage(UserName, $"Service {serviceEnvName} is exists",
                        cancellationToken);
                Logger.LogInformation("Service {serviceEnvName} is exists", serviceEnvName);
            }
            else
            {
                if (MessagesDataManager is not null)
                    await MessagesDataManager.SendMessage(UserName, $"Service {serviceEnvName} does not exists",
                        cancellationToken);
                Logger.LogInformation("Service {serviceEnvName} does not exists", serviceEnvName);
            }

            if (!serviceExists)
            {
                //ეს არის პარამეტრების განახლების პროცესი, ამიტომ თუ პროგრამა სერვისია და ეს სერვისი არ არსებობს განახლება ვერ მოხდება
                //ასეთ შემთხვევაში უნდა გაეშვას უკვე მთლიანი პროგრამის განახლების პროცესი
                if (MessagesDataManager is not null)
                    await MessagesDataManager.SendMessage(UserName,
                        $"Service {serviceEnvName} does not exists, cannot update settings file ", cancellationToken);
                Logger.LogError("Service {serviceEnvName} does not exists, cannot update settings file",
                    serviceEnvName);
                return new Err[]
                {
                    new()
                    {
                        ErrorCode = "ServiceDoesNotExists",
                        ErrorMessage = $"Service {serviceEnvName} does not exists"
                    }
                };
            }

            //თუ სერვისი გაშვებულია უკვე, გავაჩეროთ
            if (MessagesDataManager is not null)
                await MessagesDataManager.SendMessage(UserName, $"Try to stop Service {serviceEnvName}",
                    cancellationToken);
            Logger.LogInformation("Try to stop Service {serviceEnvName}", serviceEnvName);
            var stopResult = await Stop(serviceEnvName, cancellationToken);
            if (stopResult.IsSome)
                return (Err[])stopResult;
        }
        else
        {
            if (IsProcessRunning(projectName))
            {
                //თუ სერვისი არ არის და პროგრამა მაინც გაშვებულია,
                //ასეთ შემთხვევაში პარამეტრების ფაილს ვერ გავაახლებთ,
                //რადგან გაშვებული პროგრამა ვერ მიხვდება, რომ ახალი პარამეტრები უნდა გამოიყენოს.
                //ასეთ შემთხვევაში ჯერ უნდა გაჩერდეს პროგრამა და მერე უნდა განახლდეს პარამეტრები.
                if (MessagesDataManager is not null)
                    await MessagesDataManager.SendMessage(UserName,
                        $"Process {projectName} is running and cannot be updated.", cancellationToken);
                Logger.LogError("Process {projectName} is running and cannot be updated.", projectName);
                return new Err[]
                {
                    new()
                    {
                        ErrorCode = "ProcessIsRunningAndCannotBeUpdated",
                        ErrorMessage = $"Process {projectName} is running and cannot be updated"
                    }
                };
            }
        }

        //შევეცადოთ პარამეტრების ფაილის წაშლა
        var appSettingsFileDeletedSuccess = true;
        if (File.Exists(appSettingsFileFullPath))
        {
            appSettingsFileDeletedSuccess = false;
            if (MessagesDataManager is not null)
                await MessagesDataManager.SendMessage(UserName, $"File {appSettingsFileFullPath} is exists",
                    cancellationToken);
            Logger.LogInformation("File {appSettingsFileFullPath} is exists", appSettingsFileFullPath);

            var tryCount = 0;
            while (!appSettingsFileDeletedSuccess && tryCount < 10)
            {
                tryCount++;
                try
                {
                    if (MessagesDataManager is not null)
                        await MessagesDataManager.SendMessage(UserName,
                            $"Try to delete File {appSettingsFileFullPath} {tryCount}...", cancellationToken);
                    Logger.LogInformation("Try to delete File {appSettingsFileFullPath} {tryCount}...",
                        appSettingsFileFullPath, tryCount);
                    File.Delete(appSettingsFileFullPath);
                    if (MessagesDataManager is not null)
                        await MessagesDataManager.SendMessage(UserName,
                            $"File {appSettingsFileFullPath} deleted successfully", cancellationToken);
                    Logger.LogInformation("File {appSettingsFileFullPath} deleted successfully",
                        appSettingsFileFullPath);
                    appSettingsFileDeletedSuccess = true;
                }
                catch
                {
                    if (MessagesDataManager is not null)
                        await MessagesDataManager.SendMessage(UserName,
                            $"File {appSettingsFileFullPath} could not deleted on try {tryCount}", cancellationToken);
                    Logger.LogWarning("File {appSettingsFileFullPath} could not deleted on try {tryCount}",
                        appSettingsFileFullPath, tryCount);
                    if (MessagesDataManager is not null)
                        await MessagesDataManager.SendMessage(UserName, "waiting for 3 seconds...", cancellationToken);
                    Logger.LogInformation("waiting for 3 seconds...");
                    Thread.Sleep(3000);
                }
            }
        }

        if (!appSettingsFileDeletedSuccess)
        {
            //თუ მიმდინარე დაინსტალირებული პარამეტრების ფაილის წაშლა ვერ მოხერხდა,
            //მაშინ პარამეტრების ფაილის განახლების პროცესი წყდება
            if (MessagesDataManager is not null)
                await MessagesDataManager.SendMessage(UserName, $"File {appSettingsFileFullPath} can not Deleted",
                    cancellationToken);
            Logger.LogError("File {appSettingsFileFullPath} can not Deleted", appSettingsFileFullPath);
            return new Err[]
            {
                new()
                {
                    ErrorCode = "FileCanNotDeleted",
                    ErrorMessage = $"File {appSettingsFileFullPath} can not Deleted"
                }
            };
        }


        //შეიქმნას პარამეტრების ფაილი არსებულ ინფორმაციაზე დაყრდნობით
        await File.WriteAllTextAsync(appSettingsFileFullPath, appSettingsFileBody, cancellationToken);
        //შეიცვალოს პარამეტრების ფაილზე უფლებები საჭიროების მიხედვით.
        var changeOneFileOwnerResult = await ChangeOneFileOwner(appSettingsFileFullPath, filesUserName,
            filesUsersGroupName,
            cancellationToken);
        if (changeOneFileOwnerResult.IsSome)
        {
            if (MessagesDataManager is not null)
                await MessagesDataManager.SendMessage(UserName,
                    $"File {appSettingsFileFullPath} owner can not be changed", cancellationToken);
            Logger.LogError("File {appSettingsFileFullPath} owner can not be changed", appSettingsFileFullPath);
            return new Err[]
            {
                new()
                {
                    ErrorCode = "FileOwnerCanNotBeChanged",
                    ErrorMessage = $"File {appSettingsFileFullPath} owner can not be changed"
                }
            };
        }

        //თუ ეს სერვისი არ არის პროცესი დასრულებულია, თანაც წარმატებით
        if (!string.IsNullOrWhiteSpace(serviceName))
            return null;

        //თუ სერვისია, გავუშვათ ეს სერვისი და დავრწმუნდეთ, რომ გაეშვა.
        var startResult = await Start(serviceEnvName, cancellationToken);
        if (startResult.IsNone)
            return null;

        //თუ სერვისი არ გაეშვა, ვაბრუნებთ შეტყობინებას
        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName, $"Service {projectName} can not started",
                cancellationToken);
        Logger.LogError("Service {projectName} can not started", projectName);
        return Err.RecreateErrors((Err[])startResult,
            new()
            {
                ErrorCode = "ServiceProjectNameCanNotStarted", ErrorMessage = $"Service {projectName} can not started"
            });
        ;
    }

    private async Task<OneOf<string, Err[]>> CheckBeforeStartUpdate(string projectName, string installFolder,
        string environmentName, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(installFolder))
        {
            if (MessagesDataManager is not null)
                await MessagesDataManager.SendMessage(UserName,
                    $"Installer install folder {installFolder} does not exists", cancellationToken);
            Logger.LogError("Installer install folder {installFolder} does not exists", installFolder);
            return new Err[]
            {
                new()
                {
                    ErrorCode = "InstallerFolderDoesNotExists",
                    ErrorMessage = $"Installer install folder {installFolder} does not exists"
                }
            };
        }

        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName, $"Installer install folder is {installFolder}",
                cancellationToken);
        Logger.LogInformation("Installer install folder is {installFolder}", installFolder);

        var projectInstallFullPath = Path.Combine(installFolder, projectName, environmentName);
        if (!Directory.Exists(projectInstallFullPath))
        {
            if (MessagesDataManager is not null)
                await MessagesDataManager.SendMessage(UserName,
                    $"Project install folder {projectInstallFullPath} does not exists",
                    cancellationToken);
            Logger.LogError("Project install folder {projectInstallFullPath} does not exists", projectInstallFullPath);
            return new Err[]
            {
                new()
                {
                    ErrorCode = "ProjectInstallFolderDoesNotExists",
                    ErrorMessage = $"Project install folder {projectInstallFullPath} does not exists"
                }
            };
        }

        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName, $"Project install folder is {projectInstallFullPath}",
                cancellationToken);
        Logger.LogInformation("Project install folder is {projectInstallFullPath}", projectInstallFullPath);
        return projectInstallFullPath;
    }

    public async Task<OneOf<string?, Err[]>> RunUpdateService(string archiveFileName, string projectName,
        string? serviceName, string environmentName, FileNameAndTextContent? appSettingsFile, string serviceUserName,
        string filesUserName, string filesUsersGroupName, string installWorkFolder, string installFolder,
        string? serviceDescriptionSignature, string? projectDescription, CancellationToken cancellationToken)
    {
        //დავადგინოთ არსებობს თუ არა {_workFolder} სახელით ქვეფოლდერი სამუშაო ფოლდერში
        //და თუ არ არსებობს, შევქმნათ
        var checkedWorkFolder = FileStat.CreateFolderIfNotExists(installWorkFolder, UseConsole);
        if (checkedWorkFolder == null)
        {
            if (MessagesDataManager is not null)
                await MessagesDataManager.SendMessage(UserName,
                        $"Installer work folder {installWorkFolder} does not created",
                        cancellationToken)
                    ;
            Logger.LogError("Installer work folder {installWorkFolder} does not created", installWorkFolder);
            return new Err[]
            {
                new()
                {
                    ErrorCode = "InstallerWorkFolderDoesNotCreated",
                    ErrorMessage = $"Installer work folder {installWorkFolder} does not created"
                }
            };
        }

        var projectInstallFolder = Path.Combine(installFolder, projectName);

        var checkedProjectInstallFolder = FileStat.CreateFolderIfNotExists(projectInstallFolder, UseConsole);
        if (checkedProjectInstallFolder == null)
        {
            if (MessagesDataManager is not null)
                await MessagesDataManager.SendMessage(UserName,
                        $"Installer install folder {installFolder} does not created",
                        cancellationToken)
                    ;
            Logger.LogError("Installer install folder {installFolder} does not created", installFolder);
            return new Err[]
            {
                new()
                {
                    ErrorCode = "InstallerInstallFolderDoesNotCreated",
                    ErrorMessage = $"Installer work install {installWorkFolder} does not created"
                }
            };
        }

        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName,
                $"Installer project install folder is {checkedProjectInstallFolder}",
                cancellationToken);
        Logger.LogInformation("Installer project install folder is {checkedProjectInstallFolder}",
            checkedProjectInstallFolder);

        //გავშალოთ არქივი სამუშაო ფოლდერში, იმისათვის, რომ დავრწმუნდეთ,
        //რომ არქივი დაზიანებული არ არის და ყველა ფაილის გახსნა ხერხდება
        //ZipClassArchiver zipClassArchiver = new ZipClassArchiver(_logger, outputFolderPath, zipFileFullName);
        var folderName = Path.GetFileNameWithoutExtension(archiveFileName);
        var projectFilesFolderFullName = Path.Combine(checkedWorkFolder, folderName);
        var archiveFileFullName = Path.Combine(checkedWorkFolder, archiveFileName);

        if (Directory.Exists(projectFilesFolderFullName))
        {
            if (MessagesDataManager is not null)
                await MessagesDataManager.SendMessage(UserName,
                    $"Delete Existing Project files in {projectFilesFolderFullName}",
                    cancellationToken);
            Logger.LogInformation("Delete Existing Project files in {projectFilesFolderFullName}",
                projectFilesFolderFullName);
            Directory.Delete(projectFilesFolderFullName, true);
        }

        ZipFile.ExtractToDirectory(archiveFileFullName, projectFilesFolderFullName);

        if (!Directory.Exists(projectFilesFolderFullName))
        {
            if (MessagesDataManager is not null)
                await MessagesDataManager.SendMessage(UserName,
                    $"Project files is not extracted to {projectFilesFolderFullName}",
                    cancellationToken);
            Logger.LogInformation("Project files is not extracted to {projectFilesFolderFullName}",
                projectFilesFolderFullName);
            return new Err[]
            {
                new()
                {
                    ErrorCode = "ProjectFilesIsNotExtracted",
                    ErrorMessage = $"Project files is not extracted to {projectFilesFolderFullName}"
                }
            };
        }

        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName,
                    $"Project files is extracted to {projectFilesFolderFullName}",
                    cancellationToken)
                ;
        Logger.LogInformation("Project files is extracted to {projectFilesFolderFullName}", projectFilesFolderFullName);

        //ZipClassArchiver zipClassArchiver = new ZipClassArchiver(_logger);
        //zipClassArchiver.ArchiveToPath(archiveFileFullName, projectFilesFolderFullName);

        //წავშალოთ გახსნილი არქივი, რადგან ის აღარ გვჭირდება
        //(შეიძლება ისე გავაკეთო, რომ არ წავშალო არქივი, რადგან მოქაჩვას შეიძლება დრო სჭირდებოდეს
        //ასეთ შემთხვევაში უნდა შევინარჩუნო არქივების ლიმიტირებული რაოდენობა
        //და ამ რაოდენობაზე მეტი რაც იქნება, უნდა წაიშალოს)
        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName, $"Deleting {archiveFileFullName} file...",
                    cancellationToken)
                ;
        Logger.LogInformation("Deleting {archiveFileFullName} file...", archiveFileFullName);
        //წაიშალოს ლოკალური ფაილი
        File.Delete(archiveFileFullName);

        //დავადგინოთ პროგრამის ვერსია და დავაბრუნოთ
        var projectMainExeFileName = Path.Combine(projectFilesFolderFullName, $"{projectName}.dll");

        var assemblyVersion = Assembly.LoadFile(projectMainExeFileName).GetName().Version?.ToString();


        var serviceEnvName = GetServiceEnvName(serviceName, environmentName);
        var serviceExists = false;
        if (!string.IsNullOrWhiteSpace(serviceName))
        {
            //დავადგინოთ არსებობს თუ არა სერვისების სიაში სერვისი სახელით {serviceEnvName}
            serviceExists = IsServiceExists(serviceEnvName);
            if (serviceExists)
            {
                if (MessagesDataManager is not null)
                    await MessagesDataManager.SendMessage(UserName, $"Service {serviceEnvName} is exists",
                        cancellationToken);
                Logger.LogInformation("Service {serviceEnvName} is exists", serviceEnvName);
            }
            else
            {
                if (MessagesDataManager is not null)
                    await MessagesDataManager.SendMessage(UserName, $"Service {serviceEnvName} does not exists",
                        cancellationToken);
                Logger.LogInformation("Service {serviceEnvName} does not exists", serviceEnvName);
            }

            //თუ სიაში არსებობს დავადგინოთ გაშვებულია თუ არა სერვისი.
            var serviceIsRunning = IsServiceRunning(serviceEnvName);
            if (serviceIsRunning)
            {
                if (MessagesDataManager is not null)
                    await MessagesDataManager.SendMessage(UserName, $"Service {serviceEnvName} is running",
                        cancellationToken);
                Logger.LogInformation("Service {serviceEnvName} is running", serviceEnvName);
            }
            else
            {
                if (MessagesDataManager is not null)
                    await MessagesDataManager.SendMessage(UserName, $"Service {serviceEnvName} does not running",
                        cancellationToken);
                Logger.LogInformation("Service {serviceEnvName} does not running", serviceEnvName);
            }

            if (!serviceExists && serviceIsRunning)
            {
                if (MessagesDataManager is not null)
                    await MessagesDataManager.SendMessage(UserName,
                        $"Service {serviceEnvName} does not exists, but process is running",
                        cancellationToken);
                Logger.LogError("Service {serviceEnvName} does not exists, but process is running", serviceEnvName);
                return new Err[]
                {
                    new()
                    {
                        ErrorCode = "ServiceDoesNotExists",
                        ErrorMessage = $"Service {serviceEnvName} does not exists, but process is running"
                    }
                };
            }

            //თუ სერვისი გაშვებულია უკვე, გავაჩეროთ
            if (MessagesDataManager is not null)
                await MessagesDataManager.SendMessage(UserName, $"Try to stop Service {serviceEnvName}",
                        cancellationToken)
                    ;
            Logger.LogInformation("Try to stop Service {serviceEnvName}", serviceEnvName);
            var stopResult = await Stop(serviceEnvName, cancellationToken);
            if (stopResult.IsSome)
            {
                if (MessagesDataManager is not null)
                    await MessagesDataManager.SendMessage(UserName, $"Service {serviceEnvName} does not stopped",
                        cancellationToken);
                Logger.LogError("Service {serviceEnvName} does not stopped", serviceEnvName);
                return Err.RecreateErrors((Err[])stopResult,
                    new Err
                    {
                        ErrorCode = "ServiceDoesNotStopped", ErrorMessage = $"Service {serviceEnvName} does not stopped"
                    });
            }

            if (IsProcessRunning(serviceEnvName))
            {
                if (MessagesDataManager is not null)
                    await MessagesDataManager.SendMessage(UserName,
                            $"Process {serviceEnvName} is running and cannot be updated.",
                            cancellationToken)
                        ;
                Logger.LogError("Process {serviceEnvName} is running and cannot be updated.", serviceEnvName);
                return new Err[]
                {
                    new()
                    {
                        ErrorCode = "ProcessIsRunningAndCannotBeUpdated",
                        ErrorMessage = $"Process {serviceEnvName} is running and cannot be updated"
                    }
                };
            }
        }

        //თუ არსებობს, წაიშალოს არსებული ფაილები.
        //თუ არსებობს, დავაარქივოთ და გადავინახოთ პროგრამის მიმდინარე ფაილები
        //(ეს კეთდება იმისათვის, რომ შესაძლებელი იყოს წინა ვერსიაზე სწრაფად დაბრუნება)
        //რადგან გადანახვა ხდება, ზედმეტი ფაილები რომ არ დაგროვდეს, წავშალოთ წინა გადანახულები,
        //ოღონდ არ წავშალოთ ბოლო რამდენიმე. (რაოდენობა პარამეტრებით უნდა იყოს განსაზღვრული)
        var deleteSuccess = true;
        var projectInstallFullPath = Path.Combine(checkedProjectInstallFolder, environmentName);

        if (Directory.Exists(projectInstallFullPath))
        {
            deleteSuccess = false;
            if (MessagesDataManager is not null)
                await MessagesDataManager.SendMessage(UserName, $"Folder {projectInstallFullPath} already exists",
                    cancellationToken);
            Logger.LogInformation("Folder {projectInstallFullPath} already exists", projectInstallFullPath);

            var tryCount = 0;
            while (!deleteSuccess && tryCount < 10)
            {
                tryCount++;
                try
                {
                    if (MessagesDataManager is not null)
                        await MessagesDataManager.SendMessage(UserName,
                            $"Try to delete folder {projectInstallFullPath} {tryCount}...",
                            cancellationToken);
                    Logger.LogInformation("Try to delete folder {projectInstallFullPath} {tryCount}...",
                        projectInstallFullPath, tryCount);
                    Directory.Delete(projectInstallFullPath, true);
                    if (MessagesDataManager is not null)
                        await MessagesDataManager.SendMessage(UserName,
                            $"Folder {projectInstallFullPath} deleted successfully",
                            cancellationToken);
                    Logger.LogInformation("Folder {projectInstallFullPath} deleted successfully",
                        projectInstallFullPath);
                    deleteSuccess = true;
                }
                catch
                {
                    if (MessagesDataManager is not null)
                        await MessagesDataManager.SendMessage(UserName,
                                $"Folder {projectInstallFullPath} could not deleted on try {tryCount}",
                                cancellationToken)
                            ;
                    Logger.LogWarning("Folder {projectInstallFullPath} could not deleted on try {tryCount}",
                        projectInstallFullPath, tryCount);
                    if (MessagesDataManager is not null)
                        await MessagesDataManager.SendMessage(UserName, "waiting for 3 seconds...", cancellationToken)
                            ;
                    Logger.LogInformation("waiting for 3 seconds...");
                    Thread.Sleep(3000);
                }
            }
        }

        if (!deleteSuccess)
        {
            if (MessagesDataManager is not null)
                await MessagesDataManager.SendMessage(UserName, $"folder {projectInstallFullPath} can not be Deleted",
                    cancellationToken);
            Logger.LogError("folder {projectInstallFullPath} can not be Deleted", projectInstallFullPath);
            return new Err[]
            {
                new()
                {
                    ErrorCode = "FolderCanNotBeDeleted",
                    ErrorMessage = $"folder {projectInstallFullPath} can not be Deleted"
                }
            };
        }

        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName,
                    $"Install {projectName} files to {projectInstallFullPath}...",
                    cancellationToken)
                ;
        Logger.LogInformation("Install {projectName} files to {projectInstallFullPath}...", projectName,
            projectInstallFullPath);

        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName,
                    $"Move Files from {projectFilesFolderFullName} to {projectInstallFullPath}...",
                    cancellationToken)
                ;
        Logger.LogInformation("Move Files from {projectFilesFolderFullName} to {projectInstallFullPath}...",
            projectFilesFolderFullName,
            projectInstallFullPath);
        //გაშლილი არქივის ფაილები გადავიტანოთ სერვისის ფოლდერში
        Directory.Move(projectFilesFolderFullName, projectInstallFullPath);


        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName, $"WriteAllTextToPath {projectInstallFullPath}...",
                    cancellationToken)
                ;
        Logger.LogInformation("WriteAllTextToPath {projectInstallFullPath}...", projectInstallFullPath);
        //ჩავაგდოთ პარამეტრების ფაილი ახლადდაინსტალირებულ ფოლდერში
        appSettingsFile?.WriteAllTextToPath(projectInstallFullPath);

        var changeOwnerResult =
            await ChangeOwner(projectInstallFullPath, filesUserName, filesUsersGroupName, cancellationToken);
        if (changeOwnerResult.IsSome)
        {
            if (MessagesDataManager is not null)
                await MessagesDataManager.SendMessage(UserName,
                        $"folder {projectInstallFullPath} owner can not be changed",
                        cancellationToken)
                    ;
            Logger.LogError("folder {projectInstallFullPath} owner can not be changed", projectInstallFullPath);
            return Err.RecreateErrors((Err[])changeOwnerResult,
                new Err
                {
                    ErrorCode = "FolderOwnerCanNotBeChanged",
                    ErrorMessage = $"Folder {projectInstallFullPath} owner can not be changed"
                });
        }

        if (string.IsNullOrWhiteSpace(serviceName))
            return assemblyVersion;

        //თუ სერვისი უკვე დარეგისტრირებულია, შევამოწმოთ სწორად არის თუ არა დარეგისტრირებული.
        if (serviceExists)
        {
            var isServiceRegisteredProperlyResult = await IsServiceRegisteredProperly(projectName, serviceEnvName,
                serviceUserName, projectInstallFullPath, serviceDescriptionSignature, projectDescription,
                cancellationToken);
            if (isServiceRegisteredProperlyResult.IsT1)
                return isServiceRegisteredProperlyResult.AsT1;

            if (!isServiceRegisteredProperlyResult.AsT0)
                if (RemoveService(serviceEnvName))
                    serviceExists = false;
        }

        //თუ სერვისი არ არის დარეგისტრირებული და პლატფორმა მოითხოვს დარეგისტრირებას, დავარეგისტრიროთ
        if (!serviceExists)
        {
            if (MessagesDataManager is not null)
                await MessagesDataManager.SendMessage(UserName, $"registering service {serviceEnvName}...",
                    cancellationToken);
            Logger.LogInformation("registering service {serviceEnvName}...", serviceEnvName);

            var registerServiceResult = await RegisterService(projectName, serviceEnvName, serviceUserName,
                projectInstallFullPath, serviceDescriptionSignature, projectDescription, cancellationToken);

            if (registerServiceResult.IsSome)
            {
                if (MessagesDataManager is not null)
                    await MessagesDataManager.SendMessage(UserName, $"cannot register Service {serviceEnvName}",
                        cancellationToken);
                Logger.LogError("cannot register Service {serviceEnvName}", serviceEnvName);
                return Err.RecreateErrors((Err[])registerServiceResult,
                    new Err
                    {
                        ErrorCode = "CannotRegisterService", ErrorMessage = $"cannot register Service {serviceEnvName}"
                    });
            }
        }

        //გავუშვათ სერვისი და დავრწმუნდეთ, რომ გაეშვა.
        var startResult = await Start(serviceEnvName, cancellationToken);
        if (startResult.IsNone)
            return assemblyVersion;

        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName, $"Service {serviceEnvName} can not started",
                    cancellationToken);
        Logger.LogError("Service {serviceEnvName} can not be started", serviceEnvName);
        return Err.RecreateErrors((Err[])startResult,
            new()
            {
                ErrorCode = "ServiceCanNotBeStarted", ErrorMessage = $"Service {serviceEnvName} can not be started"
            });
    }

    public async Task<OneOf<string?, Err[]>> RunUpdateApplication(string archiveFileName, string projectName,
        string environmentName, string filesUserName, string filesUsersGroupName, string installWorkFolder,
        string installFolder, CancellationToken cancellationToken)
    {
        //დავადგინოთ არსებობს თუ არა {_workFolder} სახელით ქვეფოლდერი სამუშაო ფოლდერში
        //და თუ არ არსებობს, შევქმნათ
        var checkedWorkFolder = FileStat.CreateFolderIfNotExists(installWorkFolder, UseConsole);
        if (checkedWorkFolder == null)
        {
            if (MessagesDataManager is not null)
                await MessagesDataManager.SendMessage(UserName,
                        $"Installer work folder {installWorkFolder} does not created",
                        cancellationToken)
                    ;
            Logger.LogError("Installer work folder {installWorkFolder} does not created", installWorkFolder);
            return new Err[]
            {
                new()
                {
                    ErrorCode = "InstallerWorkFolderDoesNotCreated",
                    ErrorMessage = $"Installer work folder {installWorkFolder} does not created"
                }
            };
        }

        var checkedInstallFolder = FileStat.CreateFolderIfNotExists(installFolder, UseConsole);
        if (checkedInstallFolder == null)
        {
            if (MessagesDataManager is not null)
                await MessagesDataManager.SendMessage(UserName,
                        $"Installer install folder {installFolder} does not created",
                        cancellationToken)
                    ;
            Logger.LogError("Installer install folder {installFolder} does not created", installFolder);
            return new Err[]
            {
                new()
                {
                    ErrorCode = "InstallerInstallFolderDoesNotCreated",
                    ErrorMessage = $"Installer install folder {installWorkFolder} does not created"
                }
            };
        }

        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName, $"Installer install folder is {checkedInstallFolder}",
                cancellationToken);
        Logger.LogInformation("Installer install folder is {checkedInstallFolder}", checkedInstallFolder);

        //გავშალოთ არქივი სამუშაო ფოლდერში, იმისათვის, რომ დავრწმუნდეთ,
        //რომ არქივი დაზიანებული არ არის და ყველა ფაილის გახსნა ხერხდება
        //ZipClassArchiver zipClassArchiver = new ZipClassArchiver(_logger, outputFolderPath, zipFileFullName);
        var folderName = Path.GetFileNameWithoutExtension(archiveFileName);
        var projectFilesFolderFullName = Path.Combine(checkedWorkFolder, folderName);
        var archiveFileFullName = Path.Combine(checkedWorkFolder, archiveFileName);

        if (Directory.Exists(projectFilesFolderFullName))
        {
            if (MessagesDataManager is not null)
                await MessagesDataManager.SendMessage(UserName,
                    $"Delete Existing Project files in {projectFilesFolderFullName}",
                    cancellationToken);
            Logger.LogInformation("Delete Existing Project files in {projectFilesFolderFullName}",
                projectFilesFolderFullName);
            Directory.Delete(projectFilesFolderFullName, true);
        }

        ZipFile.ExtractToDirectory(archiveFileFullName, projectFilesFolderFullName);

        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName,
                    $"Project files is extracted to {projectFilesFolderFullName}",
                    cancellationToken)
                ;
        Logger.LogInformation("Project files is extracted to {projectFilesFolderFullName}", projectFilesFolderFullName);

        //ZipClassArchiver zipClassArchiver = new ZipClassArchiver(_logger);
        //zipClassArchiver.ArchiveToPath(archiveFileFullName, projectFilesFolderFullName);

        //წავშალოთ გახსნილი არქივი, რადგან ის აღარ გვჭირდება
        //(შეიძლება ისე გავაკეთო, რომ არ წავშალო არქივი, რადგან მოქაჩვას შეიძლება დრო სჭირდებოდეს
        //ასეთ შემთხვევაში უნდა შევინარჩუნო არქივების ლიმიტირებული რაოდენობა
        //და ამ რაოდენობაზე მეტი რაც იქნება, უნდა წაიშალოს)
        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName, $"Deleting {archiveFileFullName} file...",
                    cancellationToken)
                ;
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
        var projectInstallFullPath = Path.Combine(checkedInstallFolder, projectName, environmentName);

        if (Directory.Exists(projectInstallFullPath))
        {
            deleteSuccess = false;
            if (MessagesDataManager is not null)
                await MessagesDataManager.SendMessage(UserName, $"Folder {projectInstallFullPath} already exists",
                    cancellationToken);
            Logger.LogInformation("Folder {projectInstallFullPath} already exists", projectInstallFullPath);

            var tryCount = 0;
            while (!deleteSuccess && tryCount < 10)
            {
                tryCount++;
                try
                {
                    if (MessagesDataManager is not null)
                        await MessagesDataManager.SendMessage(UserName,
                            $"Try to delete folder {projectInstallFullPath} {tryCount}...",
                            cancellationToken);
                    Logger.LogInformation("Try to delete folder {projectInstallFullPath} {tryCount}...",
                        projectInstallFullPath, tryCount);
                    Directory.Delete(projectInstallFullPath, true);
                    if (MessagesDataManager is not null)
                        await MessagesDataManager.SendMessage(UserName,
                            $"Folder {projectInstallFullPath} deleted successfully",
                            cancellationToken);
                    Logger.LogInformation("Folder {projectInstallFullPath} deleted successfully",
                        projectInstallFullPath);
                    deleteSuccess = true;
                }
                catch
                {
                    if (MessagesDataManager is not null)
                        await MessagesDataManager.SendMessage(UserName,
                                $"Folder {projectInstallFullPath} could not deleted on try {tryCount}",
                                cancellationToken)
                            ;
                    Logger.LogWarning("Folder {projectInstallFullPath} could not deleted on try {tryCount}",
                        projectInstallFullPath, tryCount);
                    if (MessagesDataManager is not null)
                        await MessagesDataManager.SendMessage(UserName, "waiting for 3 seconds...", cancellationToken)
                            ;
                    Logger.LogInformation("waiting for 3 seconds...");
                    Thread.Sleep(3000);
                }
            }
        }

        if (!deleteSuccess)
        {
            if (MessagesDataManager is not null)
                await MessagesDataManager.SendMessage(UserName, $"folder {projectInstallFullPath} can not Deleted",
                    cancellationToken);
            Logger.LogError("folder {projectInstallFullPath} can not Deleted", projectInstallFullPath);
            return new Err[]
            {
                new()
                {
                    ErrorCode = "FolderCanNotBeDeleted",
                    ErrorMessage = $"folder {projectInstallFullPath} can not be Deleted"
                }
            };
        }

        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName,
                    $"Install {projectName} files to {projectInstallFullPath}...",
                    cancellationToken)
                ;
        Logger.LogInformation("Install {projectName} files to {projectInstallFullPath}...", projectName,
            projectInstallFullPath);
        //გაშლილი არქივის ფაილები გადავიტანოთ სერვისის ფოლდერში
        Directory.Move(projectFilesFolderFullName, projectInstallFullPath);

        var changeOwnerResult =
            await ChangeOwner(projectInstallFullPath, filesUserName, filesUsersGroupName, cancellationToken);
        if (changeOwnerResult.IsNone)
            return assemblyVersion;

        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName, $"folder {projectInstallFullPath} owner can not be changed",
                    cancellationToken)
                ;
        Logger.LogError("folder {projectInstallFullPath} owner can not be changed", projectInstallFullPath);
        return new Err[]
        {
            new()
            {
                ErrorCode = "FolderOwnerCanNotBeChanged",
                ErrorMessage = $"folder {projectInstallFullPath} owner can not be changed"
            }
        };

    }

    public async Task<Option<Err[]>> Stop(string? serviceName, string environmentName,
        CancellationToken cancellationToken)
    {
        return await Stop(GetServiceEnvName(serviceName, environmentName), cancellationToken);
    }

    private async Task LogInfoAndSendMessage(string message, CancellationToken cancellationToken)
    {
        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName, message,
                cancellationToken);
        Logger.LogInformation(message);

    }

    private async Task<Err[]> LogInfoAndSendMessageFromError(string errorCode, string message, CancellationToken cancellationToken)
    {
        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName, message, cancellationToken);
        Logger.LogInformation(message);
        return new Err[]
        {
            new()
            {
                ErrorCode = errorCode,
                ErrorMessage = message
            }
        };
    }

    private async Task<Err[]> LogInfoAndSendMessageFromError(Err error, CancellationToken cancellationToken)
    {
        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName, error.ErrorMessage, cancellationToken);
        Logger.LogInformation(error.ErrorMessage);
        return new[] {error};
    }

    private async Task<Option<Err[]>> Stop(string serviceEnvName, CancellationToken cancellationToken)
    {
        //დავადგინოთ არსებობს თუ არა სერვისების სიაში სერვისი სახელით {serviceEnvName}
        var serviceExists = IsServiceExists(serviceEnvName);
        if (serviceExists)
            await LogInfoAndSendMessage($"Service {serviceEnvName} is exists", cancellationToken);
        else
            return await LogInfoAndSendMessageFromError("ServiceDoesNotExists", $"Service {serviceEnvName} does not exists", cancellationToken);

        var serviceIsRunning = IsServiceRunning(serviceEnvName);
        if (!serviceIsRunning)
        {
            await LogInfoAndSendMessage($"Service {serviceEnvName} is not running", cancellationToken);
            return null;
        }
        await LogInfoAndSendMessage($"Service {serviceEnvName} is running", cancellationToken);

        var stopServiceResult = await StopService(serviceEnvName, cancellationToken);
        if (stopServiceResult.IsNone)
            return stopServiceResult;

        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName, $"Service {serviceEnvName} can not stopped",
                cancellationToken);
        Logger.LogError("Service {serviceEnvName} can not stopped", serviceEnvName);
        return new Err[]
        {
            new()
            {
                ErrorCode = "ServiceCanNotStopped",
                ErrorMessage = $"Service {serviceEnvName} can not stopped"
            }
        };
    }

    public async Task<Option<Err[]>> Start(string? serviceName, string environmentName,
        CancellationToken cancellationToken)
    {
        return await Start(GetServiceEnvName(serviceName, environmentName), cancellationToken);
    }

    private async Task<Option<Err[]>> Start(string serviceEnvName, CancellationToken cancellationToken)
    {
        var serviceIsRunning = IsServiceRunning(serviceEnvName);
        if (serviceIsRunning)
        {
            if (MessagesDataManager is not null)
                await MessagesDataManager.SendMessage(UserName, $"Service {serviceEnvName} is running",
                        cancellationToken);
            Logger.LogInformation("Service {serviceEnvName} is running", serviceEnvName);
            return null;
        }

        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName, $"Service {serviceEnvName} does not running",
                cancellationToken);

        Logger.LogInformation("Service {serviceEnvName} does not running", serviceEnvName);

        var startServiceResult = await StartService(serviceEnvName, cancellationToken);
        if (startServiceResult.IsNone)
            return null;

        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName, $"Service {serviceEnvName} can not started",
                cancellationToken);

        Logger.LogError("Service {serviceEnvName} can not started", serviceEnvName);

        return Err.RecreateErrors((Err[])startServiceResult,
            new()
            {
                ErrorCode = "ServiceCanNotBeStarted", ErrorMessage = $"Service {serviceEnvName} can not be started"
            });
    }

    public async Task<Option<Err[]>> RemoveProjectAndService(string projectName, string serviceName,
        string environmentName, string installFolder, CancellationToken cancellationToken)
    {
        var serviceEnvName = GetServiceEnvName(serviceName, environmentName);

        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName, $"Remove service {serviceEnvName} started...",
                cancellationToken);
        Logger.LogInformation("Remove service {serviceEnvName} started...", serviceEnvName);

        var serviceExists = IsServiceExists(serviceEnvName);
        if (serviceExists)
        {
            if (MessagesDataManager is not null)
                await MessagesDataManager.SendMessage(UserName, $"Service {serviceEnvName} is exists",
                        cancellationToken)
                    ;
            Logger.LogInformation("Service {serviceEnvName} is exists", serviceEnvName);
        }
        else
        {
            if (MessagesDataManager is not null)
                await MessagesDataManager.SendMessage(UserName, $"Service {serviceEnvName} does not exists",
                    cancellationToken);
            Logger.LogInformation("Service {serviceEnvName} does not exists", serviceEnvName);
        }

        var serviceIsRunning = false;
        if (serviceExists)
        {
            serviceIsRunning = IsServiceRunning(serviceEnvName);


            if (serviceIsRunning)
            {
                if (MessagesDataManager is not null)
                    await MessagesDataManager.SendMessage(UserName, $"Service {serviceEnvName} is running",
                        cancellationToken);
                Logger.LogInformation("Service {serviceEnvName} is running", serviceEnvName);
            }
            else
            {
                if (MessagesDataManager is not null)
                    await MessagesDataManager.SendMessage(UserName, $"Service {serviceEnvName} does not running",
                        cancellationToken);
                Logger.LogInformation("Service {serviceEnvName} does not running", serviceEnvName);
            }
        }


        if (serviceIsRunning)
        {
            var stopResult = await Stop(serviceEnvName, cancellationToken);
            if (stopResult.IsSome)
            {
                if (MessagesDataManager is not null)
                    await MessagesDataManager.SendMessage(UserName, $"Service {serviceEnvName} can not be stopped",
                        cancellationToken);
                Logger.LogError("Service {serviceEnvName} can not be stopped", serviceEnvName);
                return Err.RecreateErrors((Err[])stopResult,
                    new Err
                    {
                        ErrorCode = "ServiceCanNotBeStopped",
                        ErrorMessage = $"Service {serviceEnvName} can not be stopped"
                    });
            }
        }

        if (!serviceExists)
            return await RemoveProject(projectName, environmentName, installFolder, cancellationToken);

        if (RemoveService(serviceEnvName))
            return await RemoveProject(projectName, environmentName, installFolder, cancellationToken);

        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName, $"Service {serviceEnvName} can not be Removed",
                cancellationToken);
        Logger.LogError("Service {serviceEnvName} can not be Removed", serviceEnvName);
        return new Err[]
        {
            new()
            {
                ErrorCode = "ServiceCanNotBeRemoved",
                ErrorMessage = $"Service {serviceEnvName} can not be Removed"
            }
        };
    }

    public async Task<Option<Err[]>> RemoveProject(string projectName, string environmentName, string installFolder,
        CancellationToken cancellationToken)
    {
        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName, $"Remove project {projectName} started...",
                    cancellationToken)
                ;
        Logger.LogInformation("Remove project {projectName} started...", projectName);
        var checkedInstallFolder = FileStat.CreateFolderIfNotExists(installFolder, UseConsole);
        if (checkedInstallFolder == null)
        {
            if (MessagesDataManager is not null)
                await MessagesDataManager.SendMessage(UserName, "Installation folder does not found", cancellationToken)
                    ;
            Logger.LogError("Installation folder does not found");
            return new Err[]
            {
                new()
                {
                    ErrorCode = "InstallationFolderDoesNotFound",
                    ErrorMessage = "Installation folder does not found"
                }
            };
        }

        //თუ არსებობს, წაიშალოს არსებული ფაილები.
        var projectInstallFullPath = Path.Combine(checkedInstallFolder, projectName, environmentName);

        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName, $"Deleting files {projectName}...", cancellationToken);
        Logger.LogInformation("Deleting files {projectName}...", projectName);

        if (Directory.Exists(projectInstallFullPath))
            Directory.Delete(projectInstallFullPath, true);

        return null;
    }
}