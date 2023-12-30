using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LanguageExt;
using Microsoft.Extensions.Logging;
using OneOf;
using SystemToolsShared;

namespace Installer.ServiceInstaller;

public sealed class LinuxServiceInstaller : InstallerBase
{
    private readonly string _dotnetRunner;
    //private readonly string? _serviceDescriptionSignature;
    //private readonly string? _projectDescription;


    public LinuxServiceInstaller(bool useConsole, ILogger logger, string dotnetRunner,
        //string? serviceDescriptionSignature, string? projectDescription, 
        IMessagesDataManager? messagesDataManager,
        string? userName) : base(useConsole, logger, "linux-x64", messagesDataManager, userName)
    {
        _dotnetRunner = dotnetRunner;
        //_serviceDescriptionSignature = serviceDescriptionSignature;
        //_projectDescription = projectDescription;
    }

    public LinuxServiceInstaller(bool useConsole, ILogger logger, IMessagesDataManager? messagesDataManager,
        string? userName) : base(useConsole, logger, "linux-x64", messagesDataManager, userName)
    {
        _dotnetRunner = "";
        //_serviceDescriptionSignature = "";
    }

    protected override bool IsServiceExists(string serviceEnvName)
    {
        var serviceConfigFileName = GetServiceConfigFileName(serviceEnvName);
        return File.Exists(serviceConfigFileName);
    }

    private static string GetServiceConfigFileName(string serviceName)
    {
        var systemFolderFullName = "/etc/systemd/system";
        var serviceFileName = $"{serviceName}.service";
        var serviceConfigFileName = Path.Combine(systemFolderFullName, serviceFileName);
        return serviceConfigFileName;
    }

    protected override bool IsServiceRunning(string serviceEnvName)
    {
        return StShared.RunProcess(UseConsole, Logger, "systemctl",
            $"--no-ask-password --no-block --quiet is-active {serviceEnvName}").IsNone;
    }

    protected override Option<Err[]> RemoveService(string serviceEnvName)
    {
        var serviceConfigFileName = GetServiceConfigFileName(serviceEnvName);

        var disableProcessResult = StShared.RunProcess(UseConsole, Logger, "systemctl",
            $"--no-ask-password --no-block --quiet disable {serviceEnvName}");

        if (disableProcessResult.IsSome)
            return Err.RecreateErrors((Err[])disableProcessResult,
                new Err { ErrorCode = "TheServiceWasNotRemoved", ErrorMessage = "The service was not Removed" });

        File.Delete(serviceConfigFileName);

        return null;
    }

    protected override async Task<Option<Err[]>> StopService(string serviceEnvName, CancellationToken cancellationToken)
    {
        var stopProcessResult = StShared.RunProcess(UseConsole, Logger, "systemctl",
            $"--no-ask-password --no-block --quiet stop {serviceEnvName}");

        return stopProcessResult.IsSome
            ? await Task.FromResult(Err.RecreateErrors((Err[])stopProcessResult,
                new Err { ErrorCode = "TheServiceWasNotStopped", ErrorMessage = "The service was not Stopped" }))
            : null;
    }

    protected override async Task<Option<Err[]>> StartService(string serviceEnvName,
        CancellationToken cancellationToken)
    {
        var startProcessResult = StShared.RunProcess(UseConsole, Logger, "systemctl",
            $"--no-ask-password --no-block --quiet start {serviceEnvName}");

        return startProcessResult.IsSome
            ? await Task.FromResult(Err.RecreateErrors((Err[])startProcessResult,
                new Err { ErrorCode = "TheServiceWasNotStarted", ErrorMessage = "The service was not Started" }))
            : null;
    }

