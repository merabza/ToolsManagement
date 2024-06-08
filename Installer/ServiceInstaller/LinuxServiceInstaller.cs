using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Installer.Errors;
using LanguageExt;
using Microsoft.Extensions.Logging;
using OneOf;
using SystemToolsShared;
using SystemToolsShared.Errors;

namespace Installer.ServiceInstaller;

public sealed class LinuxServiceInstaller : InstallerBase
{
    private readonly string _dotnetRunner;
    private readonly ILogger _logger;

    public LinuxServiceInstaller(bool useConsole, ILogger logger, string dotnetRunner,
        IMessagesDataManager? messagesDataManager, string? userName) : base(useConsole, logger, "linux-x64",
        messagesDataManager, userName)
    {
        _logger = logger;
        _dotnetRunner = dotnetRunner;
    }

    public LinuxServiceInstaller(bool useConsole, ILogger logger, IMessagesDataManager? messagesDataManager,
        string? userName) : base(useConsole, logger, "linux-x64", messagesDataManager, userName)
    {
        _logger = logger;
        _dotnetRunner = "";
    }

    protected override bool IsServiceExists(string serviceEnvName)
    {
        var serviceConfigFileName = GetServiceConfigFileName(serviceEnvName);
        return File.Exists(serviceConfigFileName);
    }

    private static string GetServiceConfigFileName(string serviceEnvName)
    {
        const string systemFolderFullName = "/etc/systemd/system";
        var serviceFileName = $"{serviceEnvName}.service";
        var serviceConfigFileName = Path.Combine(systemFolderFullName, serviceFileName);
        return serviceConfigFileName;
    }

    protected override bool IsServiceRunning(string serviceEnvName)
    {
        return StShared.RunProcess(UseConsole, _logger, "systemctl",
            $"--no-ask-password --no-block --quiet is-active {serviceEnvName}").IsNone;
    }

    protected override Option<Err[]> RemoveService(string serviceEnvName)
    {
        var serviceConfigFileName = GetServiceConfigFileName(serviceEnvName);

        var disableProcessResult = StShared.RunProcess(UseConsole, _logger, "systemctl",
            $"--no-ask-password --no-block --quiet disable {serviceEnvName}", [1]);

        if (disableProcessResult.IsSome)
            return Err.RecreateErrors((Err[])disableProcessResult, InstallerErrors.TheServiceWasNotRemoved);

        File.Delete(serviceConfigFileName);

        return null;
    }

    protected override async Task<Option<Err[]>> StopService(string serviceEnvName, CancellationToken cancellationToken)
    {
        var stopProcessResult = StShared.RunProcess(UseConsole, _logger, "systemctl",
            $"--no-ask-password --no-block --quiet stop {serviceEnvName}");

        return stopProcessResult.IsSome
            ? await Task.FromResult(Err.RecreateErrors((Err[])stopProcessResult,
                InstallerErrors.TheServiceWasNotStopped))
            : null;
    }

