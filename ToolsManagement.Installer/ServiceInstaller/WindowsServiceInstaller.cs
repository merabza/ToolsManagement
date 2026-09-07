using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;
using ToolsManagement.Installer.Errors;

namespace ToolsManagement.Installer.ServiceInstaller;

public sealed class WindowsServiceInstaller : InstallerBase
{
    private readonly string? _projectDescription;
    private readonly string? _serviceDescriptionSignature;

    // ReSharper disable once ConvertToPrimaryConstructor
    public WindowsServiceInstaller(bool useConsole, ILogger logger, IMessagesDataManager? messagesDataManager,
        string? userName) : base(useConsole, logger, "win-x64", messagesDataManager, userName)
    {
        _serviceDescriptionSignature = null;
        _projectDescription = null;
    }

    protected override bool IsServiceExists(string serviceEnvName)
    {
#pragma warning disable CA1416 // Validate platform compatibility
        // ReSharper disable once using
        using ServiceController? sc = ServiceController.GetServices()
            .FirstOrDefault(s => s.ServiceName == serviceEnvName);
        return sc != null;
#pragma warning restore CA1416 // Validate platform compatibility
    }

    protected override bool IsServiceRunning(string serviceEnvName)
    {
#pragma warning disable CA1416 // Validate platform compatibility
        // ReSharper disable once using
        using ServiceController? sc = ServiceController.GetServices()
            .FirstOrDefault(s => s.ServiceName == serviceEnvName);
        if (sc == null)
        {
            return false;
        }

        return !(sc.Status.Equals(ServiceControllerStatus.Stopped) ||
                 sc.Status.Equals(ServiceControllerStatus.StopPending));
#pragma warning restore CA1416 // Validate platform compatibility
    }

    protected override async ValueTask<Result> RemoveService(string serviceEnvName,
        CancellationToken cancellationToken = default)
    {
#pragma warning disable CA1416 // Validate platform compatibility
        // ReSharper disable once using
        // ReSharper disable once DisposableConstructor
        using var sc = new ServiceController(serviceEnvName);
        sc.Refresh();
        if (!(sc.Status.Equals(ServiceControllerStatus.Stopped) ||
              sc.Status.Equals(ServiceControllerStatus.StopPending)))
        {
            return InstallerErrors.ServiceIsRunningAndCanNotBeRemoved(serviceEnvName);
        }
#pragma warning restore CA1416 // Validate platform compatibility

        // create empty pipeline
        // ReSharper disable once using
        using var ps = PowerShell.Create();

        // add command
        ps.AddCommand("Remove-Service").AddParameter("Name", serviceEnvName);

        return await InvokePowerShellAndCheckErrors(ps, nameof(RemoveService),
            nameof(InstallerErrors.ServiceCanNotBeRemoved), cancellationToken);
    }

    protected override async ValueTask<Result> StopService(string serviceEnvName,
        CancellationToken cancellationToken = default)
    {
#pragma warning disable CA1416 // Validate platform compatibility

        // ReSharper disable once using
        // ReSharper disable once DisposableConstructor
        using var sc = new ServiceController(serviceEnvName);

        if (sc.Status.Equals(ServiceControllerStatus.Stopped) || sc.Status.Equals(ServiceControllerStatus.StopPending))
        {
            return Result.Success();
        }

        await LogInfoAndSendMessage("Stopping the {0} service...", serviceEnvName, cancellationToken);

        sc.Stop();
        sc.WaitForStatus(ServiceControllerStatus.Stopped);

        // Refresh and display the current service status.
        sc.Refresh();

        ServiceControllerStatus status = sc.Status;

#pragma warning restore CA1416 // Validate platform compatibility

        await LogInfoAndSendMessage("The {0} service status is now set to {1}", serviceEnvName, status,
            cancellationToken);

        if (status != ServiceControllerStatus.Stopped)
        {
            return await LogErrorAndSendMessageFromError(InstallerErrors.ServiceIsNotStopped(serviceEnvName),
                cancellationToken);
        }

        return Result.Success();
    }