    protected override async Task<OneOf<bool, Err[]>> IsServiceRegisteredProperly(string projectName,
        string serviceEnvName,
        string serviceUserName, string installFolderPath, string? serviceDescriptionSignature,
        string? projectDescription, CancellationToken cancellationToken)
    {
        var serviceConfigFileName = GetServiceConfigFileName(serviceEnvName);

        var generateServiceFileTextResult = await GenerateServiceFileText(projectName, serviceEnvName,
            installFolderPath, serviceUserName, _dotnetRunner, serviceDescriptionSignature, projectDescription,
            cancellationToken);

        if (generateServiceFileTextResult.IsT1)
            return generateServiceFileTextResult.AsT1;

        var serviceFileText = generateServiceFileTextResult.AsT0;

        var existingServiceFileText = await File.ReadAllTextAsync(serviceConfigFileName, cancellationToken);

        return serviceFileText == existingServiceFileText;
    }

    private async Task<OneOf<string, Err[]>> GenerateServiceFileText(string projectName, string serviceName,
        string installFolderPath, string serviceUserName, string dotnetRunner, string? serviceDescriptionSignature,
        string? projectDescription, CancellationToken cancellationToken)
    {
        var checkedDotnetRunnerResult = CheckDotnetRunner(dotnetRunner);
        if (checkedDotnetRunnerResult.IsT1)
        {
            if (MessagesDataManager is not null)
                await MessagesDataManager.SendMessage(UserName, "dotnet location can not found", cancellationToken);
            Logger.LogError("dotnet location can not found");
            return Err.RecreateErrors(checkedDotnetRunnerResult.AsT1,
                new Err { ErrorCode = "DotnetLocationCanNotFound", ErrorMessage = "dotnet location can not found" });
        }

        var checkedDotnetRunner = checkedDotnetRunnerResult.AsT0;

        var mainDllFileName = Path.Combine(installFolderPath, $"{projectName}.dll");
        var syslogIdentifier = serviceName.Replace(".", "");

        return $"""
                [Unit]
                Description={serviceName} service {serviceDescriptionSignature ?? ""} {projectDescription ?? ""}

                [Service]
                WorkingDirectory={installFolderPath}
                ExecStart={checkedDotnetRunner} {mainDllFileName}
                Restart=always
                # Restart service after 10 seconds if the dotnet service crashes:
                RestartSec=10
                KillSignal=SIGINT
                SyslogIdentifier={syslogIdentifier}
                User={serviceUserName}
                Environment=ASPNETCORE_ENVIRONMENT=Production
                Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

                [Install]
                WantedBy=multi-user.target

                """;
    }


