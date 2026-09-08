using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;
using ToolsManagement.Installer.Domain;
using ToolsManagement.Installer.Errors;

// ReSharper disable ConvertToPrimaryConstructor

namespace ToolsManagement.Installer.ServiceInstaller;

public /*open*/ abstract class InstallerBase : MessageLogger
{
    public readonly string Runtime;
    private readonly ILogger _logger;

    protected InstallerBase(bool useConsole, ILogger logger, string runtime, IMessagesDataManager? messagesDataManager,
        string? userName) : base(logger, messagesDataManager, userName, useConsole)
    {
        Runtime = runtime;
        _logger = logger;
    }

    protected abstract ValueTask<Result<bool>> IsServiceRegisteredProperly(string projectName, string serviceEnvName,
        string serviceUserName, string installFolderPath, string? serviceDescriptionSignature,
        string? projectDescription, CancellationToken cancellationToken = default);

    protected abstract ValueTask<Result> ChangeOneFileOwner(string filePath, string? filesUserName,
        string? filesUsersGroupName, CancellationToken cancellationToken = default);

    protected abstract ValueTask<Result> ChangeFolderOwner(string folderPath, string filesUserName,
        string filesUsersGroupName, CancellationToken cancellationToken = default);

    protected abstract ValueTask<Result> RemoveService(string serviceEnvName,
        CancellationToken cancellationToken = default);

    protected abstract ValueTask<Result> StopService(string serviceEnvName,
        CancellationToken cancellationToken = default);

    protected abstract ValueTask<Result> StartService(string serviceEnvName,
        CancellationToken cancellationToken = default);

    protected abstract ValueTask<Result> RegisterService(string projectName, string serviceEnvName,
        string serviceUserName, string installFolderPath, string? serviceDescriptionSignature,
        string? projectDescription, CancellationToken cancellationToken = default);

    protected abstract bool IsServiceExists(string serviceEnvName);

    protected abstract bool IsServiceRunning(string serviceEnvName);

    //ძველი (შესაძლოა ობოლი) პროცესის PID-ის დადგენა და მისი მოკვლა, რომ გათავისუფლდეს პორტი.
    //ნაგულისხმევად არაფერს აკეთებს. გადატვირთულია Linux-ისთვის, სადაც systemctl stop ყოველთვის
    //არ წყვეტს პროცესს (განსაკუთრებით ობოლს). Windows-ზე პროცესს ასრულებს SCM-ით გაჩერება.
    protected virtual ValueTask<Result> KillProcessByPid(string serviceEnvName, string projectName,
        string installFolderPath, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(Result.Success());
    }

    private static string GetServiceEnvName(string projectName, string environmentName)
    {
        return $"{projectName}{environmentName}";
    }

    private static string? GetParametersVersion(string appSettingsFileBody)
    {
        JObject latestAppSetJObject = JObject.Parse(appSettingsFileBody);
        string? latestAppSettingsVersion = latestAppSetJObject["VersionInfo"]?["AppSettingsVersion"]?.Value<string>();
        return latestAppSettingsVersion;
    }

    private static bool IsProcessRunning(string processName)
    {
        return Process.GetProcessesByName(processName).Length > 0;
    }

    public async ValueTask<Result> RunUpdateSettings(string projectName, string environmentName,
        string appSettingsFileName, string appSettingsFileBody, string? filesUserName, string? filesUsersGroupName,
        string installFolder, CancellationToken cancellationToken = default)
    {
        Result<string> checkBeforeStartUpdateResult =
            await CheckBeforeStartUpdate(projectName, installFolder, environmentName, cancellationToken);

        if (checkBeforeStartUpdateResult.IsFailure)
        {
            return checkBeforeStartUpdateResult.Error;
        }

        string projectInstallFullPath = checkBeforeStartUpdateResult.Value;

        //დავადგინოთ დაინსტალირებული პარამეტრების ფაილის სრული გზა
        string appSettingsFileFullPath = Path.Combine(projectInstallFullPath, appSettingsFileName);

        //დავადგინოთ ფაილსაცავიდან მიღებული ბოლო პარამეტრების ფაილის ვერსია.
        string? latestAppSettingsVersion = GetParametersVersion(appSettingsFileBody);
        //თუ დაინსტალირებული ფაილი არსებობს, დავადგინოთ მისი ვერსია
        if (!string.IsNullOrWhiteSpace(latestAppSettingsVersion) && File.Exists(appSettingsFileFullPath))
        {
            //დავადგინოთ დაინსტალირებული პარამეტრების ფაილის ვერსია.
            string currentAppSettingsFileBody = await File.ReadAllTextAsync(appSettingsFileFullPath, cancellationToken);
            string? currentAppSettingsVersion = GetParametersVersion(currentAppSettingsFileBody);
            //თუ ვერსიები ემთხვევა დაინსტალირება აღარ გრძელდება, რადგან ისედაც ბოლო ვერსია აყენია
            if (!string.IsNullOrWhiteSpace(currentAppSettingsVersion) &&
                latestAppSettingsVersion == currentAppSettingsVersion)
            {
                await LogWarningAndSendMessage("Parameters file is already in latest version and not needs update",
                    cancellationToken);
                return Result.Success();
            }
        }

        //აქ თუ მოვედით, ან დაინსტალირებული ფაილი არ არსებობს, ან ახალი ფაილისა და დაინსტალირებული ფაილის ვერსიები არ ემთხვევა.
        //კიდევ აქ მოსვლის მიზეზი შეიძლება იყოს, ის რომ ფაილებში არასწორად არის, ან საერთოდ არ არის გაწერილი ვერსიები
        //ამ ბოლო შემთხვევაზე ყურადღებას არ ვამახვილებთ, იმისათვის, რომ შესაძლებელი იყოს ასეთი "არასწორი" პროგრამების პარამეტრები განახლდეს.

        string serviceEnvName = GetServiceEnvName(projectName, environmentName);

        Result stopResult = await Stop(serviceEnvName, cancellationToken);
        if (!string.IsNullOrWhiteSpace(serviceEnvName))
        {
            //დავადგინოთ არსებობს თუ არა სერვისების სიაში სერვისი სახელით {projectName}
            bool serviceExists = IsServiceExists(serviceEnvName);
            if (serviceExists)
            {
                await LogInfoAndSendMessage("Service {0} is exists", serviceEnvName, cancellationToken);
            }
            else
                //ეს არის პარამეტრების განახლების პროცესი, ამიტომ თუ პროგრამა სერვისია და ეს სერვისი არ არსებობს განახლება ვერ მოხდება
                //ასეთ შემთხვევაში უნდა გაეშვას უკვე მთლიანი პროგრამის განახლების პროცესი
            {
                return await LogErrorAndSendMessageFromError(InstallerErrors.ServiceIsNotExists(serviceEnvName),
                    cancellationToken);
            }

            //თუ სერვისი გაშვებულია უკვე, გავაჩეროთ
            await LogInfoAndSendMessage("Try to stop Service {0}", serviceEnvName, cancellationToken);
            if (stopResult.IsFailure)
            {
                return stopResult.Error;
            }
        }
        else if (IsProcessRunning(projectName))
            //თუ სერვისი არ არის და პროგრამა მაინც გაშვებულია,
            //ასეთ შემთხვევაში პარამეტრების ფაილს ვერ გავაახლებთ,
            //რადგან გაშვებული პროგრამა ვერ მიხვდება, რომ ახალი პარამეტრები უნდა გამოიყენოს.
            //ასეთ შემთხვევაში ჯერ უნდა გაჩერდეს პროგრამა და მერე უნდა განახლდეს პარამეტრები.
        {
            return await LogErrorAndSendMessageFromError(
                InstallerErrors.ProcessIsRunningAndCannotBeUpdated(projectName), cancellationToken);
        }

        //შევეცადოთ პარამეტრების ფაილის წაშლა
        bool appSettingsFileDeletedSuccess = true;
        if (File.Exists(appSettingsFileFullPath))
        {
            appSettingsFileDeletedSuccess = false;
            await LogInfoAndSendMessage("File {0} is exists", appSettingsFileFullPath, cancellationToken);

            int tryCount = 0;
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
                    await Task.Delay(3000, cancellationToken);
                }
            }
        }

        if (!appSettingsFileDeletedSuccess)
        {
            return await LogErrorAndSendMessageFromError(InstallerErrors.FileCanNotBeDeleted(appSettingsFileFullPath),
                cancellationToken).ConfigureAwait(false);
        }

        //შეიქმნას პარამეტრების ფაილი არსებულ ინფორმაციაზე დაყრდნობით
        await File.WriteAllTextAsync(appSettingsFileFullPath, appSettingsFileBody, cancellationToken);
        //შეიცვალოს პარამეტრების ფაილზე უფლებები საჭიროების მიხედვით.
        Result changeOneFileOwnerResult = await ChangeOneFileOwner(appSettingsFileFullPath, filesUserName,
            filesUsersGroupName, cancellationToken);
        if (changeOneFileOwnerResult.IsFailure)
        {
            return await LogErrorAndSendMessageFromError(
                InstallerErrors.FileOwnerCanNotBeChanged(appSettingsFileFullPath), cancellationToken);
        }

        //თუ სერვისია, გავუშვათ ეს სერვისი და დავრწმუნდეთ, რომ გაეშვა.
        Result startResult = await Start(serviceEnvName, cancellationToken);
        if (startResult.IsSuccess)
        {
            return Result.Success();
        }

        //თუ სერვისი არ გაეშვა, ვაბრუნებთ შეტყობინებას
        return await LogErrorAndSendMessageFromError(InstallerErrors.ServiceCanNotBeStarted(projectName),
            cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<Result<string>> CheckBeforeStartUpdate(string projectName, string installFolder,
        string environmentName, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(installFolder))
        {
            return await LogErrorAndSendMessageFromError(InstallerErrors.InstallerFolderIsNotExists(installFolder),
                cancellationToken);
        }

        await LogInfoAndSendMessage("Installer install folder is {0}", installFolder, cancellationToken);

        string projectInstallFullPath = Path.Combine(installFolder, projectName, environmentName);
        if (!Directory.Exists(projectInstallFullPath))
        {
            return await LogErrorAndSendMessageFromError(InstallerErrors.ProjectInstallerFolderIsNotExists(projectName),
                cancellationToken);
        }

        await LogInfoAndSendMessage("Project install folder is {0}", projectInstallFullPath, cancellationToken);

        return projectInstallFullPath;
    }

    public async ValueTask<Result<string>> RunUpdateService(string archiveFileName, string projectName,
        string environmentName, FileNameAndTextContent? appSettingsFile, string serviceUserName, string filesUserName,
        string filesUsersGroupName, string installWorkFolder, string installFolder, string? serviceDescriptionSignature,
        string? projectDescription, CancellationToken cancellationToken = default)
    {
        //დავადგინოთ არსებობს თუ არა {_workFolder} სახელით ქვეფოლდერი სამუშაო ფოლდერში
        //და თუ არ არსებობს, შევქმნათ

        string? checkedWorkFolder = FileStat.CreateFolderIfNotExists(installWorkFolder, UseConsole);
        if (checkedWorkFolder == null)
        {
            return await LogErrorAndSendMessageFromError(
                InstallerErrors.InstallerWorkFolderDoesNotCreated(installWorkFolder), cancellationToken);
        }

        string projectInstallFullPath = Path.Combine(installFolder, projectName);
        string? checkedProjectInstallFullPath = FileStat.CreateFolderIfNotExists(projectInstallFullPath, UseConsole);
        if (checkedProjectInstallFullPath == null)
        {
            return await LogErrorAndSendMessageFromError(
                InstallerErrors.InstallerInstallFolderDoesNotCreated(projectInstallFullPath), cancellationToken);
        }

        string projectInstallFullPathWithEnv = Path.Combine(projectInstallFullPath, environmentName);

        await LogInfoAndSendMessage("Installer project install folder is {0}", projectInstallFullPathWithEnv,
            cancellationToken);

        //გავშალოთ არქივი სამუშაო ფოლდერში, იმისათვის, რომ დავრწმუნდეთ,
        //რომ არქივი დაზიანებული არ არის და ყველა ფაილის გახსნა ხერხდება
        string folderName = Path.GetFileNameWithoutExtension(archiveFileName);
        string projectFilesFolderFullName = Path.Combine(checkedWorkFolder, folderName);
        string archiveFileFullName = Path.Combine(checkedWorkFolder, archiveFileName);

        if (Directory.Exists(projectFilesFolderFullName))
        {
            await LogInfoAndSendMessage("Delete Existing Project files in {0}", projectFilesFolderFullName,
                cancellationToken);
            Directory.Delete(projectFilesFolderFullName, true);
        }

        await ZipFile.ExtractToDirectoryAsync(archiveFileFullName, projectFilesFolderFullName, cancellationToken);

        if (!Directory.Exists(projectFilesFolderFullName))
        {
            return await LogErrorAndSendMessageFromError(
                InstallerErrors.ProjectFilesIsNotExtracted(projectFilesFolderFullName), cancellationToken);
        }

        await LogInfoAndSendMessage("Project files is extracted to {0}", projectFilesFolderFullName, cancellationToken);

        //წავშალოთ გახსნილი არქივი, რადგან ის აღარ გვჭირდება
        //(შეიძლება ისე გავაკეთო, რომ არ წავშალო არქივი, რადგან მოქაჩვას შეიძლება დრო სჭირდებოდეს
        //ასეთ შემთხვევაში უნდა შევინარჩუნო არქივების ლიმიტირებული რაოდენობა
        //და ამ რაოდენობაზე მეტი რაც იქნება, უნდა წაიშალოს)
        await LogInfoAndSendMessage("Deleting {0} file...", archiveFileFullName, cancellationToken);

        //წაიშალოს ლოკალური ფაილი
        File.Delete(archiveFileFullName);

        //დავადგინოთ პროგრამის ვერსია და დავაბრუნოთ
        string projectMainExeFileName = Path.Combine(projectFilesFolderFullName, $"{projectName}.dll");

        string? assemblyVersion = AssemblyName.GetAssemblyName(projectMainExeFileName).Version?.ToString();

        string serviceEnvName = GetServiceEnvName(projectName, environmentName);
        //დავადგინოთ არსებობს თუ არა სერვისების სიაში სერვისი სახელით {serviceEnvName}
        bool serviceExists = IsServiceExists(serviceEnvName);
        if (serviceExists)
        {
            await LogInfoAndSendMessage("Service {0} is exists", serviceEnvName, cancellationToken);
        }
        else
        {
            await LogInfoAndSendMessage("Service {0} is not exists", serviceEnvName, cancellationToken);
        }

        if (serviceExists)
        {
            //თუ სიაში არსებობს დავადგინოთ გაშვებულია თუ არა სერვისი.
            bool serviceIsRunning = IsServiceRunning(serviceEnvName);
            if (serviceIsRunning)
            {
                await LogInfoAndSendMessage("Service {0} is running", serviceEnvName, cancellationToken);
            }
            else
            {
                await LogInfoAndSendMessage("Service {0} is not running", serviceEnvName, cancellationToken);
            }

            if (serviceIsRunning)
            {
                //თუ სერვისი გაშვებულია უკვე, გავაჩეროთ
                await LogInfoAndSendMessage("Try to stop Service {0}", serviceEnvName, cancellationToken);
                await LogInfoAndSendMessage(
                    "Please be patient, the process may take a few seconds, maybe even a minute...", cancellationToken);

                Result stopResult = await Stop(serviceEnvName, cancellationToken);
                if (stopResult.IsFailure)
                {
                    return await LogErrorAndSendMessageFromError(InstallerErrors.ServiceIsNotStopped(serviceEnvName),
                        cancellationToken);
                }
            }
        }

        //გრაციოზული გაჩერების მიუხედავად, პროცესი შესაძლოა მაინც ცოცხალი იყოს — მაგალითად
        //ობოლი პროცესი წინა გაუმართავი განახლებიდან, რომელსაც systemd ვეღარ აკონტროლებს
        //(.service ფაილი წაშლილია, მაგრამ პროცესი პორტს კვლავ იკავებს). ამიტომ ნებისმიერ
        //შემთხვევაში დავადგინოთ გაშვებული პროცესის PID და მოვკლათ, რომ პორტი გათავისუფლდეს.
        Result killProcessResult = await KillProcessByPid(serviceEnvName, projectName, projectInstallFullPathWithEnv,
            cancellationToken);
        if (killProcessResult.IsFailure)
        {
            return killProcessResult.Error;
        }

        //თუ არსებობს, წაიშალოს არსებული ფაილები.
        //თუ არსებობს, დავაარქივოთ და გადავინახოთ პროგრამის მიმდინარე ფაილები
        //(ეს კეთდება იმისათვის, რომ შესაძლებელი იყოს წინა ვერსიაზე სწრაფად დაბრუნება)
        //რადგან გადანახვა ხდება, ზედმეტი ფაილები რომ არ დაგროვდეს, წავშალოთ წინა გადანახულები,
        //ოღონდ არ წავშალოთ ბოლო რამდენიმე. (რაოდენობა პარამეტრებით უნდა იყოს განსაზღვრული)
        if (Directory.Exists(projectInstallFullPathWithEnv))
        {
            bool deleteSuccess = false;

            await LogInfoAndSendMessage("Folder {0} already exists", projectInstallFullPathWithEnv, cancellationToken);

            int tryCount = 0;
            while (!deleteSuccess && tryCount < 10)
            {
                tryCount++;
                try
                {
                    await LogInfoAndSendMessage("Try to delete folder {0} {1}...", projectInstallFullPathWithEnv,
                        tryCount, cancellationToken);
                    Directory.Delete(projectInstallFullPathWithEnv, true);
                    await LogInfoAndSendMessage("Folder {0} {1} deleted successfully", projectInstallFullPathWithEnv,
                        tryCount, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Folder could not deleted");
                    await LogWarningAndSendMessage("Folder {0} could not deleted on try {1}",
                        projectInstallFullPathWithEnv, tryCount, cancellationToken);
                    await LogInfoAndSendMessage("waiting for 3 seconds...", cancellationToken);
                    await Task.Delay(3000, cancellationToken);
                }

                deleteSuccess = !Directory.Exists(projectInstallFullPathWithEnv);
            }

            if (!deleteSuccess)
            {
                return await LogErrorAndSendMessageFromError(
                    InstallerErrors.FolderCanNotBeDeleted(projectInstallFullPathWithEnv), cancellationToken);
            }
        }

        await LogInfoAndSendMessage("Install {0} files to {1}...", projectName, projectInstallFullPathWithEnv,
            cancellationToken);

        //გაშლილი არქივის ფაილები გადავიტანოთ სერვისის ფოლდერში
        await LogInfoAndSendMessage("Move Files from {0} to {1}...", projectFilesFolderFullName,
            projectInstallFullPathWithEnv, cancellationToken);
        Directory.Move(projectFilesFolderFullName, projectInstallFullPathWithEnv);

        //ჩავაგდოთ პარამეტრების ფაილი ახლადდაინსტალირებულ ფოლდერში
        await LogInfoAndSendMessage("WriteAllTextToPath {0}...", projectInstallFullPathWithEnv, cancellationToken);
        appSettingsFile?.WriteAllTextToPath(projectInstallFullPathWithEnv);

        await LogInfoAndSendMessage("Change Owner for Path {0} for user {1} and group {2}",
            projectInstallFullPathWithEnv, filesUserName, filesUsersGroupName, cancellationToken);

        Result changeOwnerResult = await ChangeFolderOwner(projectInstallFullPathWithEnv, filesUserName,
            filesUsersGroupName, cancellationToken);
        if (changeOwnerResult.IsFailure)
        {
            return await LogErrorAndSendMessageFromError(
                InstallerErrors.FolderOwnerCanNotBeChanged(projectInstallFullPathWithEnv), cancellationToken);
        }

        //თუ სერვისი უკვე დარეგისტრირებულია, შევამოწმოთ სწორად არის თუ არა დარეგისტრირებული.
        if (serviceExists)
        {
            await LogInfoAndSendMessage("Because service {0}/{1} is exists, Check if Service registered properly",
                projectName, serviceEnvName, cancellationToken);

            Result<bool> isServiceRegisteredProperlyResult = await IsServiceRegisteredProperly(projectName,
                serviceEnvName, serviceUserName, projectInstallFullPathWithEnv, serviceDescriptionSignature,
                projectDescription, cancellationToken);
            if (isServiceRegisteredProperlyResult.IsFailure)
            {
                return Result.CreateValidationError([
                    .. isServiceRegisteredProperlyResult.Error.ToErrorArray(),
                    InstallerErrors.IsServiceRegisteredProperlyError
                ]);
            }

            if (!isServiceRegisteredProperlyResult.Value)
            {
                await LogInfoAndSendMessage("Service {0}/{1} registration is not properly, so will be removed",
                    projectName, serviceEnvName, cancellationToken);
                Result removeServiceError = await RemoveService(serviceEnvName, cancellationToken);
                if (removeServiceError.IsFailure)
                {
                    removeServiceError.Error.PrintErrorsOnConsole();
                    return removeServiceError.Error;
                }

                //რადგან სერვისი წავშალეთ ის აღარ არსებობს და შემდგომში თავიდან უნდა შეიქმნას
                serviceExists = false;
            }
        }

        //თუ სერვისი არ არის დარეგისტრირებული და პლატფორმა მოითხოვს დარეგისტრირებას, დავარეგისტრიროთ
        if (!serviceExists)
        {
            await LogInfoAndSendMessage("registering service {0}...", serviceEnvName, cancellationToken);

            Result registerServiceResult = await RegisterService(projectName, serviceEnvName, serviceUserName,
                projectInstallFullPathWithEnv, serviceDescriptionSignature, projectDescription, cancellationToken);

            if (registerServiceResult.IsFailure)
            {
                return await LogErrorAndSendMessageFromError(InstallerErrors.CannotRegisterService(serviceEnvName),
                    cancellationToken);
            }
        }

        //გავუშვათ სერვისი და დავრწმუნდეთ, რომ გაეშვა.
        Result startResult = await Start(serviceEnvName, cancellationToken);
        if (startResult.IsFailure)
        {
            return await LogErrorAndSendMessageFromError(InstallerErrors.ServiceCanNotBeStarted(serviceEnvName),
                cancellationToken);
        }

        if (assemblyVersion is null)
        {
            return await LogErrorAndSendMessageFromError(
                InstallerErrors.CannotUpdateProject(projectName, environmentName), cancellationToken);
        }

        return assemblyVersion;
    }

    public async ValueTask<Result<string>> RunUpdateApplication(string archiveFileName, string projectName,
        string environmentName, string filesUserName, string filesUsersGroupName, string installWorkFolder,
        string installFolder, CancellationToken cancellationToken = default)
    {
        //დავადგინოთ არსებობს თუ არა {_workFolder} სახელით ქვეფოლდერი სამუშაო ფოლდერში
        //და თუ არ არსებობს, შევქმნათ
        string? checkedWorkFolder = FileStat.CreateFolderIfNotExists(installWorkFolder, UseConsole);
        if (checkedWorkFolder == null)
        {
            return await LogErrorAndSendMessageFromError(
                InstallerErrors.InstallerWorkFolderDoesNotCreated(installWorkFolder), cancellationToken);
        }

        //გავშალოთ არქივი სამუშაო ფოლდერში, იმისათვის, რომ დავრწმუნდეთ,
        //რომ არქივი დაზიანებული არ არის და ყველა ფაილის გახსნა ხერხდება
        //ZipClassArchiver zipClassArchiver = new ZipClassArchiver(_logger, outputFolderPath, zipFileFullName);
        string folderName = Path.GetFileNameWithoutExtension(archiveFileName);
        string projectFilesFolderFullName = Path.Combine(checkedWorkFolder, folderName);
        string archiveFileFullName = Path.Combine(checkedWorkFolder, archiveFileName);

        if (Directory.Exists(projectFilesFolderFullName))
        {
            await LogInfoAndSendMessage("Delete Existing Project files in {0}", projectFilesFolderFullName,
                cancellationToken);
            Directory.Delete(projectFilesFolderFullName, true);
        }

        await ZipFile.ExtractToDirectoryAsync(archiveFileFullName, projectFilesFolderFullName, cancellationToken);
        await LogInfoAndSendMessage("Project files is extracted to {0}", projectFilesFolderFullName, cancellationToken);

        //წავშალოთ გახსნილი არქივი, რადგან ის აღარ გვჭირდება
        //(შეიძლება ისე გავაკეთო, რომ არ წავშალო არქივი, რადგან მოქაჩვას შეიძლება დრო სჭირდებოდეს
        //ასეთ შემთხვევაში უნდა შევინარჩუნო არქივების ლიმიტირებული რაოდენობა
        //და ამ რაოდენობაზე მეტი რაც იქნება, უნდა წაიშალოს)

        //წაიშალოს ლოკალური ფაილი
        await LogInfoAndSendMessage("Deleting {0} file...", archiveFileFullName, cancellationToken);
        File.Delete(archiveFileFullName);

        //დავადგინოთ პროგრამის ვერსია და დავაბრუნოთ
        string projectMainExeFileName = Path.Combine(projectFilesFolderFullName, $"{projectName}.dll");
        string? assemblyVersion = AssemblyName.GetAssemblyName(projectMainExeFileName).Version?.ToString();

        //თუ არსებობს, წაიშალოს არსებული ფაილები.
        //თუ არსებობს, დავაარქივოთ და გადავინახოთ პროგრამის მიმდინარე ფაილები
        //(ეს კეთდება იმისათვის, რომ შესაძლებელი იყოს წინა ვერსიაზე სწრაფად დაბრუნება)
        //რადგან გადანახვა ხდება, ზედმეტი ფაილები რომ არ დაგროვდეს, წავშალოთ წინა გადანახულები,
        //ოღონდ არ წავშალოთ ბოლო რამდენიმე. (რაოდენობა პარამეტრებით უნდა იყოს განსაზღვრული)
        bool deleteSuccess = true;
        string projectInstallFullPath = Path.Combine(installFolder, projectName, environmentName);

        string? checkedProjectInstallFullPath = FileStat.CreateFolderIfNotExists(projectInstallFullPath, UseConsole);
        if (checkedProjectInstallFullPath == null)
        {
            return await LogErrorAndSendMessageFromError(InstallerErrors.InstallerFolderIsNotExists(installFolder),
                cancellationToken);
        }

        await LogInfoAndSendMessage("Installer project install folder is {0}", checkedProjectInstallFullPath,
            cancellationToken);

        if (Directory.Exists(projectInstallFullPath))
        {
            deleteSuccess = false;
            await LogInfoAndSendMessage("Folder {0} already exists", projectInstallFullPath, cancellationToken);

            int tryCount = 0;
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
                    await Task.Delay(3000, cancellationToken);
                }
            }
        }

        if (!deleteSuccess)
        {
            return await LogErrorAndSendMessageFromError(InstallerErrors.FolderCanNotBeDeleted(projectInstallFullPath),
                cancellationToken);
        }

        await LogWarningAndSendMessage("Install {0} files to {1}...", projectName, projectInstallFullPath,
            cancellationToken);

        await LogWarningAndSendMessage("Install {0} files to {1}...", projectName, projectInstallFullPath,
            cancellationToken);
        //გაშლილი არქივის ფაილები გადავიტანოთ სერვისის ფოლდერში
        Directory.Move(projectFilesFolderFullName, projectInstallFullPath);

        Result changeOwnerResult = await ChangeFolderOwner(projectInstallFullPath, filesUserName, filesUsersGroupName,
            cancellationToken);
        if (changeOwnerResult.IsFailure)
        {
            return await LogErrorAndSendMessageFromError(
                InstallerErrors.FolderOwnerCanNotBeChanged(checkedProjectInstallFullPath), cancellationToken);
        }

        if (assemblyVersion is null)
        {
            return await LogErrorAndSendMessageFromError(
                InstallerErrors.CannotUpdateProject(projectName, environmentName), cancellationToken);
        }

        return assemblyVersion;
    }

    public ValueTask<Result> Stop(string projectName, string environmentName,
        CancellationToken cancellationToken = default)
    {
        return Stop(GetServiceEnvName(projectName, environmentName), cancellationToken);
    }

    private async ValueTask<Result> Stop(string serviceEnvName, CancellationToken cancellationToken = default)
    {
        //დავადგინოთ არსებობს თუ არა სერვისების სიაში სერვისი სახელით {serviceEnvName}
        bool serviceExists = IsServiceExists(serviceEnvName);
        if (serviceExists)
        {
            await LogInfoAndSendMessage("Service {0} is exists", serviceEnvName, cancellationToken);
        }
        else
        {
            return await LogErrorAndSendMessageFromError(InstallerErrors.ServiceIsNotExists(serviceEnvName),
                cancellationToken);
        }

        bool serviceIsRunning = IsServiceRunning(serviceEnvName);
        if (!serviceIsRunning)
        {
            await LogInfoAndSendMessage("Service {0} is not running", serviceEnvName, cancellationToken);
            return Result.Success();
        }

        await LogInfoAndSendMessage("Service {0} is running", serviceEnvName, cancellationToken);

        Result stopServiceResult = await StopService(serviceEnvName, cancellationToken);
        if (stopServiceResult.IsSuccess)
        {
            return stopServiceResult;
        }

        return await LogErrorAndSendMessageFromError(InstallerErrors.ServiceCanNotBeStopped(serviceEnvName),
            cancellationToken);
    }

    public ValueTask<Result> Start(string projectName, string environmentName,
        CancellationToken cancellationToken = default)
    {
        return Start(GetServiceEnvName(projectName, environmentName), cancellationToken);
    }

    private async ValueTask<Result> Start(string serviceEnvName, CancellationToken cancellationToken = default)
    {
        bool serviceIsRunning = IsServiceRunning(serviceEnvName);
        if (serviceIsRunning)
        {
            await LogInfoAndSendMessage("Service {0} is running", serviceEnvName, cancellationToken);
            return Result.Success();
        }

        await LogInfoAndSendMessage("Service {0} is not running", serviceEnvName, cancellationToken);

        Result startServiceResult = await StartService(serviceEnvName, cancellationToken);
        if (startServiceResult.IsSuccess)
        {
            return Result.Success();
        }

        return await LogErrorAndSendMessageFromError(InstallerErrors.ServiceCanNotBeStarted(serviceEnvName),
            cancellationToken);
    }

    public async ValueTask<Result> RemoveProjectAndService(string projectName, string environmentName, bool isService,
        string installFolder, CancellationToken cancellationToken = default)
    {
        if (!isService)
        {
            return await RemoveProject(projectName, environmentName, installFolder, cancellationToken);
        }

        string serviceEnvName = GetServiceEnvName(projectName, environmentName);

        await LogInfoAndSendMessage("Remove service {0} started...", serviceEnvName, cancellationToken);

        bool serviceExists = IsServiceExists(serviceEnvName);
        if (serviceExists)
        {
            await LogInfoAndSendMessage("Service {0} is exists", serviceEnvName, cancellationToken);
        }
        else
        {
            await LogInfoAndSendMessage("Service {0} is not exists", serviceEnvName, cancellationToken);
        }

        bool serviceIsRunning = false;
        if (serviceExists)
        {
            serviceIsRunning = IsServiceRunning(serviceEnvName);
            if (serviceIsRunning)
            {
                await LogInfoAndSendMessage("Service {0} is running", serviceEnvName, cancellationToken);
            }
            else
            {
                await LogInfoAndSendMessage("Service {0} is not running", serviceEnvName, cancellationToken);
            }
        }

        if (serviceIsRunning)
        {
            Result stopResult = await Stop(serviceEnvName, cancellationToken);
            if (stopResult.IsFailure)
            {
                return await LogErrorAndSendMessageFromError(InstallerErrors.ServiceCanNotBeStopped(serviceEnvName),
                    cancellationToken);
            }
        }

        Result removeServiceResult = await RemoveService(serviceEnvName, cancellationToken);
        if (removeServiceResult.IsSuccess)
        {
            return await RemoveProject(projectName, environmentName, installFolder, cancellationToken);
        }

        return await LogErrorAndSendMessageFromError(InstallerErrors.ServiceCanNotBeRemoved(serviceEnvName),
            cancellationToken);
    }

    public async ValueTask<Result> RemoveProject(string projectName, string environmentName, string installFolder,
        CancellationToken cancellationToken = default)
    {
        await LogInfoAndSendMessage("Remove project {0} started...", projectName, cancellationToken);

        string? checkedInstallFolder = FileStat.CreateFolderIfNotExists(installFolder, UseConsole);
        if (checkedInstallFolder == null)
        {
            return await LogErrorAndSendMessageFromError(InstallerErrors.InstallerFolderIsNotExists(installFolder),
                cancellationToken);
        }

        //თუ არსებობს, წაიშალოს არსებული ფაილები.
        string projectInstallFullPath = Path.Combine(checkedInstallFolder, projectName, environmentName);

        await LogInfoAndSendMessage("Deleting files {0}...", projectName, cancellationToken);

        if (Directory.Exists(projectInstallFullPath))
        {
            Directory.Delete(projectInstallFullPath, true);
        }

        return Result.Success();
    }
}
