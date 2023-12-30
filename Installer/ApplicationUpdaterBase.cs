using System;
using System.Threading;
using System.Threading.Tasks;
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

    protected async Task<string?> GetParametersFileBody(string projectName, string environmentName,
        FileStorageData fileStorageForDownload, string parametersFileDateMask, string parametersFileExtension,
        CancellationToken cancellationToken)
    {
        var getLatestParametersFileBodyAction = new GetLatestParametersFileBodyAction(Logger, UseConsole,
            fileStorageForDownload, projectName, Environment.MachineName, environmentName, parametersFileDateMask,
            parametersFileExtension, MessagesDataManager, UserName);
        var result = await getLatestParametersFileBodyAction.Run(cancellationToken);
        var appSettingsFileBody = getLatestParametersFileBodyAction.LatestParametersFileContent;
        if (!result || string.IsNullOrWhiteSpace(appSettingsFileBody))
            return null;
        return appSettingsFileBody;
    }
}