    protected override async Task<Option<Err[]>> RegisterService(string projectName, string serviceEnvName,
        string serviceUserName, string installFolderPath, string? serviceDescriptionSignature,
        string? projectDescription, CancellationToken cancellationToken)
    {
        var serviceConfigFileName = GetServiceConfigFileName(serviceEnvName);

        var generateServiceFileTextResult = await GenerateServiceFileText(projectName, serviceEnvName,
            installFolderPath, serviceUserName, _dotnetRunner, serviceDescriptionSignature, projectDescription,
            cancellationToken);
        if (generateServiceFileTextResult.IsT1)
            return generateServiceFileTextResult.AsT1;
        var serviceFileText = generateServiceFileTextResult.AsT0;

        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName, $"Create service file {serviceConfigFileName}",
                cancellationToken);
        Logger.LogInformation("Create service file {serviceConfigFileName}", serviceConfigFileName);
        await File.WriteAllTextAsync(serviceConfigFileName, (string?)serviceFileText, cancellationToken);

        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName, $"Enable service {serviceEnvName}", cancellationToken);
        Logger.LogInformation("Enable service {serviceName}", serviceEnvName);
        if (StShared.RunProcess(UseConsole, Logger, "systemctl",
                $"--no-ask-password --no-block --quiet enable {serviceEnvName}"))
        {
            if (IsServiceExists(serviceEnvName))
                return null;
            return new Err[]
            {
                new()
                {
                    ErrorCode = "ServiceIsNotEnabled",
                    ErrorMessage = $"Service {serviceEnvName} is not enabled"
                }
            };
        }

        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName, $"Service {serviceEnvName} can not enabled",
                cancellationToken);
        Logger.LogError("Service {serviceEnvName} can not enabled", serviceEnvName);
        return new Err[]
        {
            new()
            {
                ErrorCode = "ServiceCanNotEnabled",
                ErrorMessage = $"Service {serviceEnvName} can not enabled"
            }
        };
    }

    private OneOf<string, Err[]> CheckDotnetRunner(string? dotnetRunner)
    {
        if (!string.IsNullOrWhiteSpace(dotnetRunner) && File.Exists(dotnetRunner))
            return dotnetRunner;
        var runProcessWithOutputResult = StShared.RunProcessWithOutput(UseConsole, Logger, "which", "dotnet");
        if (runProcessWithOutputResult.IsT1)
            return Err.RecreateErrors(runProcessWithOutputResult.AsT1,
                new Err { ErrorCode = "WhichDotnetError", ErrorMessage = "Which Dotnet finished with Errors" });
        var newDotnetRunner = runProcessWithOutputResult.AsT0;
        if (!string.IsNullOrWhiteSpace(newDotnetRunner) && File.Exists(newDotnetRunner))
            return newDotnetRunner;
        return Err.RecreateErrors(runProcessWithOutputResult.AsT1,
            new Err { ErrorCode = "DotnetDetectError", ErrorMessage = "Dotnet detect Errors" });
    }

    protected override async Task<Option<Err[]>> ChangeOneFileOwner(string filePath, string? filesUserName,
        string? filesUsersGroupName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            if (MessagesDataManager is not null)
                await MessagesDataManager.SendMessage(UserName, "File name is empty", CancellationToken.None);
            Logger.LogError("File name is empty");
            return new Err[] { new() { ErrorCode = "FileNameIsEmpty", ErrorMessage = "File name is empty" } };
        }

        if (string.IsNullOrWhiteSpace(filesUserName))
        {
            if (MessagesDataManager is not null)
                await MessagesDataManager.SendMessage(UserName, "user name is empty. owner not changed",
                    cancellationToken);
            Logger.LogWarning("user name is empty. owner not changed");
            return null;
        }

        if (File.Exists(filePath))
            return StShared.RunProcess(UseConsole, Logger, "chown",
                $"{filesUserName}{(string.IsNullOrWhiteSpace(filesUsersGroupName) ? "" : $":{filesUsersGroupName}")} {filePath}");

        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName, $"Error changing owner to file {filePath}",
                cancellationToken);
        Logger.LogError("Error changing owner to file {filePath}", filePath);
        return new Err[]
        {
            new()
            {
                ErrorCode = "ErrorChangingOwnerToFile",
                ErrorMessage = $"Error changing owner to file {filePath}"
            }
        };
    }

    protected override async Task<Option<Err[]>> ChangeOwner(string folderPath, string filesUserName,
        string filesUsersGroupName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            if (MessagesDataManager is not null)
                await MessagesDataManager.SendMessage(UserName, "Folder name is empty", cancellationToken);
            Logger.LogError("Folder name is empty");
            return new Err[]
            {
                new()
                {
                    ErrorCode = "FolderNameIsEmpty",
                    ErrorMessage = "Folder name is empty"
                }
            };
        }

        if (string.IsNullOrWhiteSpace(filesUserName))
        {
            if (MessagesDataManager is not null)
                await MessagesDataManager.SendMessage(UserName, "user name is empty. owner not changed",
                    cancellationToken);
            Logger.LogWarning("user name is empty. owner not changed");
            return null;
        }

        if (Directory.Exists(folderPath))
            return StShared.RunProcess(UseConsole, Logger, "chown",
                $"-R {filesUserName}{(string.IsNullOrWhiteSpace(filesUsersGroupName) ? "" : $":{filesUsersGroupName}")} {folderPath}");

        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName, $"Error changing owner to folder {folderPath}",
                cancellationToken);
        Logger.LogError("Error changing owner to folder {folderPath}", folderPath);
        return new Err[]
        {
            new()
            {
                ErrorCode = "ErrorChangingOwner",
                ErrorMessage = $"Error changing owner to folder {folderPath}"
            }
        };
    }
}