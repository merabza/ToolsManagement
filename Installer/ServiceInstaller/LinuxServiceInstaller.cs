using System.IO;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace Installer.ServiceInstaller;

public sealed class LinuxServiceInstaller : InstallerBase
{
    private readonly string _dotnetRunner;

    public LinuxServiceInstaller(bool useConsole, ILogger logger, string dotnetRunner,
        IMessagesDataManager? messagesDataManager, string? userName) : base(useConsole, logger, "linux-x64",
        messagesDataManager, userName)
    {
        _dotnetRunner = dotnetRunner;
    }

    public LinuxServiceInstaller(bool useConsole, ILogger logger, IMessagesDataManager? messagesDataManager,
        string? userName) : base(useConsole, logger, "linux-x64", messagesDataManager, userName)
    {
        _dotnetRunner = "";
    }

    protected override bool IsServiceExists(string serviceName)
    {
        var serviceConfigFileName = GetServiceConfigFileName(serviceName);
        return File.Exists(serviceConfigFileName);
    }

    private static string GetServiceConfigFileName(string serviceName)
    {
        var systemFolderFullName = "/etc/systemd/system";
        var serviceFileName = $"{serviceName}.service";
        var serviceConfigFileName = Path.Combine(systemFolderFullName, serviceFileName);
        return serviceConfigFileName;
    }

    protected override bool IsServiceRunning(string serviceName)
    {
        return StShared.RunProcess(UseConsole, Logger, "systemctl",
            $"--no-ask-password --no-block --quiet is-active {serviceName}");
    }

    protected override bool RemoveService(string serviceName)
    {
        var serviceConfigFileName = GetServiceConfigFileName(serviceName);

        if (!StShared.RunProcess(UseConsole, Logger, "systemctl",
                $"--no-ask-password --no-block --quiet disable {serviceName}"))
            return false;

        File.Delete(serviceConfigFileName);

        return true;
    }

    protected override bool StopService(string serviceName)
    {
        return StShared.RunProcess(UseConsole, Logger, "systemctl",
            $"--no-ask-password --no-block --quiet stop {serviceName}");
    }

    protected override bool StartService(string serviceName)
    {
        return StShared.RunProcess(UseConsole, Logger, "systemctl",
            $"--no-ask-password --no-block --quiet start {serviceName}");
    }

    protected override bool IsServiceRegisteredProperly(string projectName, string serviceName, string serviceUserName,
        string installFolderPath)
    {
        var serviceConfigFileName = GetServiceConfigFileName(serviceName);

        var serviceFileText =
            GenerateServiceFileText(serviceName, installFolderPath, serviceUserName, _dotnetRunner);

        var existingServiceFileText = File.ReadAllText(serviceConfigFileName);

        return serviceFileText == existingServiceFileText;
    }

    private string? GenerateServiceFileText(string serviceName, string installFolderPath, string serviceUserName,
        string dotnetRunner)
    {
        var checkedDotnetRunner = CheckDotnetRunner(dotnetRunner);
        if (checkedDotnetRunner == null)
        {
            MessagesDataManager?.SendMessage(UserName, "dotnet location can not found").Wait();
            Logger.LogError("dotnet location can not found");
            return null;
        }

        var mainDllFileName = Path.Combine(installFolderPath, $"{serviceName}.dll");
        var syslogIdentifier = serviceName.Replace(".", "");

        return $@"[Unit]
Description={serviceName} service

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
";
    }


    protected override bool RegisterService(string projectName, string serviceName, string serviceUserName,
        string installFolderPath)
    {
        var serviceConfigFileName = GetServiceConfigFileName(serviceName);

        var serviceFileText =
            GenerateServiceFileText(serviceName, installFolderPath, serviceUserName, _dotnetRunner);

        MessagesDataManager?.SendMessage(UserName, $"Create service file {serviceConfigFileName}").Wait();
        Logger.LogInformation("Create service file {serviceConfigFileName}", serviceConfigFileName);
        File.WriteAllText(serviceConfigFileName, serviceFileText);

        MessagesDataManager?.SendMessage(UserName, $"Enable service {serviceName}").Wait();
        Logger.LogInformation("Enable service {serviceName}", serviceName);
        if (StShared.RunProcess(UseConsole, Logger, "systemctl",
                $"--no-ask-password --no-block --quiet enable {serviceName}"))
            return IsServiceExists(serviceName);

        MessagesDataManager?.SendMessage(UserName, $"Service {serviceName} can not enabled").Wait();
        Logger.LogError("Service {serviceName} can not enabled", serviceName);
        return false;
    }

    private string? CheckDotnetRunner(string? dotnetRunner)
    {
        if (!string.IsNullOrWhiteSpace(dotnetRunner) && File.Exists(dotnetRunner))
            return dotnetRunner;
        var newDotnetRunner = StShared.RunProcessWithOutput(UseConsole, Logger, "which", "dotnet");
        if (!string.IsNullOrWhiteSpace(newDotnetRunner) && File.Exists(newDotnetRunner))
            return newDotnetRunner;
        return null;
    }

    protected override bool ChangeOneFileOwner(string filePath, string? filesUserName, string? filesUsersGroupName)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            MessagesDataManager?.SendMessage(UserName, "File name is empty").Wait();
            Logger.LogError("File name is empty");
            return false;
        }

        if (string.IsNullOrWhiteSpace(filesUserName))
        {
            MessagesDataManager?.SendMessage(UserName, "user name is empty. owner not changed").Wait();
            Logger.LogWarning("user name is empty. owner not changed");
            return true;
        }

        if (File.Exists(filePath))
            return StShared.RunProcess(UseConsole, Logger, "chown",
                $"{filesUserName}{(string.IsNullOrWhiteSpace(filesUsersGroupName) ? "" : $":{filesUsersGroupName}")} {filePath}");

        MessagesDataManager?.SendMessage(UserName, $"Error changing owner to file {filePath}").Wait();
        Logger.LogError("Error changing owner to file {filePath}", filePath);
        return false;
    }

    protected override bool ChangeOwner(string folderPath, string filesUserName, string filesUsersGroupName)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            MessagesDataManager?.SendMessage(UserName, "Folder name is empty").Wait();
            Logger.LogError("Folder name is empty");
            return false;
        }

        if (string.IsNullOrWhiteSpace(filesUserName))
        {
            MessagesDataManager?.SendMessage(UserName, "user name is empty. owner not changed").Wait();
            Logger.LogWarning("user name is empty. owner not changed");
            return true;
        }

        if (Directory.Exists(folderPath))
            return StShared.RunProcess(UseConsole, Logger, "chown",
                $"-R {filesUserName}{(string.IsNullOrWhiteSpace(filesUsersGroupName) ? "" : $":{filesUsersGroupName}")} {folderPath}");

        MessagesDataManager?.SendMessage(UserName, $"Error changing owner to folder {folderPath}").Wait();
        Logger.LogError("Error changing owner to folder {folderPath}", folderPath);
        return false;
    }
}