using Installer.Models;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace Installer.Domain;

public sealed class LocalInstallerSettingsDomain
{
    private LocalInstallerSettingsDomain(string installerWorkFolder, string filesUserName, string filesUsersGroupName,
        string serviceUserName, string downloadTempExtension, string installFolder, string? dotnetRunner)
    {
        InstallerWorkFolder = installerWorkFolder;
        FilesUserName = filesUserName;
        FilesUsersGroupName = filesUsersGroupName;
        ServiceUserName = serviceUserName;
        DownloadTempExtension = downloadTempExtension;
        InstallFolder = installFolder;
        DotnetRunner = dotnetRunner;
    }

    public string InstallerWorkFolder { get; }
    public string FilesUserName { get; }
    public string FilesUsersGroupName { get; }
    public string ServiceUserName { get; }
    public string DownloadTempExtension { get; }
    public string InstallFolder { get; }
    public string? DotnetRunner { get; }


    public static LocalInstallerSettingsDomain? Create(ILogger? logger, bool useConsole, InstallerSettings? lis)
    {
        if (lis is not null)
            return Create(logger, useConsole, lis.InstallerWorkFolder, lis.FilesUserName, lis.FilesUsersGroupName,
                lis.ServiceUserName, lis.DownloadTempExtension, lis.InstallFolder, lis.DotnetRunner);

        StShared.WriteErrorLine("LocalInstallerSettings does not configured for support tools", true);
        return null;
    }


    public static LocalInstallerSettingsDomain? Create(ILogger? logger, bool useConsole, string? installerWorkFolder,
        string? filesUserName, string? filesUsersGroupName, string? serviceUserName, string? downloadTempExtension,
        string? installFolder, string? dotnetRunner)
    {
        if (string.IsNullOrWhiteSpace(installerWorkFolder))
        {
            StShared.WriteErrorLine("installerWorkFolder does not specified", useConsole, logger);
            return null;
        }

        if (string.IsNullOrWhiteSpace(filesUserName))
        {
            StShared.WriteErrorLine("filesUserName does not specified", useConsole, logger);
            return null;
        }

        if (string.IsNullOrWhiteSpace(filesUsersGroupName))
        {
            StShared.WriteErrorLine("filesUsersGroupName does not specified", useConsole, logger);
            return null;
        }

        if (string.IsNullOrWhiteSpace(serviceUserName))
        {
            StShared.WriteErrorLine("serviceUserName does not specified", useConsole, logger);
            return null;
        }

        if (string.IsNullOrWhiteSpace(downloadTempExtension))
        {
            StShared.WriteErrorLine("downloadTempExtension does not specified", useConsole, logger);
            return null;
        }

        if (string.IsNullOrWhiteSpace(installFolder))
        {
            StShared.WriteErrorLine("installFolder does not specified", useConsole, logger);
            return null;
        }

        return new LocalInstallerSettingsDomain(installerWorkFolder, filesUserName, filesUsersGroupName,
            serviceUserName, downloadTempExtension, installFolder, dotnetRunner);
    }
}