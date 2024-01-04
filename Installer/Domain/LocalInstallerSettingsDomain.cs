using System.Threading;
using System.Threading.Tasks;
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
        //ServiceDescriptionSignature = serviceDescriptionSignature;
        //ProjectDescription = projectDescription;
    }

    public string InstallerWorkFolder { get; }
    public string FilesUserName { get; }
    public string FilesUsersGroupName { get; }
    public string ServiceUserName { get; }
    public string DownloadTempExtension { get; }
    public string InstallFolder { get; }

    public string? DotnetRunner { get; }
    //public string? ServiceDescriptionSignature { get; }
    //public string? ProjectDescription { get; }


    public static async Task<LocalInstallerSettingsDomain?> Create(ILogger? logger, bool useConsole,
        InstallerSettings? lis,
        IMessagesDataManager? messagesDataManager, string? userName, CancellationToken cancellationToken)
    {
        if (lis is not null)
            return await Create(logger, useConsole, lis.InstallerWorkFolder, lis.FilesUserName, lis.FilesUsersGroupName,
                lis.ServiceUserName, lis.DownloadTempExtension, lis.InstallFolder, lis.DotnetRunner,
                messagesDataManager, userName, cancellationToken);

        if (messagesDataManager is not null)
            await messagesDataManager.SendMessage(userName,
                "LocalInstallerSettings does not configured for support tools", cancellationToken);
        StShared.WriteErrorLine("LocalInstallerSettings does not configured for support tools", true);
        return null;
    }


    public static async Task<LocalInstallerSettingsDomain?> Create(ILogger? logger, bool useConsole,
        string? installerWorkFolder, string? filesUserName, string? filesUsersGroupName, string? serviceUserName,
        string? downloadTempExtension, string? installFolder, string? dotnetRunner,
        IMessagesDataManager? messagesDataManager, string? userName, CancellationToken cancellationToken)
    {
        if (messagesDataManager is not null)
            await messagesDataManager.SendMessage(userName, "Creating LocalInstallerSettingsDomain", cancellationToken);

        if (string.IsNullOrWhiteSpace(installerWorkFolder))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName, "installerWorkFolder does not specified",
                    cancellationToken);
            StShared.WriteErrorLine("installerWorkFolder does not specified", useConsole, logger);
            return null;
        }

        if (string.IsNullOrWhiteSpace(filesUserName))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName, "filesUserName does not specified", cancellationToken);
            StShared.WriteErrorLine("filesUserName does not specified", useConsole, logger);
            return null;
        }

        if (string.IsNullOrWhiteSpace(filesUsersGroupName))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName, "filesUsersGroupName does not specified",
                    cancellationToken);
            StShared.WriteErrorLine("filesUsersGroupName does not specified", useConsole, logger);
            return null;
        }

        if (string.IsNullOrWhiteSpace(serviceUserName))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName, "serviceUserName does not specified",
                    cancellationToken);
            StShared.WriteErrorLine("serviceUserName does not specified", useConsole, logger);
            return null;
        }

        if (string.IsNullOrWhiteSpace(downloadTempExtension))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName, "downloadTempExtension does not specified",
                    cancellationToken);
            StShared.WriteErrorLine("downloadTempExtension does not specified", useConsole, logger);
            return null;
        }

        if (!string.IsNullOrWhiteSpace(installFolder))
            return new LocalInstallerSettingsDomain(installerWorkFolder, filesUserName, filesUsersGroupName,
                serviceUserName, downloadTempExtension, installFolder, dotnetRunner);

        if (messagesDataManager is not null)
            await messagesDataManager.SendMessage(userName, "installFolder does not specified", cancellationToken);
        StShared.WriteErrorLine("installFolder does not specified", useConsole, logger);
        return null;
    }
}