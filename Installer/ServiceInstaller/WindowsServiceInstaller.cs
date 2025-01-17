using Installer.Errors;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using OneOf;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using SystemToolsShared;
using SystemToolsShared.Errors;

namespace Installer.ServiceInstaller;

public sealed class WindowsServiceInstaller : InstallerBase
{
    private readonly string? _projectDescription;
    private readonly string? _serviceDescriptionSignature;

    //public WindowsServiceInstaller(bool useConsole, ILogger logger, string? serviceDescriptionSignature,
    //    string? projectDescription, IMessagesDataManager? messagesDataManager, string? userName) : base(useConsole,
    //    logger, "win10-x64", messagesDataManager, userName)
    //{
    //    _serviceDescriptionSignature = serviceDescriptionSignature;
    //    _projectDescription = projectDescription;
    //}

    // ReSharper disable once ConvertToPrimaryConstructor
    public WindowsServiceInstaller(bool useConsole, ILogger logger, IMessagesDataManager? messagesDataManager,
        string? userName) : base(useConsole, logger, "win10-x64", messagesDataManager, userName)
    {
        _serviceDescriptionSignature = null;
        _projectDescription = null;
    }

    protected override bool IsServiceExists(string serviceEnvName)
    {
#pragma warning disable CA1416 // Validate platform compatibility
        // ReSharper disable once using
        using var sc = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == serviceEnvName);
        return sc != null;
#pragma warning restore CA1416 // Validate platform compatibility
    }

    protected override bool IsServiceRunning(string serviceEnvName)
    {
#pragma warning disable CA1416 // Validate platform compatibility
        // ReSharper disable once using
        using var sc = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == serviceEnvName);
        if (sc == null)
            return false;
        return !(sc.Status.Equals(ServiceControllerStatus.Stopped) ||
                 sc.Status.Equals(ServiceControllerStatus.StopPending));
