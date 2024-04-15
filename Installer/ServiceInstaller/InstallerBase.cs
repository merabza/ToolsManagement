using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Installer.Domain;
using Installer.ErrorModels;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OneOf;
using SystemToolsShared;

// ReSharper disable ConvertToPrimaryConstructor

namespace Installer.ServiceInstaller;

public /*open*/ abstract class InstallerBase : MessageLogger
{
    public readonly string Runtime;
    protected readonly bool UseConsole;

    protected InstallerBase(bool useConsole, ILogger logger, string runtime, IMessagesDataManager? messagesDataManager,
        string? userName) : base(logger, messagesDataManager, userName)
    {
        Runtime = runtime;
        UseConsole = useConsole;
    }

    protected abstract Task<OneOf<bool, Err[]>> IsServiceRegisteredProperly(string projectName, string serviceEnvName,
        string userName, string installFolderPath, string? serviceDescriptionSignature, string? projectDescription,
        CancellationToken cancellationToken);

    protected abstract Task<Option<Err[]>> ChangeOneFileOwner(string filePath, string? filesUserName,
        string? filesUsersGroupName, CancellationToken cancellationToken);

    protected abstract Task<Option<Err[]>> ChangeFolderOwner(string folderPath, string filesUserName,
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
                await LogWarningAndSendMessage("Parameters file is already in latest version and not needs update",
                    cancellationToken);
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
                await LogInfoAndSendMessage("Service {0} is exists", serviceEnvName, cancellationToken);
            else
                //ეს არის პარამეტრების განახლების პროცესი, ამიტომ თუ პროგრამა სერვისია და ეს სერვისი არ არსებობს განახლება ვერ მოხდება
                //ასეთ შემთხვევაში უნდა გაეშვას უკვე მთლიანი პროგრამის განახლების პროცესი
                return await LogErrorAndSendMessageFromError(InstallerErrors.ServiceIsNotExists(serviceEnvName),
                    cancellationToken);

            //თუ სერვისი გაშვებულია უკვე, გავაჩეროთ
            await LogInfoAndSendMessage("Try to stop Service {0}", serviceEnvName, cancellationToken);
            var stopResult = await Stop(serviceEnvName, cancellationToken);
            if (stopResult.IsSome)
                return (Err[])stopResult;
        }
        else if (IsProcessRunning(projectName))
            //თუ სერვისი არ არის და პროგრამა მაინც გაშვებულია,
            //ასეთ შემთხვევაში პარამეტრების ფაილს ვერ გავაახლებთ,
            //რადგან გაშვებული პროგრამა ვერ მიხვდება, რომ ახალი პარამეტრები უნდა გამოიყენოს.
            //ასეთ შემთხვევაში ჯერ უნდა გაჩერდეს პროგრამა და მერე უნდა განახლდეს პარამეტრები.
            return await LogErrorAndSendMessageFromError(
                InstallerErrors.ProcessIsRunningAndCannotBeUpdated(projectName),
                cancellationToken);

        //შევეცადოთ პარამეტრების ფაილის წაშლა
        var appSettingsFileDeletedSuccess = true;
        if (File.Exists(appSettingsFileFullPath))
        {
            appSettingsFileDeletedSuccess = false;
            await LogInfoAndSendMessage("File {0} is exists", appSettingsFileFullPath, cancellationToken);

            var tryCount = 0;
            while (!appSettingsFileDeletedSuccess && tryCount < 10)
            {
                tryCount++;
                try
                {
                    await LogInfoAndSendMessage("Try to delete File {0} {1}...", appSettingsFileFullPath, tryCount,
                        cancellationToken);
                    File.Delete(appSettingsFileFullPath);
                    await LogInfoAndSendMessage("File {0} deleted successfully", appSettingsFileFullPath,
                        cancellationToken);
                    appSettingsFileDeletedSuccess = true;
                }
                catch
                {
                    await LogWarningAndSendMessage("File {0} could not deleted on try {1}", appSettingsFileFullPath,
                        tryCount, cancellationToken);
                    await LogInfoAndSendMessage("waiting for 3 seconds...", cancellationToken);
                    Thread.Sleep(3000);
                }
            }
        }

        if (!appSettingsFileDeletedSuccess)
            return await LogErrorAndSendMessageFromError(InstallerErrors.FileCanNotBeDeleted(appSettingsFileFullPath),
                cancellationToken);


        //შეიქმნას პარამეტრების ფაილი არსებულ ინფორმაციაზე დაყრდნობით
        await File.WriteAllTextAsync(appSettingsFileFullPath, appSettingsFileBody, cancellationToken);
        //შეიცვალოს პარამეტრების ფაილზე უფლებები საჭიროების მიხედვით.
        var changeOneFileOwnerResult = await ChangeOneFileOwner(appSettingsFileFullPath, filesUserName,
            filesUsersGroupName, cancellationToken);
        if (changeOneFileOwnerResult.IsSome)
        {
            return await LogErrorAndSendMessageFromError(
                InstallerErrors.FileOwnerCanNotBeChanged(appSettingsFileFullPath), cancellationToken);
        }

        //თუ ეს სერვისი არ არის პროცესი დასრულებულია, თანაც წარმატებით
        if (!string.IsNullOrWhiteSpace(serviceName))
            return null;

        //თუ სერვისია, გავუშვათ ეს სერვისი და დავრწმუნდეთ, რომ გაეშვა.
        var startResult = await Start(serviceEnvName, cancellationToken);
        if (startResult.IsNone)
            return null;

        //თუ სერვისი არ გაეშვა, ვაბრუნებთ შეტყობინებას
        return await LogErrorAndSendMessageFromError(InstallerErrors.ServiceCanNotBeStarted(projectName),
            cancellationToken);
    }

    private async Task<OneOf<string, Err[]>> CheckBeforeStartUpdate(string projectName, string installFolder,
        string environmentName, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(installFolder))
            return await LogErrorAndSendMessageFromError(InstallerErrors.InstallerFolderIsNotExists(installFolder),
                cancellationToken);

        await LogInfoAndSendMessage("Installer install folder is {0}", installFolder, cancellationToken);

        var projectInstallFullPath = Path.Combine(installFolder, projectName, environmentName);
        if (!Directory.Exists(projectInstallFullPath))
            return await LogErrorAndSendMessageFromError(
                InstallerErrors.ProjectInstallerFolderIsNotExists(projectName), cancellationToken);

        await LogInfoAndSendMessage("Project install folder is {0}", projectInstallFullPath, cancellationToken);

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
            return await LogErrorAndSendMessageFromError(
                InstallerErrors.InstallerWorkFolderDoesNotCreated(installWorkFolder), cancellationToken);

        var projectInstallFullPath = Path.Combine(installFolder, projectName, environmentName);

        var checkedProjectInstallFullPath = FileStat.CreateFolderIfNotExists(projectInstallFullPath, UseConsole);
        if (checkedProjectInstallFullPath == null)
            return await LogErrorAndSendMessageFromError(
                InstallerErrors.InstallerInstallFolderDoesNotCreated(projectInstallFullPath), cancellationToken);

        await LogInfoAndSendMessage("Installer project install folder is {0}", projectInstallFullPath,
            cancellationToken);

        //გავშალოთ არქივი სამუშაო ფოლდერში, იმისათვის, რომ დავრწმუნდეთ,
        //რომ არქივი დაზიანებული არ არის და ყველა ფაილის გახსნა ხერხდება
        var folderName = Path.GetFileNameWithoutExtension(archiveFileName);
        var projectFilesFolderFullName = Path.Combine(checkedWorkFolder, folderName);
        var archiveFileFullName = Path.Combine(checkedWorkFolder, archiveFileName);

        if (Directory.Exists(projectFilesFolderFullName))
        {
            await LogInfoAndSendMessage("Delete Existing Project files in {0}", projectFilesFolderFullName,
                cancellationToken);
            Directory.Delete(projectFilesFolderFullName, true);
        }

        ZipFile.ExtractToDirectory(archiveFileFullName, projectFilesFolderFullName);

        if (!Directory.Exists(projectFilesFolderFullName))
            return await LogErrorAndSendMessageFromError(
                InstallerErrors.ProjectFilesIsNotExtracted(projectFilesFolderFullName), cancellationToken);

        await LogInfoAndSendMessage("Project files is extracted to {0}", projectFilesFolderFullName, cancellationToken);

        //წავშალოთ გახსნილი არქივი, რადგან ის აღარ გვჭირდება
        //(შეიძლება ისე გავაკეთო, რომ არ წავშალო არქივი, რადგან მოქაჩვას შეიძლება დრო სჭირდებოდეს
        //ასეთ შემთხვევაში უნდა შევინარჩუნო არქივების ლიმიტირებული რაოდენობა
        //და ამ რაოდენობაზე მეტი რაც იქნება, უნდა წაიშალოს)
        await LogInfoAndSendMessage("Deleting {0} file...", archiveFileFullName, cancellationToken);

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
                await LogInfoAndSendMessage("Service {0} is exists", serviceEnvName, cancellationToken);
            else
                await LogInfoAndSendMessage("Service {0} is not exists", serviceEnvName, cancellationToken);

            //თუ სიაში არსებობს დავადგინოთ გაშვებულია თუ არა სერვისი.
            var serviceIsRunning = IsServiceRunning(serviceEnvName);
            if (serviceIsRunning)
                await LogInfoAndSendMessage("Service {0} is running", serviceEnvName, cancellationToken);
            else
                await LogInfoAndSendMessage("Service {0} is not running", serviceEnvName, cancellationToken);

            if (!serviceExists && serviceIsRunning)
                return await LogErrorAndSendMessageFromError(InstallerErrors.ServiceIsNotExists(serviceEnvName),
                    cancellationToken);

            //თუ სერვისი გაშვებულია უკვე, გავაჩეროთ
            await LogInfoAndSendMessage("Try to stop Service {0}", serviceEnvName, cancellationToken);

            var stopResult = await Stop(serviceEnvName, cancellationToken);
            if (stopResult.IsSome)
                return await LogErrorAndSendMessageFromError(InstallerErrors.ServiceIsNotStopped(serviceEnvName),
                    cancellationToken);

            if (IsProcessRunning(serviceEnvName))
                return await LogErrorAndSendMessageFromError(
                    InstallerErrors.ServiceIsRunningAndCannotBeUpdated(serviceEnvName), cancellationToken);
        }

        //თუ არსებობს, წაიშალოს არსებული ფაილები.
        //თუ არსებობს, დავაარქივოთ და გადავინახოთ პროგრამის მიმდინარე ფაილები
        //(ეს კეთდება იმისათვის, რომ შესაძლებელი იყოს წინა ვერსიაზე სწრაფად დაბრუნება)
        //რადგან გადანახვა ხდება, ზედმეტი ფაილები რომ არ დაგროვდეს, წავშალოთ წინა გადანახულები,
        //ოღონდ არ წავშალოთ ბოლო რამდენიმე. (რაოდენობა პარამეტრებით უნდა იყოს განსაზღვრული)
        var deleteSuccess = false;

        if (Directory.Exists(checkedProjectInstallFullPath))
        {
            await LogInfoAndSendMessage("Folder {0} already exists", checkedProjectInstallFullPath, cancellationToken);

            var tryCount = 0;
            while (!deleteSuccess && tryCount < 10)
            {
                tryCount++;
                try
                {
                    await LogInfoAndSendMessage("Try to delete folder {0} {1}...", checkedProjectInstallFullPath,
                        tryCount, cancellationToken);
                    Directory.Delete(checkedProjectInstallFullPath, true);
                    await LogInfoAndSendMessage("Folder {0} {1} deleted successfully", checkedProjectInstallFullPath,
                        cancellationToken);
                    deleteSuccess = true;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Folder could not deleted");
                    await LogWarningAndSendMessage("Folder {0} could not deleted on try {1}",
                        checkedProjectInstallFullPath, tryCount, cancellationToken);
                    await LogInfoAndSendMessage("waiting for 3 seconds...", cancellationToken);
                    Thread.Sleep(3000);
                }
            }
        }

        if (!deleteSuccess)
            return await LogErrorAndSendMessageFromError(
                InstallerErrors.FolderCanNotBeDeleted(checkedProjectInstallFullPath), cancellationToken);

        await LogInfoAndSendMessage("Install {0} files to {1}...", projectName, checkedProjectInstallFullPath,
            cancellationToken);

        //გაშლილი არქივის ფაილები გადავიტანოთ სერვისის ფოლდერში
        await LogInfoAndSendMessage("Move Files from {0} to {1}...", projectFilesFolderFullName, projectInstallFullPath,
            cancellationToken);
        Directory.Move(projectFilesFolderFullName, checkedProjectInstallFullPath);

        //ჩავაგდოთ პარამეტრების ფაილი ახლადდაინსტალირებულ ფოლდერში
        await LogInfoAndSendMessage("WriteAllTextToPath {0}...", checkedProjectInstallFullPath, cancellationToken);
        appSettingsFile?.WriteAllTextToPath(checkedProjectInstallFullPath);

        await LogInfoAndSendMessage("Change Owner for Path {0} for user {1} and group {2}",
            checkedProjectInstallFullPath, filesUserName, filesUsersGroupName, cancellationToken);

        var changeOwnerResult =
            await ChangeFolderOwner(checkedProjectInstallFullPath, filesUserName, filesUsersGroupName, cancellationToken);
        if (changeOwnerResult.IsSome)
            return await LogErrorAndSendMessageFromError(
                InstallerErrors.FolderOwnerCanNotBeChanged(checkedProjectInstallFullPath), cancellationToken);

        if (string.IsNullOrWhiteSpace(serviceName))
            return assemblyVersion;

        //თუ სერვისი უკვე დარეგისტრირებულია, შევამოწმოთ სწორად არის თუ არა დარეგისტრირებული.
        if (serviceExists)
        {
            await LogInfoAndSendMessage("Because service {0}/{1} is exists, Check if Service registered properly",
                projectName, serviceEnvName, cancellationToken);

            var isServiceRegisteredProperlyResult = await IsServiceRegisteredProperly(projectName, serviceEnvName,
                serviceUserName, checkedProjectInstallFullPath, serviceDescriptionSignature, projectDescription,
                cancellationToken);
            if (isServiceRegisteredProperlyResult.IsT1)
                return Err.RecreateErrors(isServiceRegisteredProperlyResult.AsT1,
                    InstallerErrors.IsServiceRegisteredProperlyError);

            if (!isServiceRegisteredProperlyResult.AsT0)
            {
                await LogInfoAndSendMessage("Service {0}/{} registration is not properly, so will be removed",
                    projectName, serviceEnvName, cancellationToken);
                var removeServiceError = RemoveService(serviceEnvName);
                if (removeServiceError.IsSome)
                {
                    Err.PrintErrorsOnConsole((Err[])removeServiceError);
                    return (Err[])removeServiceError;
                }

                //რადგან სერვისი წავშალეთ ის აღარ არსებობს და შემდგომში თავიდან უნდა შეიქმნას
                serviceExists = false;
            }
        }

        //თუ სერვისი არ არის დარეგისტრირებული და პლატფორმა მოითხოვს დარეგისტრირებას, დავარეგისტრიროთ
        if (!serviceExists)
        {
            await LogInfoAndSendMessage("registering service {0}...", serviceEnvName, cancellationToken);

            var registerServiceResult = await RegisterService(projectName, serviceEnvName, serviceUserName,
                checkedProjectInstallFullPath, serviceDescriptionSignature, projectDescription, cancellationToken);

            if (registerServiceResult.IsSome)
                return await LogErrorAndSendMessageFromError(InstallerErrors.CannotRegisterService(serviceEnvName),
                    cancellationToken);
        }

        //გავუშვათ სერვისი და დავრწმუნდეთ, რომ გაეშვა.
        var startResult = await Start(serviceEnvName, cancellationToken);
        if (startResult.IsNone)
            return assemblyVersion;

        return await LogErrorAndSendMessageFromError(InstallerErrors.ServiceCanNotBeStarted(serviceEnvName),
            cancellationToken);
    }

    public async Task<OneOf<string?, Err[]>> RunUpdateApplication(string archiveFileName, string projectName,
        string environmentName, string filesUserName, string filesUsersGroupName, string installWorkFolder,
        string installFolder, CancellationToken cancellationToken)
    {
        //დავადგინოთ არსებობს თუ არა {_workFolder} სახელით ქვეფოლდერი სამუშაო ფოლდერში
        //და თუ არ არსებობს, შევქმნათ
        var checkedWorkFolder = FileStat.CreateFolderIfNotExists(installWorkFolder, UseConsole);
        if (checkedWorkFolder == null)
            return await LogErrorAndSendMessageFromError(
                InstallerErrors.InstallerWorkFolderDoesNotCreated(installWorkFolder), cancellationToken);

        //გავშალოთ არქივი სამუშაო ფოლდერში, იმისათვის, რომ დავრწმუნდეთ,
        //რომ არქივი დაზიანებული არ არის და ყველა ფაილის გახსნა ხერხდება
        //ZipClassArchiver zipClassArchiver = new ZipClassArchiver(_logger, outputFolderPath, zipFileFullName);
        var folderName = Path.GetFileNameWithoutExtension(archiveFileName);
        var projectFilesFolderFullName = Path.Combine(checkedWorkFolder, folderName);
        var archiveFileFullName = Path.Combine(checkedWorkFolder, archiveFileName);

        if (Directory.Exists(projectFilesFolderFullName))
        {
            await LogInfoAndSendMessage("Delete Existing Project files in {0}", projectFilesFolderFullName,
                cancellationToken);
            Directory.Delete(projectFilesFolderFullName, true);
        }

        ZipFile.ExtractToDirectory(archiveFileFullName, projectFilesFolderFullName);
        await LogInfoAndSendMessage("Project files is extracted to {0}", projectFilesFolderFullName, cancellationToken);

        //წავშალოთ გახსნილი არქივი, რადგან ის აღარ გვჭირდება
        //(შეიძლება ისე გავაკეთო, რომ არ წავშალო არქივი, რადგან მოქაჩვას შეიძლება დრო სჭირდებოდეს
        //ასეთ შემთხვევაში უნდა შევინარჩუნო არქივების ლიმიტირებული რაოდენობა
        //და ამ რაოდენობაზე მეტი რაც იქნება, უნდა წაიშალოს)

        //წაიშალოს ლოკალური ფაილი
        await LogInfoAndSendMessage("Deleting {0} file...", archiveFileFullName, cancellationToken);
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
        var projectInstallFullPath = Path.Combine(installFolder, projectName, environmentName);

        var checkedProjectInstallFullPath = FileStat.CreateFolderIfNotExists(projectInstallFullPath, UseConsole);
        if (checkedProjectInstallFullPath == null)
            return await LogErrorAndSendMessageFromError(InstallerErrors.InstallerFolderIsNotExists(installFolder),
                cancellationToken);

        await LogInfoAndSendMessage("Installer project install folder is {0}", checkedProjectInstallFullPath,
            cancellationToken);

        if (Directory.Exists(projectInstallFullPath))
        {
            deleteSuccess = false;
            await LogInfoAndSendMessage("Folder {0} already exists", projectInstallFullPath, cancellationToken);

            var tryCount = 0;
            while (!deleteSuccess && tryCount < 10)
            {
                tryCount++;
                try
                {
                    await LogInfoAndSendMessage("Try to delete folder {0} {1}...", projectInstallFullPath, tryCount,
                        cancellationToken);
                    Directory.Delete(projectInstallFullPath, true);
                    await LogInfoAndSendMessage("Folder {0} deleted successfully", projectInstallFullPath,
                        cancellationToken);
                    deleteSuccess = true;
                }
                catch
                {
                    await LogWarningAndSendMessage("Folder {0} could not deleted on try {1}", projectInstallFullPath,
                        tryCount, cancellationToken);
                    await LogInfoAndSendMessage("waiting for 3 seconds...", cancellationToken);
                    Thread.Sleep(3000);
                }
            }
        }

        if (!deleteSuccess)
            return await LogErrorAndSendMessageFromError(InstallerErrors.FolderCanNotBeDeleted(projectInstallFullPath),
                cancellationToken);

        await LogWarningAndSendMessage("Install {0} files to {1}...", projectName, projectInstallFullPath,
            cancellationToken);

        await LogWarningAndSendMessage("Install {0} files to {1}...", projectName, projectInstallFullPath,
            cancellationToken);
        //გაშლილი არქივის ფაილები გადავიტანოთ სერვისის ფოლდერში
        Directory.Move(projectFilesFolderFullName, projectInstallFullPath);

        var changeOwnerResult =
            await ChangeFolderOwner(projectInstallFullPath, filesUserName, filesUsersGroupName, cancellationToken);
        if (changeOwnerResult.IsNone)
            return assemblyVersion;

        return await LogErrorAndSendMessageFromError(
            InstallerErrors.FolderOwnerCanNotBeChanged(checkedProjectInstallFullPath), cancellationToken);
    }

    public async Task<Option<Err[]>> Stop(string? serviceName, string environmentName,
        CancellationToken cancellationToken)
    {
        return await Stop(GetServiceEnvName(serviceName, environmentName), cancellationToken);
    }

    private async Task<Option<Err[]>> Stop(string serviceEnvName, CancellationToken cancellationToken)
    {
        //დავადგინოთ არსებობს თუ არა სერვისების სიაში სერვისი სახელით {serviceEnvName}
        var serviceExists = IsServiceExists(serviceEnvName);
        if (serviceExists)
            await LogInfoAndSendMessage("Service {0} is exists", serviceEnvName, cancellationToken);
        else
            return await LogErrorAndSendMessageFromError(InstallerErrors.ServiceIsNotExists(serviceEnvName),
                cancellationToken);

        var serviceIsRunning = IsServiceRunning(serviceEnvName);
        if (!serviceIsRunning)
        {
            await LogInfoAndSendMessage("Service {0} is not running", serviceEnvName, cancellationToken);
            return null;
        }

        await LogInfoAndSendMessage("Service {0} is running", serviceEnvName, cancellationToken);

        var stopServiceResult = await StopService(serviceEnvName, cancellationToken);
        if (stopServiceResult.IsNone)
            return stopServiceResult;

        return await LogErrorAndSendMessageFromError(InstallerErrors.ServiceCanNotBeStopped(serviceEnvName),
            cancellationToken);
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
            await LogInfoAndSendMessage("Service {0} is running", serviceEnvName, cancellationToken);
            return null;
        }

        await LogInfoAndSendMessage("Service {0} is not running", serviceEnvName, cancellationToken);

        var startServiceResult = await StartService(serviceEnvName, cancellationToken);
        if (startServiceResult.IsNone)
            return null;

        return await LogErrorAndSendMessageFromError(InstallerErrors.ServiceCanNotBeStarted(serviceEnvName),
            cancellationToken);
    }

    public async Task<Option<Err[]>> RemoveProjectAndService(string projectName, string serviceName,
        string environmentName, string installFolder, CancellationToken cancellationToken)
    {
        var serviceEnvName = GetServiceEnvName(serviceName, environmentName);

        await LogInfoAndSendMessage("Remove service {0} started...", serviceEnvName, cancellationToken);

        var serviceExists = IsServiceExists(serviceEnvName);
        if (serviceExists)
            await LogInfoAndSendMessage("Service {0} is exists", serviceEnvName, cancellationToken);
        else
            await LogInfoAndSendMessage("Service {0} is not exists", serviceEnvName, cancellationToken);

        var serviceIsRunning = false;
        if (serviceExists)
        {
            serviceIsRunning = IsServiceRunning(serviceEnvName);
            if (serviceIsRunning)
                await LogInfoAndSendMessage("Service {0} is running", serviceEnvName, cancellationToken);
            else
                await LogInfoAndSendMessage("Service {0} is not running", serviceEnvName, cancellationToken);
        }

        if (serviceIsRunning)
        {
            var stopResult = await Stop(serviceEnvName, cancellationToken);
            if (stopResult.IsSome)
                return await LogErrorAndSendMessageFromError(InstallerErrors.ServiceCanNotBeStopped(serviceEnvName),
                    cancellationToken);
        }

        if (!serviceExists)
            return await RemoveProject(projectName, environmentName, installFolder, cancellationToken);

        if (RemoveService(serviceEnvName))
            return await RemoveProject(projectName, environmentName, installFolder, cancellationToken);

        return await LogErrorAndSendMessageFromError(InstallerErrors.ServiceCanNotBeRemoved(serviceEnvName), cancellationToken);
    }

    public async Task<Option<Err[]>> RemoveProject(string projectName, string environmentName, string installFolder,
        CancellationToken cancellationToken)
    {
        await LogInfoAndSendMessage("Remove project {0} started...", projectName, cancellationToken);

        var checkedInstallFolder = FileStat.CreateFolderIfNotExists(installFolder, UseConsole);
        if (checkedInstallFolder == null)
            return await LogErrorAndSendMessageFromError(InstallerErrors.InstallerFolderIsNotExists(installFolder),
                cancellationToken);

        //თუ არსებობს, წაიშალოს არსებული ფაილები.
        var projectInstallFullPath = Path.Combine(checkedInstallFolder, projectName, environmentName);

        await LogInfoAndSendMessage("Deleting files {0}...", projectName, cancellationToken);

        if (Directory.Exists(projectInstallFullPath))
            Directory.Delete(projectInstallFullPath, true);

        return null;
    }
}