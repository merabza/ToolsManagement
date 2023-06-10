using Installer.Domain;
using Installer.Models;
using LibFileParameters.Models;
using Microsoft.Extensions.Logging;

namespace Installer.AgentClients;

public static class AgentClientsFabric
{
    public static IAgentClientWithFileStorage? CreateAgentClientWithFileStorage(ILogger logger,
        InstallerSettings webAgentInstallerSettings, FileStorageData fileStorageForUpload,
        bool useConsole)
    {
        var localInstallerSettingsDomain =
            LocalInstallerSettingsDomain.Create(logger, useConsole, webAgentInstallerSettings);

        if (localInstallerSettingsDomain is not null)
            return new LocalAgentWithFileStorage(logger, useConsole, fileStorageForUpload,
                localInstallerSettingsDomain);

        logger.LogError("localInstallerSettingsDomain does not created");
        return null;
    }


    public static IAgentClient? CreateAgentClient(ILogger logger, bool useConsole, string? installFolder)
    {
        if (string.IsNullOrWhiteSpace(installFolder))
        {
            logger.LogError("downloadTempExtension is empty");
            return null;
        }


        return new LocalAgent(logger, useConsole, installFolder);
    }
}