﻿using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using OneOf;
using SystemToolsShared;

namespace Installer.ServiceInstaller;

public sealed class WindowsServiceInstaller : InstallerBase
{
    private readonly string? _projectDescription;
    private readonly string? _serviceDescriptionSignature;

    public WindowsServiceInstaller(bool useConsole, ILogger logger, string? serviceDescriptionSignature,
        string? projectDescription, IMessagesDataManager? messagesDataManager, string? userName) : base(useConsole,
        logger, "win10-x64", messagesDataManager, userName)
    {
        _serviceDescriptionSignature = serviceDescriptionSignature;
        _projectDescription = projectDescription;
    }

    public WindowsServiceInstaller(bool useConsole, ILogger logger,
        IMessagesDataManager? messagesDataManager, string? userName) : base(useConsole, logger, "win10-x64",
        messagesDataManager, userName)
    {
        _serviceDescriptionSignature = null;
        _projectDescription = null;
    }

    protected override bool IsServiceExists(string serviceEnvName)
    {
#pragma warning disable CA1416 // Validate platform compatibility
        var sc = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == serviceEnvName);
        return sc != null;
#pragma warning restore CA1416 // Validate platform compatibility
    }

    protected override bool IsServiceRunning(string serviceEnvName)
    {
#pragma warning disable CA1416 // Validate platform compatibility
        var sc = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == serviceEnvName);
        if (sc == null)
            return false;
        return !(sc.Status.Equals(ServiceControllerStatus.Stopped) ||
                 sc.Status.Equals(ServiceControllerStatus.StopPending));
#pragma warning restore CA1416 // Validate platform compatibility
    }

    protected override Option<Err[]> RemoveService(string serviceEnvName)
    {
#pragma warning disable CA1416 // Validate platform compatibility
        var sc = new ServiceController(serviceEnvName);
        sc.Refresh();
        if (!(sc.Status.Equals(ServiceControllerStatus.Stopped) ||
              sc.Status.Equals(ServiceControllerStatus.StopPending)))
            return new Err[]
            {
                new()
                {
                    ErrorCode = "ServiceIsRunningAndCanNotBeRemoved",
                    ErrorMessage = $"Service {serviceEnvName} is running and can not be removed"
                }
            };
#pragma warning restore CA1416 // Validate platform compatibility

        // create empty pipeline
        using var ps = PowerShell.Create();

        // add command
        ps.AddCommand("Remove-Service").AddParameter("Name", serviceEnvName);

        //Collection<PSObject> results = ps.Invoke();
        ps.Invoke();

        return null;
    }

    protected override async Task<Option<Err[]>> StopService(string serviceEnvName, CancellationToken cancellationToken)
    {
#pragma warning disable CA1416 // Validate platform compatibility

        var sc = new ServiceController(serviceEnvName);

        if (sc.Status.Equals(ServiceControllerStatus.Stopped) ||
            sc.Status.Equals(ServiceControllerStatus.StopPending))
            return null;

        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName, $"Stopping the {serviceEnvName} service...",
                cancellationToken);
        Logger.LogInformation("Stopping the {serviceName} service...", serviceEnvName);
        sc.Stop();
        sc.WaitForStatus(ServiceControllerStatus.Stopped);

        // Refresh and display the current service status.
        sc.Refresh();

        var status = sc.Status;

        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName,
                $"The {serviceEnvName} service status is now set to {status}.", cancellationToken);
        Logger.LogInformation("The {serviceEnvName} service status is now set to {status}.", serviceEnvName, status);
#pragma warning restore CA1416 // Validate platform compatibility

        return new Err[]
        {
            new()
            {
                ErrorCode = "ServiceStatusNotChanged",
                ErrorMessage = $"The {serviceEnvName} service status is now set to {status}."
            }
        };
    }

    protected override async Task<Option<Err[]>> StartService(string serviceEnvName,
        CancellationToken cancellationToken)
    {
#pragma warning disable CA1416 // Validate platform compatibility

        var sc = new ServiceController(serviceEnvName);
        sc.Refresh();
        if (!(sc.Status.Equals(ServiceControllerStatus.Stopped) ||
              sc.Status.Equals(ServiceControllerStatus.StopPending)))
            return null;

        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName, $"Starting the {serviceEnvName} service...",
                cancellationToken);
        Logger.LogInformation("Starting the {serviceName} service...", serviceEnvName);
        sc.Start();
        sc.WaitForStatus(ServiceControllerStatus.Running);
        // Refresh and display the current service status.
        sc.Refresh();

        var status = sc.Status;
        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName,
                $"The {serviceEnvName} service status is now set to {status}.", cancellationToken);
        Logger.LogInformation("The {serviceName} service status is now set to {status}.", serviceEnvName, status);
