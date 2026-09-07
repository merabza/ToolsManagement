using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibFileParameters.Models;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;
using ToolsManagement.Installer.Domain;
using ToolsManagement.Installer.Errors;

// ReSharper disable ConvertToPrimaryConstructor

namespace ToolsManagement.Installer.ProjectManagers;

public sealed class ProjectsManagerLocalWithFileStorage : IIProjectsManagerWithFileStorage
{
    private readonly string _appName;
    private readonly FileStorageData _fileStorageForUpload;
    private readonly LocalInstallerSettingsDomain _localInstallerSettings;
    private readonly ILogger _logger;
    private readonly IMessagesDataManager? _messagesDataManager;
    private readonly bool _useConsole;
    private readonly string? _userName;

    public ProjectsManagerLocalWithFileStorage(string appName, ILogger logger, bool useConsole,
        FileStorageData fileStorageForUpload, LocalInstallerSettingsDomain localInstallerSettings,
        IMessagesDataManager? messagesDataManager, string? userName)
    {
        _appName = appName;
        _logger = logger;
        _fileStorageForUpload = fileStorageForUpload;
        _localInstallerSettings = localInstallerSettings;
        _messagesDataManager = messagesDataManager;
        _userName = userName;
        _useConsole = useConsole;
    }

    public async ValueTask<Result> UpdateAppParametersFile(string projectName, string environmentName,
        string appSettingsFileName, string parametersFileDateMask, string parametersFileExtension,
        CancellationToken cancellationToken = default)
    {
        var applicationUpdater = await AppParametersFileUpdater.Create(_appName, _logger, _useConsole,
            parametersFileDateMask, parametersFileExtension, _fileStorageForUpload,
            _localInstallerSettings.FilesUserName, _localInstallerSettings.FilesUsersGroupName,
            _localInstallerSettings.InstallFolder, _localInstallerSettings.DotnetRunner, _messagesDataManager,
            _userName, cancellationToken);

        if (applicationUpdater is null)
        {
            return ProjectManagersErrors.AppParametersFileUpdaterCreateError;
        }

        return await applicationUpdater.UpdateParameters(projectName, environmentName, appSettingsFileName,
            cancellationToken);
    }

    public async ValueTask<Result<string>> InstallProgram(string projectName, string environmentName,
        string programArchiveDateMask, string programArchiveExtension, string parametersFileDateMask,
        string parametersFileExtension, CancellationToken cancellationToken = default)
    {
        Result<ApplicationUpdater> applicationUpdaterCreateResult = await ApplicationUpdater.Create(_appName,
            _logger, _useConsole, programArchiveDateMask, programArchiveExtension, parametersFileDateMask,
            parametersFileExtension, _fileStorageForUpload, _localInstallerSettings.InstallerWorkFolder,
            _localInstallerSettings.FilesUserName, _localInstallerSettings.FilesUsersGroupName,
            _localInstallerSettings.ServiceUserName, _localInstallerSettings.DownloadTempExtension,
            _localInstallerSettings.InstallFolder, _localInstallerSettings.DotnetRunner, _messagesDataManager,
            _userName, cancellationToken);
        if (applicationUpdaterCreateResult.IsFailure)
        {
            return applicationUpdaterCreateResult.Error;
        }

        ApplicationUpdater applicationUpdater = applicationUpdaterCreateResult.Value;
        return await applicationUpdater.UpdateProgram(projectName, environmentName, cancellationToken);
    }

    public async ValueTask<Result<string>> InstallService(string projectName, string environmentName,
        string serviceUserName, string appSettingsFileName, string programArchiveDateMask,
        string programArchiveExtension, string parametersFileDateMask, string parametersFileExtension,
        string? serviceDescriptionSignature, string? projectDescription, CancellationToken cancellationToken = default)
    {
        Result<ApplicationUpdater> applicationUpdaterCreateResult = await ApplicationUpdater.Create(_appName,
            _logger, _useConsole, programArchiveDateMask, programArchiveExtension, parametersFileDateMask,
            parametersFileExtension, _fileStorageForUpload, _localInstallerSettings.InstallerWorkFolder,
            _localInstallerSettings.FilesUserName, _localInstallerSettings.FilesUsersGroupName,
            _localInstallerSettings.ServiceUserName, _localInstallerSettings.DownloadTempExtension,
            _localInstallerSettings.InstallFolder, _localInstallerSettings.DotnetRunner, _messagesDataManager,
            _userName, cancellationToken);
        if (applicationUpdaterCreateResult.IsFailure)
        {
            return applicationUpdaterCreateResult.Error;
        }

        ApplicationUpdater applicationUpdater = applicationUpdaterCreateResult.Value;
        Result<string> updateServiceWithParametersResult =
            await applicationUpdater.UpdateServiceWithParameters(projectName, environmentName, serviceUserName,
                appSettingsFileName, serviceDescriptionSignature, projectDescription, cancellationToken);
        return updateServiceWithParametersResult;
    }
}
