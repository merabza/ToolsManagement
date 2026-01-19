using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibFileParameters.Models;
using SystemTools.SystemToolsShared;
using ToolsManagement.Installer.ToolActions;

namespace ToolsManagement.Installer;

public /*open*/ class ApplicationUpdaterBase : MessageLogger
{
    private readonly ILogger _logger;
    private readonly IMessagesDataManager? _messagesDataManager;
    private readonly string? _userName;

    protected ApplicationUpdaterBase(ILogger logger, bool useConsole, IMessagesDataManager? messagesDataManager,
        string? userName) : base(logger, messagesDataManager, userName, useConsole)
    {
        _logger = logger;
        _messagesDataManager = messagesDataManager;
        _userName = userName;
    }

    protected async Task<string?> GetParametersFileBody(string projectName, string environmentName,
        FileStorageData fileStorageForDownload, string parametersFileDateMask, string parametersFileExtension,
        CancellationToken cancellationToken = default)
    {
        var getLatestParametersFileBodyAction = new GetLatestParametersFileBodyAction(_logger, UseConsole,
            fileStorageForDownload, projectName, Environment.MachineName, environmentName, parametersFileDateMask,
            parametersFileExtension, _messagesDataManager, _userName);
        var result = await getLatestParametersFileBodyAction.Run(cancellationToken);
        var appSettingsFileBody = getLatestParametersFileBodyAction.LatestParametersFileContent;
        return !result || string.IsNullOrWhiteSpace(appSettingsFileBody) ? null : appSettingsFileBody;
    }
}