#pragma warning restore CA1416 // Validate platform compatibility
    }

    protected override Option<IEnumerable<Err>> RemoveService(string serviceEnvName)
    {
#pragma warning disable CA1416 // Validate platform compatibility
        // ReSharper disable once using
        // ReSharper disable once DisposableConstructor
        using var sc = new ServiceController(serviceEnvName);
        sc.Refresh();
        if (!(sc.Status.Equals(ServiceControllerStatus.Stopped) ||
              sc.Status.Equals(ServiceControllerStatus.StopPending)))
            return new[] { InstallerErrors.ServiceIsRunningAndCanNotBeRemoved(serviceEnvName) };
#pragma warning restore CA1416 // Validate platform compatibility

        // create empty pipeline
        // ReSharper disable once using
        using var ps = PowerShell.Create();

        // add command
        ps.AddCommand("Remove-Service").AddParameter("Name", serviceEnvName);

        ps.Invoke();

        return null;
    }

    protected override async ValueTask<Option<IEnumerable<Err>>> StopService(string serviceEnvName,
        CancellationToken cancellationToken = default)
    {
#pragma warning disable CA1416 // Validate platform compatibility

        // ReSharper disable once using
        // ReSharper disable once DisposableConstructor
        using var sc = new ServiceController(serviceEnvName);

        if (sc.Status.Equals(ServiceControllerStatus.Stopped) || sc.Status.Equals(ServiceControllerStatus.StopPending))
            return null;

        await LogInfoAndSendMessage("Stopping the {0} service...", serviceEnvName, cancellationToken);

        sc.Stop();
        sc.WaitForStatus(ServiceControllerStatus.Stopped);

        // Refresh and display the current service status.
        sc.Refresh();

        var status = sc.Status;

#pragma warning restore CA1416 // Validate platform compatibility

        await LogInfoAndSendMessage("The {0} service status is now set to {1}", serviceEnvName, status,
            cancellationToken);

        if (status != ServiceControllerStatus.Stopped)
            return await LogErrorAndSendMessageFromError(InstallerErrors.ServiceIsNotStopped(serviceEnvName),
                cancellationToken);

        return null;
    }

    protected override async ValueTask<Option<IEnumerable<Err>>> StartService(string serviceEnvName,
        CancellationToken cancellationToken = default)
    {
#pragma warning disable CA1416 // Validate platform compatibility

        // ReSharper disable once using
        // ReSharper disable once DisposableConstructor
        using var sc = new ServiceController(serviceEnvName);
        sc.Refresh();
        if (!(sc.Status.Equals(ServiceControllerStatus.Stopped) ||
              sc.Status.Equals(ServiceControllerStatus.StopPending)))
            return null;

        await LogInfoAndSendMessage("Starting the {0} service...", serviceEnvName, cancellationToken);

        sc.Start();
        sc.WaitForStatus(ServiceControllerStatus.Running);
        // Refresh and display the current service status.
        sc.Refresh();

        var status = sc.Status;

#pragma warning restore CA1416 // Validate platform compatibility

        await LogInfoAndSendMessage("The {0} service status is now set to {1}", serviceEnvName, status,
            cancellationToken);

        if (status != ServiceControllerStatus.Running)
            return await LogErrorAndSendMessageFromError(InstallerErrors.ServiceCanNotBeStarted(serviceEnvName),
                cancellationToken);

        return null;
    }

    protected override async ValueTask<Option<IEnumerable<Err>>> ChangeOneFileOwner(string filePath,
        string? filesUserName, string? filesUsersGroupName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return await LogErrorAndSendMessageFromError(InstallerErrors.FileNameIsEmpty, cancellationToken);

        if (!File.Exists(filePath))
            return await LogErrorAndSendMessageFromError(InstallerErrors.FileIsNotExists(filePath), cancellationToken);

        var userName = "NT AUTHORITY\\LOCAL SERVICE";
        if (!string.IsNullOrWhiteSpace(filesUserName))
            userName = filesUserName;

        var file = new FileInfo(filePath);
#pragma warning disable CA1416 // Validate platform compatibility

        var dac = file.GetAccessControl();
        IdentityReference ir = new NTAccount(userName);
        var fsaRule = new FileSystemAccessRule(ir,
            FileSystemRights.Read & FileSystemRights.Write & FileSystemRights.ReadAndExecute,
            InheritanceFlags.ContainerInherit & InheritanceFlags.ObjectInherit, PropagationFlags.None,
            AccessControlType.Allow);
        dac.SetAccessRule(fsaRule);
        file.SetAccessControl(dac);

#pragma warning restore CA1416 // Validate platform compatibility
        return null;
    }

    protected override async ValueTask<Option<IEnumerable<Err>>> ChangeFolderOwner(string folderPath,
        string filesUserName, string filesUsersGroupName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            return await LogErrorAndSendMessageFromError(InstallerErrors.FolderNameIsEmpty, cancellationToken);

        if (!Directory.Exists(folderPath))
            return await LogErrorAndSendMessageFromError(InstallerErrors.FolderIsNotExists(folderPath),
                cancellationToken);

        var userName = "NT AUTHORITY\\LOCAL SERVICE";
        if (!string.IsNullOrWhiteSpace(filesUserName)) userName = filesUserName;

        var installFolder = new DirectoryInfo(folderPath);
#pragma warning disable CA1416 // Validate platform compatibility

        var dac = installFolder.GetAccessControl();
        IdentityReference ir = new NTAccount(userName);
        var fsaRule = new FileSystemAccessRule(ir,
            FileSystemRights.Read & FileSystemRights.Write & FileSystemRights.ReadAndExecute,
            InheritanceFlags.ContainerInherit & InheritanceFlags.ObjectInherit, PropagationFlags.None,
            AccessControlType.Allow);
        dac.SetAccessRule(fsaRule);
        installFolder.SetAccessControl(dac);

#pragma warning restore CA1416 // Validate platform compatibility
        return null;
    }

    protected override async ValueTask<OneOf<bool, IEnumerable<Err>>> IsServiceRegisteredProperly(string projectName,
        string serviceEnvName, string userName, string installFolderPath, string? serviceDescriptionSignature,
        string? projectDescription, CancellationToken cancellationToken = default)
    {
        var exeFilePath = Path.Combine(installFolderPath, $"{projectName}.exe");
        var mustBeDescription =
            $"{serviceEnvName} service {_serviceDescriptionSignature ?? string.Empty} {_projectDescription ?? string.Empty}";
        //თუ სერვისი უკვე დარეგისტრირებულია და გვინდა დავადგინოთ გამშვები ფაილი რომელია, გვაქვს 2 გზა
        //1. გამოვიყენოთ sc qc <service name> და გავარჩიოთ რას დააბრუნებს
        //2. HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services.
        //   Find the service you want to redirect,
        //   locate the ImagePath subkey value.
#pragma warning disable CA1416 // Validate platform compatibility
        // ReSharper disable once using
        using var regKey = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\services\{serviceEnvName}");
        var imagePath = regKey?.GetValue("ImagePath")?.ToString();
        var description = regKey?.GetValue("Description")?.ToString();
#pragma warning restore CA1416 // Validate platform compatibility

        var toReturn = imagePath is not null && imagePath == exeFilePath && description is not null &&
                       description == mustBeDescription;
        return await Task.FromResult(toReturn);
    }

    protected override async ValueTask<Option<IEnumerable<Err>>> RegisterService(string projectName,
        string serviceEnvName, string serviceUserName, string installFolderPath, string? serviceDescriptionSignature,
        string? projectDescription, CancellationToken cancellationToken = default)
    {
        // create empty pipeline
        // ReSharper disable once using
        using var ps = PowerShell.Create();

        // add command
        var exeFilePath = Path.Combine(installFolderPath, $"{projectName}.exe");
        ps.AddCommand("New-Service").AddParameter("Name", serviceEnvName)
            .AddParameter("Description",
                $"{serviceEnvName} service {_serviceDescriptionSignature ?? string.Empty} {_projectDescription ?? string.Empty}")
            .AddParameter("BinaryPathName", exeFilePath).AddParameter("StartupType", "Automatic");

        await ps.InvokeAsync();

        if (IsServiceExists(serviceEnvName))
            return null;

        return await LogErrorAndSendMessageFromError(InstallerErrors.ServiceIsNotExists(serviceEnvName),
            cancellationToken);
    }
}