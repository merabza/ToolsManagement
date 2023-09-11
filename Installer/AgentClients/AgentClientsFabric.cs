using Installer.Domain;
using Installer.Models;
using LibFileParameters.Models;
using Microsoft.Extensions.Logging;
using System.Threading;
using SystemToolsShared;

namespace Installer.AgentClients;

public static class AgentClientsFabric
{
    public static IIProjectsApiClientWithFileStorage? CreateAgentClientWithFileStorage(ILogger logger,
        InstallerSettings webAgentInstallerSettings, FileStorageData fileStorageForUpload, bool useConsole,
        IMessagesDataManager? messagesDataManager, string? userName)
    {
        messagesDataManager?.SendMessage(userName, "Creating local Agent With File Storage", CancellationToken.None)
            .Wait();

        var localInstallerSettingsDomain =
            LocalInstallerSettingsDomain.Create(logger, useConsole, webAgentInstallerSettings, messagesDataManager,
                userName);

        if (localInstallerSettingsDomain is not null)
            return new ProjectsLocalAgentWithFileStorage(logger, useConsole, fileStorageForUpload,
                localInstallerSettingsDomain,
                messagesDataManager, userName);

        messagesDataManager
            ?.SendMessage(userName, "localInstallerSettingsDomain does not created", CancellationToken.None).Wait();
        logger.LogError("localInstallerSettingsDomain does not created");
        return null;
    }


    public static IProjectsApiClient? CreateAgentClient(ILogger logger, bool useConsole, string? installFolder,
        IMessagesDataManager? messagesDataManager, string? userName)
    {
        messagesDataManager?.SendMessage(userName, "Creating local Agent", CancellationToken.None).Wait();

        if (!string.IsNullOrWhiteSpace(installFolder))
            return new ProjectsLocalAgent(logger, useConsole, installFolder, messagesDataManager, userName);

        messagesDataManager?.SendMessage(userName, "installFolder name in parameters is empty", CancellationToken.None)
            .Wait();
        logger.LogError("installFolder name in parameters is empty");
        return null;
    }
}