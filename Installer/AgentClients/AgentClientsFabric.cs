using Installer.Domain;
using Installer.Models;
using LibFileParameters.Models;
using Microsoft.Extensions.Logging;
using WebAgentMessagesContracts;

namespace Installer.AgentClients;

public static class AgentClientsFabric
{
    public static IAgentClientWithFileStorage? CreateAgentClientWithFileStorage(ILogger logger,
        InstallerSettings webAgentInstallerSettings, FileStorageData fileStorageForUpload, bool useConsole,
        IMessagesDataManager messagesDataManager, string? userName)
    {
        var localInstallerSettingsDomain =
            LocalInstallerSettingsDomain.Create(logger, useConsole, webAgentInstallerSettings);

        if (localInstallerSettingsDomain is not null)
            return new LocalAgentWithFileStorage(logger, useConsole, fileStorageForUpload, localInstallerSettingsDomain,
                messagesDataManager, userName);

        logger.LogError("localInstallerSettingsDomain does not created");
        return null;
    }


    public static IAgentClient? CreateAgentClient(ILogger logger, bool useConsole, string? installFolder,
        IMessagesDataManager messagesDataManager, string? userName)
    {
        if (!string.IsNullOrWhiteSpace(installFolder))
            return new LocalAgent(logger, useConsole, installFolder, messagesDataManager, userName);

        logger.LogError("installFolder name in parameters is empty");
        return null;
    }
}