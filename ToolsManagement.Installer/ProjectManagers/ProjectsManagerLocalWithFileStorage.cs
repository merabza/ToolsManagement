using System.Threading;
using System.Threading.Tasks;
using Installer;
using Installer.Domain;
using LanguageExt;
using Microsoft.Extensions.Logging;
using OneOf;
using ParametersManagement.LibFileParameters.Models;
using SystemTools.SystemToolsShared;
using SystemTools.SystemToolsShared.Errors;
using ToolsManagement.Installer.Errors;

// ReSharper disable ConvertToPrimaryConstructor

namespace ToolsManagement.Installer.ProjectManagers;

public sealed class ProjectsManagerLocalWithFileStorage : IIProjectsManagerWithFileStorage
{
    private readonly FileStorageData _fileStorageForUpload;
    private readonly LocalInstallerSettingsDomain _localInstallerSettings;
    private readonly ILogger _logger;
    private readonly IMessagesDataManager? _messagesDataManager;
    private readonly bool _useConsole;
    private readonly string? _userName;

    public ProjectsManagerLocalWithFileStorage(ILogger logger, bool useConsole, FileStorageData fileStorageForUpload,
        LocalInstallerSettingsDomain localInstallerSettings, IMessagesDataManager? messagesDataManager,
        string? userName)
    {
        _logger = logger;
        _fileStorageForUpload = fileStorageForUpload;
        _localInstallerSettings = localInstallerSettings;
        _messagesDataManager = messagesDataManager;
        _userName = userName;
        _useConsole = useConsole;
    }

    public async ValueTask<Option<Err[]>> UpdateAppParametersFile(string projectName, string environmentName,
        string appSettingsFileName, string parametersFileDateMask, string parametersFileExtension,
        CancellationToken cancellationToken = default)
    {
        var applicationUpdater = await AppParametersFileUpdater.Create(_logger, _useConsole, parametersFileDateMask,
            parametersFileExtension, _fileStorageForUpload, _localInstallerSettings.FilesUserName,
            _localInstallerSettings.FilesUsersGroupName, _localInstallerSettings.InstallFolder,
            _localInstallerSettings.DotnetRunner, _messagesDataManager, _userName, cancellationToken);

        if (applicationUpdater is null)
            return new[] { ProjectManagersErrors.AppParametersFileUpdaterCreateError };
        return await applicationUpdater.UpdateParameters(projectName, environmentName, appSettingsFileName,
            cancellationToken);
    }

    public async ValueTask<OneOf<string, Err[]>> InstallProgram(string projectName, string environmentName,
        string programArchiveDateMask, string programArchiveExtension, string parametersFileDateMask,
        string parametersFileExtension, CancellationToken cancellationToken = default)
    {
        var applicationUpdaterCreateResult = await ApplicationUpdater.Create(_logger, _useConsole,
            programArchiveDateMask, programArchiveExtension, parametersFileDateMask, parametersFileExtension,
            _fileStorageForUpload, _localInstallerSettings.InstallerWorkFolder, _localInstallerSettings.FilesUserName,
            _localInstallerSettings.FilesUsersGroupName, _localInstallerSettings.ServiceUserName,
            _localInstallerSettings.DownloadTempExtension, _localInstallerSettings.InstallFolder,
            _localInstallerSettings.DotnetRunner, _messagesDataManager, _userName, cancellationToken);
        if (applicationUpdaterCreateResult.IsT1)
            return applicationUpdaterCreateResult.AsT1;
        var applicationUpdater = applicationUpdaterCreateResult.AsT0;
        if (applicationUpdater is null)
            return new[] { ProjectManagersErrors.ApplicationUpdaterDoesNotCreated(projectName, environmentName) };
        return await applicationUpdater.UpdateProgram(projectName, environmentName, cancellationToken);
    }

    public async ValueTask<OneOf<string, Err[]>> InstallService(string projectName, string environmentName,
        string serviceUserName, string appSettingsFileName, string programArchiveDateMask,
        string programArchiveExtension, string parametersFileDateMask, string parametersFileExtension,
        string? serviceDescriptionSignature, string? projectDescription, CancellationToken cancellationToken = default)
    {
        var applicationUpdaterCreateResult = await ApplicationUpdater.Create(_logger, _useConsole,
            programArchiveDateMask, programArchiveExtension, parametersFileDateMask, parametersFileExtension,
            _fileStorageForUpload, _localInstallerSettings.InstallerWorkFolder, _localInstallerSettings.FilesUserName,
            _localInstallerSettings.FilesUsersGroupName, _localInstallerSettings.ServiceUserName,
            _localInstallerSettings.DownloadTempExtension, _localInstallerSettings.InstallFolder,
            _localInstallerSettings.DotnetRunner, _messagesDataManager, _userName, cancellationToken);
        if (applicationUpdaterCreateResult.IsT1)
            return applicationUpdaterCreateResult.AsT1;
        var applicationUpdater = applicationUpdaterCreateResult.AsT0;
        var updateServiceWithParametersResult = await applicationUpdater.UpdateServiceWithParameters(projectName,
            environmentName, serviceUserName, appSettingsFileName, serviceDescriptionSignature, projectDescription,
            cancellationToken);
        return updateServiceWithParametersResult;
    }
}