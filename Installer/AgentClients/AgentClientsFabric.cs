using Installer.Domain;
using Installer.Models;
using LibFileParameters.Models;
using Microsoft.Extensions.Logging;
using SystemToolsShared;
using WebAgentMessagesContracts;

namespace Installer.AgentClients;

public static class AgentClientsFabric
{
    public static IAgentClientWithFileStorage? CreateAgentClientWithFileStorage(ILogger logger,
        InstallerSettings webAgentInstallerSettings, FileStorageData fileStorageForUpload, bool useConsole,
        IMessagesDataManager? messagesDataManager, string? userName)
    {
        messagesDataManager?.SendMessage(userName, "Creating local Agent With File Storage").Wait();

        var localInstallerSettingsDomain =
            LocalInstallerSettingsDomain.Create(logger, useConsole, webAgentInstallerSettings, messagesDataManager,
                userName);

        if (localInstallerSettingsDomain is not null)
            return new LocalAgentWithFileStorage(logger, useConsole, fileStorageForUpload, localInstallerSettingsDomain,
                messagesDataManager, userName);

        messagesDataManager?.SendMessage(userName, "localInstallerSettingsDomain does not created").Wait();
        logger.LogError("localInstallerSettingsDomain does not created");
        return null;
    }


    public static IAgentClient? CreateAgentClient(ILogger logger, bool useConsole, string? installFolder,
        IMessagesDataManager? messagesDataManager, string? userName)
    {
        messagesDataManager?.SendMessage(userName, "Creating local Agent").Wait();

        if (!string.IsNullOrWhiteSpace(installFolder))
            return new LocalAgent(logger, useConsole, installFolder, messagesDataManager, userName);

        messagesDataManager?.SendMessage(userName, "installFolder name in parameters is empty").Wait();
        logger.LogError("installFolder name in parameters is empty");
        return null;
    }
}