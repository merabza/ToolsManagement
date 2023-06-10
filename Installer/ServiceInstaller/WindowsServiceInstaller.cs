using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ServiceProcess;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace Installer.ServiceInstaller;

public sealed class WindowsServiceInstaller : InstallerBase
{
    public WindowsServiceInstaller(bool useConsole, ILogger logger) : base(useConsole, logger, "win10-x64")
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

        Logger.LogInformation($"Stopping the {serviceName} service...");
        sc.Stop();
        sc.WaitForStatus(ServiceControllerStatus.Stopped);

        // Refresh and display the current service status.
        sc.Refresh();

        Logger.LogInformation($"The {serviceName} service status is now set to {sc.Status}.");
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

        Logger.LogInformation($"Starting the {serviceName} service...");
        sc.Start();
        sc.WaitForStatus(ServiceControllerStatus.Running);
        // Refresh and display the current service status.
        sc.Refresh();

        Logger.LogInformation($"The {serviceName} service status is now set to {sc.Status}.");
#pragma warning restore CA1416 // Validate platform compatibility

        return true;
    }

    protected override bool ChangeOneFileOwner(string filePath, string? filesUserName, string? filesUsersGroupName)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
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

        Logger.LogError($"Error changing owner to file {filePath}");
        return false;
    }

    protected override bool ChangeOwner(string folderPath, string filesUserName, string filesUsersGroupName)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
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

        Logger.LogError($"Error changing owner to folder {folderPath}");
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

    //private PSCredential CreateCredential()
    //{
    //  //var v = new Impersonator("", "", "");


    //  SecureString password = new SecureString();
    //  Array.ForEach("password".ToCharArray(), password.AppendChar);
    //  return new PSCredential("username", password);
    //}

    //public static Collection<PSObject> GetPsResults(PowerShell ps, PSCredential credential, bool throwErrors = true)
    //{
    //  WSManConnectionInfo connectionInfo = new WSManConnectionInfo() { Credential = credential };

    //  using Runspace runSpace = RunspaceFactory.CreateRunspace(connectionInfo);
    //  runSpace.Open();
    //  ps.Runspace = runSpace;
    //  Collection<PSObject> toReturn = ps.Invoke();
    //  if (throwErrors)
    //  {
    //    if (ps.HadErrors)
    //    {
    //      throw ps.Streams.Error.ElementAt(0).Exception;
    //    }
    //  }
    //  runSpace.Close();

    //  return toReturn;
    //}


    //public void RunWithParameters()
    //{
    //  // create empty pipeline
    //  PowerShell ps = PowerShell.Create();

    //  // add command
    //  ps.AddCommand("test-path").AddParameter("Path", Environment.CurrentDirectory); ;

    //  var obj = ps.Invoke();
    //}

    //private string RunScript(string scriptText)
    //{
    //  // create Powershell runspace

    //  Runspace runspace = RunspaceFactory.CreateRunspace();

    //  // open it

    //  runspace.Open();

    //  // create a pipeline and feed it the script text

    //  Pipeline pipeline = runspace.CreatePipeline();
    //  pipeline.Commands.AddScript(scriptText);

    //  // add an extra command to transform the script
    //  // output objects into nicely formatted strings

    //  // remove this line to get the actual objects
    //  // that the script returns. For example, the script

    //  // "Get-Process" returns a collection
    //  // of System.Diagnostics.Process instances.

    //  pipeline.Commands.Add("Out-String");

    //  // execute the script

    //  Collection<PSObject> results = pipeline.Invoke();

    //  // close the runspace

    //  runspace.Close();

    //  // convert the script result into a single string

    //  StringBuilder stringBuilder = new StringBuilder();
    //  foreach (PSObject obj in results)
    //  {
    //    stringBuilder.AppendLine(obj.ToString());
    //  }

    //  return stringBuilder.ToString();
    //}


    //public static int RunPowershellScript(string ps)
    //{
    //  int errorLevel;
    //  ProcessStartInfo processInfo;
    //  Process process;

    //  processInfo = new ProcessStartInfo("powershell.exe", "-File " + ps);
    //  processInfo.CreateNoWindow = true;
    //  processInfo.UseShellExecute = false;

    //  process = Process.Start(processInfo);
    //  process.WaitForExit();

    //  errorLevel = process.ExitCode;
    //  process.Close();

    //  return errorLevel;
    //}


    //{
    //  System.Diagnostics.Process proc = new System.Diagnostics.Process();
    //  System.Security.SecureString ssPwd = new System.Security.SecureString();
    //  proc.StartInfo.UseShellExecute = false;
    //  proc.StartInfo.FileName = "filename";
    //  proc.StartInfo.Arguments = "args...";
    //  proc.StartInfo.Domain = "domainname";
    //  proc.StartInfo.UserName = "username";
    //  string password = "user entered password";
    //  for (int x = 0; x<password.Length; x++)
    //  {
    //    ssPwd.AppendChar(password[x]);
    //  }
    //  password = "";
    //  proc.StartInfo.Password = ssPwd;
    //  proc.Start();
    //}
}