    protected override async ValueTask<Result> StartService(string serviceEnvName,
        CancellationToken cancellationToken = default)
    {
#pragma warning disable CA1416 // Validate platform compatibility

        // ReSharper disable once using
        // ReSharper disable once DisposableConstructor
        using var sc = new ServiceController(serviceEnvName);
        sc.Refresh();
        if (!(sc.Status.Equals(ServiceControllerStatus.Stopped) ||
              sc.Status.Equals(ServiceControllerStatus.StopPending)))
        {
            return Result.Success();
        }

        await LogInfoAndSendMessage("Starting the {0} service...", serviceEnvName, cancellationToken);

        sc.Start();
        sc.WaitForStatus(ServiceControllerStatus.Running);
        // Refresh and display the current service status.
        sc.Refresh();

        ServiceControllerStatus status = sc.Status;

#pragma warning restore CA1416 // Validate platform compatibility

        await LogInfoAndSendMessage("The {0} service status is now set to {1}", serviceEnvName, status,
            cancellationToken);

        if (status != ServiceControllerStatus.Running)
        {
            return await LogErrorAndSendMessageFromError(InstallerErrors.ServiceCanNotBeStarted(serviceEnvName),
                cancellationToken);
        }

        return Result.Success();
    }

    protected override async ValueTask<Result> ChangeOneFileOwner(string filePath, string? filesUserName,
        string? filesUsersGroupName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return await LogErrorAndSendMessageFromError(InstallerErrors.FileNameIsEmpty, cancellationToken);
        }

        if (!File.Exists(filePath))
        {
            return await LogErrorAndSendMessageFromError(InstallerErrors.FileIsNotExists(filePath), cancellationToken);
        }

        string userName = "NT AUTHORITY\\LOCAL SERVICE";
        if (!string.IsNullOrWhiteSpace(filesUserName))
        {
            userName = filesUserName;
        }

        var file = new FileInfo(filePath);
#pragma warning disable CA1416 // Validate platform compatibility

        FileSecurity dac = file.GetAccessControl();
        IdentityReference ir = new NTAccount(userName);
        var fsaRule = new FileSystemAccessRule(ir,
            FileSystemRights.Read & FileSystemRights.Write & FileSystemRights.ReadAndExecute,
            InheritanceFlags.ContainerInherit & InheritanceFlags.ObjectInherit, PropagationFlags.None,
            AccessControlType.Allow);
        dac.SetAccessRule(fsaRule);
        file.SetAccessControl(dac);

#pragma warning restore CA1416 // Validate platform compatibility
        return Result.Success();
    }

    protected override async ValueTask<Result> ChangeFolderOwner(string folderPath, string filesUserName,
        string filesUsersGroupName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return await LogErrorAndSendMessageFromError(InstallerErrors.FolderNameIsEmpty, cancellationToken);
        }

        if (!Directory.Exists(folderPath))
        {
            return await LogErrorAndSendMessageFromError(InstallerErrors.FolderIsNotExists(folderPath),
                cancellationToken);
        }

        string userName = "NT AUTHORITY\\LOCAL SERVICE";
        if (!string.IsNullOrWhiteSpace(filesUserName))
        {
            userName = filesUserName;
        }

        var installFolder = new DirectoryInfo(folderPath);
#pragma warning disable CA1416 // Validate platform compatibility

        DirectorySecurity dac = installFolder.GetAccessControl();
        IdentityReference ir = new NTAccount(userName);
        var fsaRule = new FileSystemAccessRule(ir,
            FileSystemRights.Read & FileSystemRights.Write & FileSystemRights.ReadAndExecute,
            InheritanceFlags.ContainerInherit & InheritanceFlags.ObjectInherit, PropagationFlags.None,
            AccessControlType.Allow);
        dac.SetAccessRule(fsaRule);
        installFolder.SetAccessControl(dac);

