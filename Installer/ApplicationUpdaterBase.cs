using System;
using Installer.Actions;
using LibFileParameters.Models;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace Installer;

public /*open*/ class ApplicationUpdaterBase
{
    protected readonly ILogger Logger;
    protected readonly bool UseConsole;
    protected readonly IMessagesDataManager? MessagesDataManager;
    protected readonly string? UserName;

    protected ApplicationUpdaterBase(ILogger logger, bool useConsole, IMessagesDataManager? messagesDataManager,
        string? userName)
    {
        Logger = logger;
        UseConsole = useConsole;
        MessagesDataManager = messagesDataManager;
        UserName = userName;
    }

    protected string? GetParametersFileBody(string projectName, string environmentName,
        FileStorageData fileStorageForDownload, string parametersFileDateMask, string parametersFileExtension)
    {
        var getLatestParametersFileBodyAction = new GetLatestParametersFileBodyAction(Logger, UseConsole,
            fileStorageForDownload, projectName, Environment.MachineName, environmentName, parametersFileDateMask,
            parametersFileExtension, MessagesDataManager, UserName);
        var result = getLatestParametersFileBodyAction.Run();
        var appSettingsFileBody = getLatestParametersFileBodyAction.LatestParametersFileContent;
        if (!result || string.IsNullOrWhiteSpace(appSettingsFileBody))
            return null;
        return appSettingsFileBody;
    }
}