using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;
using ToolsManagement.Installer.Errors;

namespace ToolsManagement.Installer.ServiceInstaller;

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
        _dotnetRunner = string.Empty;
    }

    protected override bool IsServiceExists(string serviceEnvName)
    {
        string serviceConfigFileName = GetServiceConfigFileName(serviceEnvName);
        return File.Exists(serviceConfigFileName);
    }

    private static string GetServiceConfigFileName(string serviceEnvName)
    {
        const string systemFolderFullName = "/etc/systemd/system";
        string serviceFileName = $"{serviceEnvName}.service";
        string serviceConfigFileName = Path.Combine(systemFolderFullName, serviceFileName);
        return serviceConfigFileName;
    }

    protected override bool IsServiceRunning(string serviceEnvName)
    {
        //is-active აბრუნებს 0-ს active მდგომარეობისას, 3-ს — გაჩერებულის/არარსებულის.
        //3 დასაშვებ კოდად ითვლება, რომ გაჩერებულმა სერვისმა (ნორმალური შემთხვევა) error-ლოგი არ გამოიწვიოს.
        Result<(string, int)> result = StShared.RunProcessWithOutput(UseConsole, _logger, "systemctl",
            $"--no-ask-password --quiet is-active {serviceEnvName}", [3]);
        return result.IsSuccess && result.Value.Item2 == 0;
    }

    protected override async ValueTask<Result> RemoveService(string serviceEnvName,
        CancellationToken cancellationToken = default)
    {
        string serviceConfigFileName = GetServiceConfigFileName(serviceEnvName);

        Result disableProcessResult = StShared.RunProcess(UseConsole, _logger, "systemctl",
            $"--no-ask-password --no-block --quiet disable {serviceEnvName}", [1]);

        if (disableProcessResult.IsFailure)
        {
            return await Task.FromResult(Result.CreateValidationError([
                .. disableProcessResult.Error.ToErrorArray(), InstallerErrors.TheServiceWasNotRemoved
            ]));
        }

        File.Delete(serviceConfigFileName);

        return Result.Success();
    }

    protected override async ValueTask<Result> StopService(string serviceEnvName,
        CancellationToken cancellationToken = default)
    {
        //--no-block განზრახ მოშორებულია: stop უნდა იყოს სინქრონული, რომ systemd დაელოდოს
        //უნიტის სრულ გაჩერებას (საჭიროების შემთხვევაში მოკვლას TimeoutStopSec-ის შემდეგ),
        //თორემ ძველი პროცესი კვლავ იკავებს TCP პორტს და განახლება ჩაიშლება.
        Result stopProcessResult = StShared.RunProcess(UseConsole, _logger, "systemctl",
            $"--no-ask-password --quiet stop {serviceEnvName}");

        if (stopProcessResult.IsFailure)
        {
            return await Task.FromResult(Result.CreateValidationError([
                .. stopProcessResult.Error.ToErrorArray(), InstallerErrors.TheServiceWasNotStopped
            ]));
        }

        return Result.Success();
    }

    //ძველი (შესაძლოა ობოლი) პროცესის PID-ის დადგენა მთავარი dll-ის გზით და მისი მოკვლა PID-ით.
    protected override async ValueTask<Result> KillProcessByPid(string serviceEnvName, string projectName,
        string installFolderPath, CancellationToken cancellationToken = default)
    {
        //გაშვებული პროცესის ამოცნობა ხდება მთავარი dll-ის სრული გზით — ეს მუშაობს მაშინაც,
        //როცა systemd-ს პროცესი აღარ აკონტროლებს (ობოლი პროცესი წინა გაუმართავი განახლებიდან).
        string mainDllFileName = Path.Combine(installFolderPath, $"{projectName}.dll");

        //pgrep -f პოულობს პროცესებს, რომელთა ბრძანების ხაზი შეიცავს ამ გზას, და აბრუნებს PID-ებს.
        //pgrep აბრუნებს 1-ს, თუ ვერცერთი პროცესი ვერ მოიძებნა — ეს ნორმალური (უშეცდომო) შემთხვევაა.
        Result<(string, int)> pgrepResult =
            StShared.RunProcessWithOutput(UseConsole, _logger, "pgrep", $"-f \"{mainDllFileName}\"", [1]);
        if (pgrepResult.IsFailure)
        {
            //pgrep ვერ შესრულდა (მაგ. დაყენებული არ არის) — გავაფრთხილოთ და გავაგრძელოთ.
            await LogWarningAndSendMessage("Cannot determine running process PID for {0}", mainDllFileName,
                cancellationToken);
            return Result.Success();
        }

        (string pgrepOutput, _) = pgrepResult.Value;

        string[] pidStrings =
            pgrepOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (pidStrings.Length == 0)
        {
            await LogInfoAndSendMessage("No running process found for {0}", mainDllFileName, cancellationToken);
            return Result.Success();
        }

        foreach (string pidString in pidStrings)
        {
            if (!int.TryParse(pidString, CultureInfo.InvariantCulture, out int processId) || processId <= 0)
            {
                continue;
            }

            await LogInfoAndSendMessage("Killing process with PID {0} for {1}", processId, mainDllFileName,
                cancellationToken);

            //ვკლავთ კონკრეტული PID-ის მიხედვით SIGKILL-ით. თუ პროცესი უკვე აღარ არსებობს,
            //kill აბრუნებს 1-ს — ამ შემთხვევას დასაშვებად ვთვლით.
            Result killResult = StShared.RunProcess(UseConsole, _logger, "kill", $"-9 {processId}", [1]);
            if (killResult.IsFailure)
            {
                return await LogErrorAndSendMessageFromError(
                    LinuxServiceInstallerErrors.ProcessCanNotBeKilled(processId), cancellationToken);
            }
        }

        return Result.Success();
    }

    protected override async ValueTask<Result> StartService(string serviceEnvName,
        CancellationToken cancellationToken = default)
    {
        Result startProcessResult = StShared.RunProcess(UseConsole, _logger, "systemctl",
            $"--no-ask-password --no-block --quiet start {serviceEnvName}");

        if (startProcessResult.IsFailure)
        {
            return await Task.FromResult(Result.CreateValidationError([
                .. startProcessResult.Error.ToErrorArray(), InstallerErrors.TheServiceWasNotStarted
            ]));
        }

        return Result.Success();
    }

    protected override async ValueTask<Result<bool>> IsServiceRegisteredProperly(string projectName,
        string serviceEnvName, string serviceUserName, string installFolderPath, string? serviceDescriptionSignature,
        string? projectDescription, CancellationToken cancellationToken = default)
    {
        string serviceConfigFileName = GetServiceConfigFileName(serviceEnvName);

        Result<string> generateServiceFileTextResult = await GenerateServiceFileText(projectName, serviceEnvName,
            installFolderPath, serviceUserName, _dotnetRunner, serviceDescriptionSignature, projectDescription,
            cancellationToken);

        if (generateServiceFileTextResult.IsFailure)
        {
            return generateServiceFileTextResult.Error;
        }

        string serviceFileText = generateServiceFileTextResult.Value;

        string existingServiceFileText = await File.ReadAllTextAsync(serviceConfigFileName, cancellationToken);

        return serviceFileText == existingServiceFileText;
    }

    private async ValueTask<Result<string>> GenerateServiceFileText(string projectName, string serviceDescription,
        string installFolderPath, string serviceUserName, string dotnetRunner, string? serviceDescriptionSignature,
        string? projectDescription, CancellationToken cancellationToken = default)
    {
        Result<string> checkedDotnetRunnerResult = CheckDotnetRunner(dotnetRunner);
        if (checkedDotnetRunnerResult.IsFailure)
        {
            ValidationError error = Result.CreateValidationError([
                .. checkedDotnetRunnerResult.Error.ToErrorArray(), LinuxServiceInstallerErrors.DotnetLocationIsNotFound
            ]);

            return await LogErrorAndSendMessageFromError(error, cancellationToken);
        }

        string checkedDotnetRunner = checkedDotnetRunnerResult.Value;

        string mainDllFileName = Path.Combine(installFolderPath, $"{projectName}.dll");
        string syslogIdentifier = serviceDescription.Replace(".", string.Empty);

        return $"""
                [Unit]
                Description={serviceDescription} service {serviceDescriptionSignature ?? string.Empty} {projectDescription ?? string.Empty}

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

    protected override async ValueTask<Result> RegisterService(string projectName, string serviceEnvName,
        string serviceUserName, string installFolderPath, string? serviceDescriptionSignature,
        string? projectDescription, CancellationToken cancellationToken = default)
    {
        string serviceConfigFileName = GetServiceConfigFileName(serviceEnvName);

        Result<string> generateServiceFileTextResult = await GenerateServiceFileText(projectName, serviceEnvName,
            installFolderPath, serviceUserName, _dotnetRunner, serviceDescriptionSignature, projectDescription,
            cancellationToken);
        if (generateServiceFileTextResult.IsFailure)
        {
            return generateServiceFileTextResult.Error;
        }

        string serviceFileText = generateServiceFileTextResult.Value;

        await LogInfoAndSendMessage("Create service file {0}", serviceConfigFileName, cancellationToken);
        await File.WriteAllTextAsync(serviceConfigFileName, serviceFileText, cancellationToken);

        await LogInfoAndSendMessage("Enable service {0}", serviceEnvName, cancellationToken);
        Result processResult = StShared.RunProcess(UseConsole, _logger, "systemctl",
            $"--no-ask-password --no-block --quiet enable {serviceEnvName}");

        if (processResult.IsFailure)
        {
            return await LogErrorAndSendMessageFromError(
                LinuxServiceInstallerErrors.ServiceCanNotBeEnabled(serviceEnvName), cancellationToken);
        }

        if (IsServiceExists(serviceEnvName))
        {
            return Result.Success();
        }

        return await LogErrorAndSendMessageFromError(LinuxServiceInstallerErrors.ServiceIsNotEnabled(serviceEnvName),
            cancellationToken);
    }

    private Result<string> CheckDotnetRunner(string? dotnetRunner)
    {
        if (!string.IsNullOrWhiteSpace(dotnetRunner) && File.Exists(dotnetRunner))
        {
            return dotnetRunner;
        }

        Result<(string, int)> runProcessWithOutputResult =
            StShared.RunProcessWithOutput(UseConsole, _logger, "which", "dotnet");
        if (runProcessWithOutputResult.IsFailure)
        {
            return Result.CreateValidationError([
                .. runProcessWithOutputResult.Error.ToErrorArray(), LinuxServiceInstallerErrors.WhichDotnetError
            ]);
        }

        string newDotnetRunner = runProcessWithOutputResult.Value.Item1.Trim('\0', ' ', '\t', '\r', '\n');
        if (!string.IsNullOrWhiteSpace(newDotnetRunner) && File.Exists(newDotnetRunner))
        {
            return newDotnetRunner;
        }

        return LinuxServiceInstallerErrors.DotnetDetectError;
    }

    protected override async ValueTask<Result> ChangeOneFileOwner(string filePath, string? filesUserName,
        string? filesUsersGroupName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return await LogErrorAndSendMessageFromError(InstallerErrors.FileNameIsEmpty, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(filesUserName))
        {
            await LogWarningAndSendMessage("user name is empty. owner not changed", cancellationToken);
            return Result.Success();
        }

        if (File.Exists(filePath))
        {
            return StShared.RunProcess(UseConsole, _logger, "chown",
                $"{filesUserName}{(string.IsNullOrWhiteSpace(filesUsersGroupName) ? string.Empty : $":{filesUsersGroupName}")} {filePath}");
        }

        return await LogErrorAndSendMessageFromError(InstallerErrors.FileIsNotExists(filePath), cancellationToken);
    }

    protected override async ValueTask<Result> ChangeFolderOwner(string folderPath, string filesUserName,
        string filesUsersGroupName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return await LogErrorAndSendMessageFromError(InstallerErrors.FolderNameIsEmpty, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(filesUserName))
        {
            await LogWarningAndSendMessage("user name is empty. owner not changed", cancellationToken);
            return Result.Success();
        }

        if (Directory.Exists(folderPath))
        {
            return StShared.RunProcess(UseConsole, _logger, "chown",
                $"-R {filesUserName}{(string.IsNullOrWhiteSpace(filesUsersGroupName) ? string.Empty : $":{filesUsersGroupName}")} {folderPath}");
        }

        return await LogErrorAndSendMessageFromError(InstallerErrors.FolderOwnerCanNotBeChanged(folderPath),
            cancellationToken);
    }
}