#pragma warning restore CA1416 // Validate platform compatibility

        return null;
    }

    protected override async Task<Option<Err[]>> ChangeOneFileOwner(string filePath, string? filesUserName,
        string? filesUsersGroupName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filePath))
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

        var userName = "NT AUTHORITY\\LOCAL SERVICE";
        if (!string.IsNullOrWhiteSpace(filesUserName)) userName = filesUserName;

        if (File.Exists(filePath))
        {
            var file = new FileInfo(filePath);
#pragma warning disable CA1416 // Validate platform compatibility

            var dac = file.GetAccessControl();
            //IdentityReference ir = new SecurityIdentifier("S-1-5-18");
            //IdentityReference ir = new NTAccount("Local System Account");
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

        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName, $"Error changing owner to file {filePath}",
                cancellationToken);
        Logger.LogError("Error changing owner to file {filePath}", filePath);
        return new Err[]
        {
            new()
            {
                ErrorCode = "ErrorChangingOwner",
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

        var userName = "NT AUTHORITY\\LOCAL SERVICE";
        if (!string.IsNullOrWhiteSpace(filesUserName)) userName = filesUserName;

        if (Directory.Exists(folderPath))
        {
            var installFolder = new DirectoryInfo(folderPath);
#pragma warning disable CA1416 // Validate platform compatibility

            var dac = installFolder.GetAccessControl();
            //IdentityReference ir = new SecurityIdentifier("S-1-5-18");
            //IdentityReference ir = new NTAccount("Local System Account");
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


//    private static string GetServiceInstallPath(string serviceName)
//    {
//#pragma warning disable CA1416 // Validate platform compatibility
//      RegistryKey regKey = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\services\{serviceName}");
//      return regKey?.GetValue("ImagePath")?.ToString();
//#pragma warning restore CA1416 // Validate platform compatibility
//    }

    protected override async Task<OneOf<bool, Err[]>> IsServiceRegisteredProperly(string projectName,
        string serviceEnvName, string userName, string installFolderPath, string? serviceDescriptionSignature,
        string? projectDescription, CancellationToken cancellationToken)
    {
        var exeFilePath = Path.Combine(installFolderPath, $"{projectName}.exe");
        var mustBeDescription =
            $"{serviceEnvName} service {_serviceDescriptionSignature ?? ""} {_projectDescription ?? ""}";
        //თუ სერვისი უკვე დარეგისტრირებულია და გვინდა დავადგინოთ გამშვები ფაილი რომელია, გვაქვს 2 გზა
        //1. გამოვიყენოთ sc qc <service name> და გავარჩიოთ რას დააბრუნებს
        //2. HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services.
        //   Find the service you want to redirect,
        //   locate the ImagePath subkey value.
#pragma warning disable CA1416 // Validate platform compatibility
        var regKey = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\services\{serviceEnvName}");
        var imagePath = regKey?.GetValue("ImagePath")?.ToString();
        var description = regKey?.GetValue("Description")?.ToString();
#pragma warning restore CA1416 // Validate platform compatibility

        var toReturn = imagePath is not null && imagePath == exeFilePath && description is not null &&
                       description == mustBeDescription;
        return await Task.FromResult(toReturn);
    }

    protected override async Task<Option<Err[]>> RegisterService(string projectName, string serviceEnvName,
        string serviceUserName, string installFolderPath, string? serviceDescriptionSignature,
        string? projectDescription, CancellationToken cancellationToken)
    {
        // create empty pipeline
        using var ps = PowerShell.Create();

        // add command
        var exeFilePath = Path.Combine(installFolderPath, $"{projectName}.exe");
        ps.AddCommand("New-Service")
            .AddParameter("Name", serviceEnvName)
            .AddParameter("Description",
                $"{serviceEnvName} service {_serviceDescriptionSignature ?? ""} {_projectDescription ?? ""}")
            .AddParameter("BinaryPathName", exeFilePath)
            //.AddParameter("Credential", "NT AUTHORITY\\LOCAL SERVICE")
            .AddParameter("StartupType", "Automatic");

        //Collection<PSObject> results = ps.Invoke();
        await ps.InvokeAsync();

        //StringBuilder sb = new StringBuilder();
        //foreach (PSObject obj in results)
        //{
        //  sb.AppendLine(obj.ToString());
        //}

        //Logger.LogInformation(sb.ToString());


        //Collection<PSObject> obj = GetPsResults(ps, CreateCredential());
        if (IsServiceExists(serviceEnvName))
            return null;

        return new Err[]
        {
            new()
            {
                ErrorCode = "ServiceDoesNotExists",
                ErrorMessage = $"Service {serviceEnvName} does not exists"
            }
        };
    }
}