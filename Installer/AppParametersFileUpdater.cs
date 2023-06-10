using Installer.Domain;
using Installer.ServiceInstaller;
using LibFileParameters.Models;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace Installer;

public sealed class AppParametersFileUpdater : ApplicationUpdaterBase
{
    private readonly AppParametersFileUpdaterParameters _applicationUpdaterParameters;
    private readonly InstallerBase _serviceInstaller;

    private AppParametersFileUpdater(ILogger logger, bool useConsole,
        AppParametersFileUpdaterParameters applicationUpdaterParameters, InstallerBase serviceInstaller) : base(logger,
        useConsole)
    {
        _applicationUpdaterParameters = applicationUpdaterParameters;
        _serviceInstaller = serviceInstaller;
    }

    public static AppParametersFileUpdater? Create(ILogger logger, bool useConsole, string parametersFileDateMask,
        string parametersFileExtension, FileStorageData fileStorageForUpload, string? filesUserName,
        string? filesUsersGroupName, string? installFolder, string? dotnetRunner)
    {
        if (string.IsNullOrWhiteSpace(installFolder))
        {
            logger.LogError("filesUserName is empty");
            return null;
        }

        if (string.IsNullOrWhiteSpace(filesUserName))
        {
            logger.LogError("filesUserName is empty");
            return null;
        }

        if (string.IsNullOrWhiteSpace(filesUsersGroupName))
        {
            logger.LogError("filesUsersGroupName is empty");
            return null;
        }

        var serviceInstaller = InstallerFabric.CreateInstaller(logger, useConsole, dotnetRunner);

        if (serviceInstaller == null)
        {
            logger.LogError("Installer does Not Created");
            return null;
        }


        var applicationUpdaterParameters =
            new AppParametersFileUpdaterParameters(fileStorageForUpload, parametersFileDateMask,
                parametersFileExtension, filesUserName, filesUsersGroupName, installFolder);

        return new AppParametersFileUpdater(logger, useConsole, applicationUpdaterParameters, serviceInstaller);
    }


    public bool UpdateParameters(string projectName, string? serviceName, string appSettingsFileName)
    {
        if (projectName == ProgramAttributes.Instance.GetAttribute<string>("AppName"))
        {
            Logger.LogError("Cannot update self");
            return false;
        }

        //მოვქაჩოთ ბოლო პარამეტრების ფაილი
        var appSettingsFileBody = GetParametersFileBody(projectName,
            _applicationUpdaterParameters.ProgramExchangeFileStorage,
            _applicationUpdaterParameters.ParametersFileDateMask,
            _applicationUpdaterParameters.ParametersFileExtension);
        if (appSettingsFileBody == null)
            return false;

        return _serviceInstaller.RunUpdateSettings(projectName, serviceName, appSettingsFileName, appSettingsFileBody,
            _applicationUpdaterParameters.FilesUserName, _applicationUpdaterParameters.FilesUsersGroupName,
            _applicationUpdaterParameters.InstallFolder);
    }
}