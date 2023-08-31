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
        AppParametersFileUpdaterParameters applicationUpdaterParameters, InstallerBase serviceInstaller,
        IMessagesDataManager? messagesDataManager, string? userName) : base(logger, useConsole, messagesDataManager,
        userName)
    {
        _applicationUpdaterParameters = applicationUpdaterParameters;
        _serviceInstaller = serviceInstaller;
    }

    public static AppParametersFileUpdater? Create(ILogger logger, bool useConsole, string parametersFileDateMask,
        string parametersFileExtension, FileStorageData fileStorageForUpload, string? filesUserName,
        string? filesUsersGroupName, string? installFolder, string? dotnetRunner,
        IMessagesDataManager? messagesDataManager, string? userName)
    {
        messagesDataManager?.SendMessage(userName, "creating AppParametersFileUpdater").Wait();

        if (string.IsNullOrWhiteSpace(installFolder))
        {
            messagesDataManager?.SendMessage(userName, "installFolder is empty").Wait();
            logger.LogError("installFolder is empty");
            return null;
        }

        if (string.IsNullOrWhiteSpace(filesUserName))
        {
            messagesDataManager?.SendMessage(userName, "filesUserName is empty").Wait();
            logger.LogError("filesUserName is empty");
            return null;
        }

        if (string.IsNullOrWhiteSpace(filesUsersGroupName))
        {
            messagesDataManager?.SendMessage(userName, "filesUsersGroupName is empty").Wait();
            logger.LogError("filesUsersGroupName is empty");
            return null;
        }

        var serviceInstaller =
            InstallerFabric.CreateInstaller(logger, useConsole, dotnetRunner, messagesDataManager, userName);

        if (serviceInstaller == null)
        {
            messagesDataManager?.SendMessage(userName, "Installer does Not Created").Wait();
            logger.LogError("Installer does Not Created");
            return null;
        }


        var applicationUpdaterParameters =
            new AppParametersFileUpdaterParameters(fileStorageForUpload, parametersFileDateMask,
                parametersFileExtension, filesUserName, filesUsersGroupName, installFolder);

        return new AppParametersFileUpdater(logger, useConsole, applicationUpdaterParameters, serviceInstaller,
            messagesDataManager, userName);
    }


    public bool UpdateParameters(string projectName, string environmentName, string? serviceName,
        string appSettingsFileName)
    {
        if (projectName == ProgramAttributes.Instance.GetAttribute<string>("AppName"))
        {
            MessagesDataManager?.SendMessage(UserName, "Installer does Not Created").Wait();
            Logger.LogError("Cannot update self");
            return false;
        }

        //მოვქაჩოთ ბოლო პარამეტრების ფაილი
        var appSettingsFileBody = GetParametersFileBody(projectName, environmentName,
            _applicationUpdaterParameters.ProgramExchangeFileStorage,
            _applicationUpdaterParameters.ParametersFileDateMask,
            _applicationUpdaterParameters.ParametersFileExtension);
        if (appSettingsFileBody == null)
            return false;

        return _serviceInstaller.RunUpdateSettings(projectName, serviceName, environmentName, appSettingsFileName,
            appSettingsFileBody, _applicationUpdaterParameters.FilesUserName,
            _applicationUpdaterParameters.FilesUsersGroupName, _applicationUpdaterParameters.InstallFolder);
    }
}