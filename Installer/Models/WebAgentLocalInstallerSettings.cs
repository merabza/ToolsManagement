using LibParameters;
using Microsoft.Extensions.Configuration;

namespace Installer.Models;

public sealed class InstallerSettings : IParameters
{
    public const string DefaultDownloadFileTempExtension = ".down!";

    public string? InstallerWorkFolder { get; set; }
    public string? InstallFolder { get; set; }
    public string? DotnetRunner { get; set; }
    public string? ProgramArchiveDateMask { get; set; }
    public string? ProgramArchiveExtension { get; set; }
    public string? ParametersFileDateMask { get; set; }
    public string? ParametersFileExtension { get; set; }

    //ეს არის იმ ადგილის სახელი საიდანაც უნდა მოხდეს ჩამოტვირთვა
    public string? ProgramExchangeFileStorageName { get; set; }

    public string? ServiceUserName { get; set; }
    public string? DownloadTempExtension { get; set; }
    public string? FilesUserName { get; set; }
    public string? FilesUsersGroupName { get; set; }

    public bool CheckBeforeSave()
    {
        return true;
    }

    public string GetDownloadTempExtension()
    {
        return DownloadTempExtension ?? DefaultDownloadFileTempExtension;
    }

    public static InstallerSettings Create(IConfiguration configuration)
    {
        var installerSettings = configuration.GetSection("InstallerSettings");
        return installerSettings.Get<InstallerSettings>() ?? new InstallerSettings();
    }
}