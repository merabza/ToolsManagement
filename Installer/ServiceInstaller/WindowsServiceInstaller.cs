using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ServiceProcess;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using WebAgentMessagesContracts;

namespace Installer.ServiceInstaller;

public sealed class WindowsServiceInstaller : InstallerBase
{
    public WindowsServiceInstaller(bool useConsole, ILogger logger, IMessagesDataManager? messagesDataManager,
        string? userName) : base(useConsole, logger, "win10-x64", messagesDataManager, userName)
    {
    }

    protected override bool IsServiceExists(string serviceName)
    {
#pragma warning disable CA1416 // Validate platform compatibility
        var sc = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == serviceName);
        return sc != null;
#pragma warning restore CA1416 // Validate platform compatibility
    }

    protected override bool IsServiceRunning(string serviceName)
    {
#pragma warning disable CA1416 // Validate platform compatibility
        var sc = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == serviceName);
        if (sc == null)
            return false;
        return !(sc.Status.Equals(ServiceControllerStatus.Stopped) ||
                 sc.Status.Equals(ServiceControllerStatus.StopPending));
#pragma warning restore CA1416 // Validate platform compatibility
    }

    protected override bool RemoveService(string serviceName)
    {
#pragma warning disable CA1416 // Validate platform compatibility
        var sc = new ServiceController(serviceName);
        sc.Refresh();
        if (!(sc.Status.Equals(ServiceControllerStatus.Stopped) ||
              sc.Status.Equals(ServiceControllerStatus.StopPending)))
            return false;
#pragma warning restore CA1416 // Validate platform compatibility

        // create empty pipeline
        using var ps = PowerShell.Create();

        // add command
        ps.AddCommand("Remove-Service").AddParameter("Name", serviceName);

        //Collection<PSObject> results = ps.Invoke();
        ps.Invoke();

        return true;
    }

    protected override bool StopService(string serviceName)
    {
#pragma warning disable CA1416 // Validate platform compatibility

        var sc = new ServiceController(serviceName);

        if (sc.Status.Equals(ServiceControllerStatus.Stopped) ||
            sc.Status.Equals(ServiceControllerStatus.StopPending))
            return true;

        MessagesDataManager?.SendMessage(UserName, $"Stopping the {serviceName} service...").Wait();
        Logger.LogInformation("Stopping the {serviceName} service...", serviceName);
        sc.Stop();
        sc.WaitForStatus(ServiceControllerStatus.Stopped);

        // Refresh and display the current service status.
        sc.Refresh();

        var status = sc.Status;

        MessagesDataManager?.SendMessage(UserName, $"The {serviceName} service status is now set to {status}.").Wait();
        Logger.LogInformation("The {serviceName} service status is now set to {status}.", serviceName, status);
#pragma warning restore CA1416 // Validate platform compatibility

        return true;
    }

    protected override bool StartService(string serviceName)
    {
#pragma warning disable CA1416 // Validate platform compatibility

        var sc = new ServiceController(serviceName);
        sc.Refresh();
        if (!(sc.Status.Equals(ServiceControllerStatus.Stopped) ||
              sc.Status.Equals(ServiceControllerStatus.StopPending)))
            return true;

        MessagesDataManager?.SendMessage(UserName, $"Starting the {serviceName} service...").Wait();
        Logger.LogInformation("Starting the {serviceName} service...", serviceName);
        sc.Start();
        sc.WaitForStatus(ServiceControllerStatus.Running);
        // Refresh and display the current service status.
        sc.Refresh();

        var status = sc.Status;
        MessagesDataManager?.SendMessage(UserName, $"The {serviceName} service status is now set to {status}.").Wait();
        Logger.LogInformation("The {serviceName} service status is now set to {status}.", serviceName, status);
#pragma warning restore CA1416 // Validate platform compatibility

        return true;
    }

    protected override bool ChangeOneFileOwner(string filePath, string? filesUserName, string? filesUsersGroupName)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            MessagesDataManager?.SendMessage(UserName, "Folder name is empty").Wait();
            Logger.LogError("Folder name is empty");
            return false;
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
            return true;
        }

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
            return true;
        }

        MessagesDataManager?.SendMessage(UserName, $"Error changing owner to folder {folderPath}").Wait();
        Logger.LogError("Error changing owner to folder {folderPath}", folderPath);
        return false;
    }


//    private static string GetServiceInstallPath(string serviceName)
//    {
//#pragma warning disable CA1416 // Validate platform compatibility
//      RegistryKey regKey = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\services\{serviceName}");
//      return regKey?.GetValue("ImagePath")?.ToString();
//#pragma warning restore CA1416 // Validate platform compatibility
//    }

    protected override bool IsServiceRegisteredProperly(string projectName, string serviceName, string userName,
        string installFolderPath)
    {
        var exeFilePath = Path.Combine(installFolderPath, $"{projectName}.exe");
        //თუ სერვისი უკვე დარეგისტრირებულია და გვინდა დავადგინოთ გამშვები ფაილი რომელია, გვაქვს 2 გზა
        //1. გამოვიყენოთ sc qc <service name> და გავარჩიოთ რას დააბრუნებს
        //2. HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services.
        //   Find the service you want to redirect,
        //   locate the ImagePath subkey value.
#pragma warning disable CA1416 // Validate platform compatibility
        var regKey = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\services\{serviceName}");
        var imagePath = regKey?.GetValue("ImagePath")?.ToString();
#pragma warning restore CA1416 // Validate platform compatibility

        return imagePath != null && imagePath == exeFilePath;
    }

    protected override bool RegisterService(string projectName, string serviceName, string serviceUserName,
        string installFolderPath)
    {
        // create empty pipeline
        using var ps = PowerShell.Create();

        // add command
        var exeFilePath = Path.Combine(installFolderPath, $"{projectName}.exe");
        ps.AddCommand("New-Service")
            .AddParameter("Name", serviceName)
            .AddParameter("BinaryPathName", exeFilePath)
            //.AddParameter("Credential", "NT AUTHORITY\\LOCAL SERVICE")
            .AddParameter("StartupType", "Automatic");

        //Collection<PSObject> results = ps.Invoke();
        ps.Invoke();

        //StringBuilder sb = new StringBuilder();
        //foreach (PSObject obj in results)
        //{
        //  sb.AppendLine(obj.ToString());
        //}

        //Logger.LogInformation(sb.ToString());


        //Collection<PSObject> obj = GetPsResults(ps, CreateCredential());
        return IsServiceExists(serviceName);
    }
}