﻿using System.Threading;
using System.Threading.Tasks;
using Installer.Domain;
using Installer.Models;
using LibFileParameters.Models;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace Installer.ProjectManagers;

public static class ProjectManagersFactory
{
    public static async ValueTask<IIProjectsManagerWithFileStorage?> CreateAgentClientWithFileStorage(ILogger logger,
        InstallerSettings webAgentInstallerSettings, FileStorageData fileStorageForUpload, bool useConsole,
        IMessagesDataManager? messagesDataManager, string? userName, CancellationToken cancellationToken = default)
    {
        if (messagesDataManager is not null)
            await messagesDataManager.SendMessage(userName, "Creating local Agent With File Storage",
                cancellationToken);

        var localInstallerSettingsDomain = await LocalInstallerSettingsDomain.Create(logger, useConsole,
            webAgentInstallerSettings, messagesDataManager, userName, cancellationToken);

        if (localInstallerSettingsDomain is not null)
            return new ProjectsManagerLocalWithFileStorage(logger, useConsole, fileStorageForUpload,
                localInstallerSettingsDomain, messagesDataManager, userName);

        if (messagesDataManager is not null)
            await messagesDataManager.SendMessage(userName, "localInstallerSettingsDomain does not created",
                cancellationToken);
        logger.LogError("localInstallerSettingsDomain does not created");
        return null;
    }

    public static async ValueTask<IProjectsManager?> CreateAgentClient(ILogger logger, bool useConsole,
        string? installFolder, IMessagesDataManager? messagesDataManager, string? userName,
        CancellationToken cancellationToken = default)
    {
        if (messagesDataManager is not null)
            await messagesDataManager.SendMessage(userName, "Creating local Agent", cancellationToken);

        if (!string.IsNullOrWhiteSpace(installFolder))
            return new ProjectsManagerLocal(logger, useConsole, installFolder, messagesDataManager, userName);

        if (messagesDataManager is not null)
            await messagesDataManager.SendMessage(userName, "installFolder name in parameters is empty",
                cancellationToken);
        logger.LogError("installFolder name in parameters is empty");
        return null;
    }
}