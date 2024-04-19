using System.Threading;
using System.Threading.Tasks;
using Installer.Domain;
using Installer.ServiceInstaller;
using LanguageExt;
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

    public static async Task<AppParametersFileUpdater?> Create(ILogger logger, bool useConsole,
        string parametersFileDateMask, string parametersFileExtension, FileStorageData fileStorageForUpload,
        string? filesUserName, string? filesUsersGroupName, string? installFolder, string? dotnetRunner,
        IMessagesDataManager? messagesDataManager, string? userName, CancellationToken cancellationToken)
    {
        if (messagesDataManager is not null)
            await messagesDataManager.SendMessage(userName, "creating AppParametersFileUpdater", cancellationToken);

        if (string.IsNullOrWhiteSpace(installFolder))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName, "installFolder is empty", cancellationToken);
            logger.LogError("installFolder is empty");
            return null;
        }

        if (string.IsNullOrWhiteSpace(filesUserName))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName, "filesUserName is empty", cancellationToken);
            logger.LogError("filesUserName is empty");
            return null;
        }

        if (string.IsNullOrWhiteSpace(filesUsersGroupName))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName, "filesUsersGroupName is empty", cancellationToken);
            logger.LogError("filesUsersGroupName is empty");
            return null;
        }

        var serviceInstaller = await InstallerFabric.CreateInstaller(logger, useConsole, dotnetRunner,
            messagesDataManager, userName, cancellationToken);

        if (serviceInstaller == null)
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName, "Installer does Not Created", cancellationToken);
            logger.LogError("Installer does Not Created");
            return null;
        }


        var applicationUpdaterParameters =
            new AppParametersFileUpdaterParameters(fileStorageForUpload, parametersFileDateMask,
                parametersFileExtension, filesUserName, filesUsersGroupName, installFolder);

        return new AppParametersFileUpdater(logger, useConsole, applicationUpdaterParameters, serviceInstaller,
            messagesDataManager, userName);
    }


    public async Task<Option<Err[]>> UpdateParameters(string projectName, string environmentName, bool isService,
        string appSettingsFileName, CancellationToken cancellationToken)
    {
        if (projectName == ProgramAttributes.Instance.GetAttribute<string>("AppName"))
        {
            if (MessagesDataManager is not null)
                await MessagesDataManager.SendMessage(UserName, "Installer does Not Created", cancellationToken);
            Logger.LogError("Cannot update self");
            return new Err[]
            {
                new()
                {
                    ErrorCode = "CannotUpdateSelf",
                    ErrorMessage = "Cannot update self"
                }
            };
        }

        //მოვქაჩოთ ბოლო პარამეტრების ფაილი
        var appSettingsFileBody = await GetParametersFileBody(projectName, environmentName,
            _applicationUpdaterParameters.ProgramExchangeFileStorage,
            _applicationUpdaterParameters.ParametersFileDateMask, _applicationUpdaterParameters.ParametersFileExtension,
            cancellationToken);
        if (appSettingsFileBody == null)
            return new Err[]
            {
                new()
                {
                    ErrorCode = "CannotUpdateSelf",
                    ErrorMessage = "Cannot update self"
                }
            };

        return await _serviceInstaller.RunUpdateSettings(projectName, isService, environmentName, appSettingsFileName,
            appSettingsFileBody, _applicationUpdaterParameters.FilesUserName,
            _applicationUpdaterParameters.FilesUsersGroupName, _applicationUpdaterParameters.InstallFolder,
            cancellationToken);
    }
}