#pragma warning restore CA1416 // Validate platform compatibility
        return Result.Success();
    }

    protected override async ValueTask<Result<bool>> IsServiceRegisteredProperly(string projectName,
        string serviceEnvName, string serviceUserName, string installFolderPath, string? serviceDescriptionSignature,
        string? projectDescription, CancellationToken cancellationToken = default)
    {
        string exeFilePath = Path.Combine(installFolderPath, $"{projectName}.exe");
        string mustBeDescription =
            $"{serviceEnvName} service {_serviceDescriptionSignature ?? string.Empty} {_projectDescription ?? string.Empty}";
        //თუ სერვისი უკვე დარეგისტრირებულია და გვინდა დავადგინოთ გამშვები ფაილი რომელია, გვაქვს 2 გზა
        //1. გამოვიყენოთ sc qc <service name> და გავარჩიოთ რას დააბრუნებს
        //2. HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services.
        //   Find the service you want to redirect,
        //   locate the ImagePath subkey value.
#pragma warning disable CA1416 // Validate platform compatibility
        // ReSharper disable once using
        using RegistryKey? regKey =
            Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\services\{serviceEnvName}");
        string? imagePath = regKey?.GetValue("ImagePath")?.ToString();
        string? description = regKey?.GetValue("Description")?.ToString();
#pragma warning restore CA1416 // Validate platform compatibility

        bool toReturn = imagePath is not null && imagePath == exeFilePath && description is not null &&
                        description == mustBeDescription;
        return await Task.FromResult(toReturn);
    }

    protected override async ValueTask<Result> RegisterService(string projectName, string serviceEnvName,
        string serviceUserName, string installFolderPath, string? serviceDescriptionSignature,
        string? projectDescription, CancellationToken cancellationToken = default)
    {
        // create empty pipeline
        // ReSharper disable once using
        using var ps = PowerShell.Create();

        // add command
        string exeFilePath = Path.Combine(installFolderPath, $"{projectName}.exe");
        ps.AddCommand("New-Service").AddParameter("Name", serviceEnvName)
            .AddParameter("Description",
                $"{serviceEnvName} service {_serviceDescriptionSignature ?? string.Empty} {_projectDescription ?? string.Empty}")
            .AddParameter("BinaryPathName", exeFilePath).AddParameter("StartupType", "Automatic");

        Result invokeResult = await InvokePowerShellAndCheckErrors(ps, nameof(RegisterService),
            nameof(InstallerErrors.CannotRegisterService), cancellationToken);
        if (invokeResult.IsFailure)
        {
            return invokeResult;
        }

        if (IsServiceExists(serviceEnvName))
        {
            return Result.Success();
        }

        return await LogErrorAndSendMessageFromError(InstallerErrors.ServiceIsNotExists(serviceEnvName),
            cancellationToken);
    }

    //PowerShell-ის ბრძანების გაშვება და შეცდომების გაანალიზება.
    //ტერმინირებად (exception) და არატერმინირებად (Error ნაკადი) შეცდომებს გამოვიტანთ მომხმარებლისთვის და ვაბრუნებთ Error-ებად.
    private async ValueTask<Result> InvokePowerShellAndCheckErrors(PowerShell ps, string methodName,
        string streamErrorCode, CancellationToken cancellationToken = default)
    {
        try
        {
            await ps.InvokeAsync();
        }
        catch (Exception ex)
        {
            //PowerShell-ის ტერმინირებადი შეცდომა (მაგალითად, ადმინისტრატორის უფლებების უქონლობა) — გამოვიტანოთ მისი ტექსტი მომხმარებლისთვის
            return await LogErrorAndSendMessageFromException(ex, methodName, cancellationToken);
        }

        //PowerShell-ის არატერმინირებადი შეცდომები გროვდება Error ნაკადში — წავიკითხოთ და გამოვიტანოთ მომხმარებლისთვის
        if (ps.Streams.Error.Count <= 0)
        {
            return Result.Success();
        }

        var errors = new List<Error>();
        foreach (ErrorRecord errorRecord in ps.Streams.Error)
        {
            errors.Add(await LogErrorAndSendMessageFromError(streamErrorCode, errorRecord.ToString(),
                cancellationToken));
        }

        return errors.Count == 1 ? errors[0] : Result.CreateValidationError([.. errors]);
    }
}
