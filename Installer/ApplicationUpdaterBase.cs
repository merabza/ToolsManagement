using System;
using Installer.Actions;
using LibFileParameters.Models;
using Microsoft.Extensions.Logging;

namespace Installer;

public /*open*/ class ApplicationUpdaterBase
{
    protected readonly ILogger Logger;
    protected readonly bool UseConsole;

    protected ApplicationUpdaterBase(ILogger logger, bool useConsole)
    {
        Logger = logger;
        UseConsole = useConsole;
    }

    protected string? GetParametersFileBody(string projectName, FileStorageData fileStorageForDownload,
        string parametersFileDateMask, string parametersFileExtension)
    {
        var getLatestParametersFileBodyAction =
            new GetLatestParametersFileBodyAction(Logger, UseConsole, fileStorageForDownload, projectName,
                Environment.MachineName, parametersFileDateMask, parametersFileExtension);
        var result = getLatestParametersFileBodyAction.Run();
        var appSettingsFileBody = getLatestParametersFileBodyAction.LatestParametersFileContent;
        if (!result || string.IsNullOrWhiteSpace(appSettingsFileBody))
            return null;
        return appSettingsFileBody;
    }
}