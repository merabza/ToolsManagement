using System.Threading;
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


    public static LocalInstallerSettingsDomain? Create(ILogger? logger, bool useConsole, InstallerSettings? lis,
        IMessagesDataManager? messagesDataManager, string? userName)
    {
        if (lis is not null)
            return Create(logger, useConsole, lis.InstallerWorkFolder, lis.FilesUserName, lis.FilesUsersGroupName,
                lis.ServiceUserName, lis.DownloadTempExtension, lis.InstallFolder, lis.DotnetRunner,
                messagesDataManager, userName);

        messagesDataManager?.SendMessage(userName, "LocalInstallerSettings does not configured for support tools",
                CancellationToken.None)
            .Wait();
        StShared.WriteErrorLine("LocalInstallerSettings does not configured for support tools", true);
        return null;
    }


    public static LocalInstallerSettingsDomain? Create(ILogger? logger, bool useConsole, string? installerWorkFolder,
        string? filesUserName, string? filesUsersGroupName, string? serviceUserName, string? downloadTempExtension,
        string? installFolder, string? dotnetRunner, IMessagesDataManager? messagesDataManager, string? userName)
    {
        messagesDataManager?.SendMessage(userName, "Creating LocalInstallerSettingsDomain", CancellationToken.None)
            .Wait();

        if (string.IsNullOrWhiteSpace(installerWorkFolder))
        {
            messagesDataManager?.SendMessage(userName, "installerWorkFolder does not specified", CancellationToken.None)
                .Wait();
            StShared.WriteErrorLine("installerWorkFolder does not specified", useConsole, logger);
            return null;
        }

        if (string.IsNullOrWhiteSpace(filesUserName))
        {
            messagesDataManager?.SendMessage(userName, "filesUserName does not specified", CancellationToken.None)
                .Wait();
            StShared.WriteErrorLine("filesUserName does not specified", useConsole, logger);
            return null;
        }

        if (string.IsNullOrWhiteSpace(filesUsersGroupName))
        {
            messagesDataManager?.SendMessage(userName, "filesUsersGroupName does not specified", CancellationToken.None)
                .Wait();
            StShared.WriteErrorLine("filesUsersGroupName does not specified", useConsole, logger);
            return null;
        }

        if (string.IsNullOrWhiteSpace(serviceUserName))
        {
            messagesDataManager?.SendMessage(userName, "serviceUserName does not specified", CancellationToken.None)
                .Wait();
            StShared.WriteErrorLine("serviceUserName does not specified", useConsole, logger);
            return null;
        }

        if (string.IsNullOrWhiteSpace(downloadTempExtension))
        {
            messagesDataManager
                ?.SendMessage(userName, "downloadTempExtension does not specified", CancellationToken.None).Wait();
            StShared.WriteErrorLine("downloadTempExtension does not specified", useConsole, logger);
            return null;
        }

        if (!string.IsNullOrWhiteSpace(installFolder))
            return new LocalInstallerSettingsDomain(installerWorkFolder, filesUserName, filesUsersGroupName,
                serviceUserName, downloadTempExtension, installFolder, dotnetRunner);

        messagesDataManager?.SendMessage(userName, "installFolder does not specified", CancellationToken.None).Wait();
        StShared.WriteErrorLine("installFolder does not specified", useConsole, logger);
        return null;
    }
}