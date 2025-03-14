﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Installer.ToolActions;
using LibFileParameters.Models;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace Installer;

public /*open*/ class ApplicationUpdaterBase : MessageLogger
{
    private readonly ILogger _logger;
    private readonly IMessagesDataManager? _messagesDataManager;
    private readonly string? _userName;
    protected readonly bool UseConsole;

    protected ApplicationUpdaterBase(ILogger logger, bool useConsole, IMessagesDataManager? messagesDataManager,
        string? userName) : base(logger, messagesDataManager, userName, useConsole)
    {
        _logger = logger;
        UseConsole = useConsole;
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