    protected override async Task<Option<Err[]>> StartService(string serviceEnvName,
        CancellationToken cancellationToken)
    {
        var startProcessResult = StShared.RunProcess(UseConsole, _logger, "systemctl",
            $"--no-ask-password --no-block --quiet start {serviceEnvName}");

        return startProcessResult.IsSome
            ? await Task.FromResult(Err.RecreateErrors((Err[])startProcessResult,
                InstallerErrors.TheServiceWasNotStarted))
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

    private async Task<OneOf<string, Err[]>> GenerateServiceFileText(string projectName, string serviceDescription,
        string installFolderPath, string serviceUserName, string dotnetRunner, string? serviceDescriptionSignature,
        string? projectDescription, CancellationToken cancellationToken)
    {
        var checkedDotnetRunnerResult = CheckDotnetRunner(dotnetRunner);
        if (checkedDotnetRunnerResult.IsT1)
            return await LogErrorAndSendMessageFromError(LinuxServiceInstallerErrors.DotnetLocationIsNotFound,
                cancellationToken);

        var checkedDotnetRunner = checkedDotnetRunnerResult.AsT0;

        var mainDllFileName = Path.Combine(installFolderPath, $"{projectName}.dll");
        var syslogIdentifier = serviceDescription.Replace(".", "");

        return $"""
                [Unit]
                Description={serviceDescription} service {serviceDescriptionSignature ?? ""} {projectDescription ?? ""}

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

        await LogInfoAndSendMessage("Create service file {0}", serviceConfigFileName,
            cancellationToken);
        await File.WriteAllTextAsync(serviceConfigFileName, (string?)serviceFileText, cancellationToken);

        await LogInfoAndSendMessage("Enable service {0}", serviceEnvName, cancellationToken);
        var processResult = StShared.RunProcess(UseConsole, _logger, "systemctl",
            $"--no-ask-password --no-block --quiet enable {serviceEnvName}");

        if (processResult.IsSome)
            return await LogErrorAndSendMessageFromError(
                LinuxServiceInstallerErrors.ServiceCanNotBeEnabled(serviceEnvName), cancellationToken);
        if (IsServiceExists(serviceEnvName))
            return null;
        return await LogErrorAndSendMessageFromError(LinuxServiceInstallerErrors.ServiceIsNotEnabled(serviceEnvName),
            cancellationToken);
    }

    private OneOf<string, Err[]> CheckDotnetRunner(string? dotnetRunner)
    {
        if (!string.IsNullOrWhiteSpace(dotnetRunner) && File.Exists(dotnetRunner))
            return dotnetRunner;
        var runProcessWithOutputResult = StShared.RunProcessWithOutput(UseConsole, _logger, "which", "dotnet");
        if (runProcessWithOutputResult.IsT1)
            return Err.RecreateErrors(runProcessWithOutputResult.AsT1, LinuxServiceInstallerErrors.WhichDotnetError);
        var newDotnetRunner = runProcessWithOutputResult.AsT0.Item1;
        if (!string.IsNullOrWhiteSpace(newDotnetRunner) && File.Exists(newDotnetRunner))
            return newDotnetRunner;
        return new[] { LinuxServiceInstallerErrors.DotnetDetectError };
    }

    protected override async Task<Option<Err[]>> ChangeOneFileOwner(string filePath, string? filesUserName,
        string? filesUsersGroupName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return await LogErrorAndSendMessageFromError(InstallerErrors.FileNameIsEmpty,
                cancellationToken);

        if (string.IsNullOrWhiteSpace(filesUserName))
        {
            await LogWarningAndSendMessage("user name is empty. owner not changed", cancellationToken);
            return null;
        }

        if (File.Exists(filePath))
            return StShared.RunProcess(UseConsole, _logger, "chown",
                $"{filesUserName}{(string.IsNullOrWhiteSpace(filesUsersGroupName) ? "" : $":{filesUsersGroupName}")} {filePath}");

        return await LogErrorAndSendMessageFromError(InstallerErrors.FileIsNotExists(filePath), cancellationToken);
    }

    protected override async Task<Option<Err[]>> ChangeFolderOwner(string folderPath, string filesUserName,
        string filesUsersGroupName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            return await LogErrorAndSendMessageFromError(InstallerErrors.FolderNameIsEmpty, cancellationToken);

        if (string.IsNullOrWhiteSpace(filesUserName))
        {
            await LogWarningAndSendMessage("user name is empty. owner not changed", cancellationToken);
            return null;
        }

        if (Directory.Exists(folderPath))
            return StShared.RunProcess(UseConsole, _logger, "chown",
                $"-R {filesUserName}{(string.IsNullOrWhiteSpace(filesUsersGroupName) ? "" : $":{filesUsersGroupName}")} {folderPath}");

        return await LogErrorAndSendMessageFromError(InstallerErrors.FolderOwnerCanNotBeChanged(folderPath),
            cancellationToken);
    }
}