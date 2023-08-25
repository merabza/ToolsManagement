using System.Threading.Tasks;
using Installer.Domain;
using LibFileParameters.Models;
using Microsoft.Extensions.Logging;
using WebAgentMessagesContracts;

namespace Installer.AgentClients;

public sealed class LocalAgentWithFileStorage : IAgentClientWithFileStorage
{
    private readonly FileStorageData _fileStorageForUpload;
    private readonly LocalInstallerSettingsDomain _localInstallerSettings;
    private readonly IMessagesDataManager? _messagesDataManager;
    private readonly string? _userName;
    private readonly ILogger _logger;
    private readonly bool _useConsole;

    public LocalAgentWithFileStorage(ILogger logger, bool useConsole, FileStorageData fileStorageForUpload,
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

    public bool UpdateAppParametersFile(string projectName, string environmentName, string? serviceName,
        string appSettingsFileName, string parametersFileDateMask, string parametersFileExtension)
    {
        var applicationUpdater = AppParametersFileUpdater.Create(_logger, _useConsole, parametersFileDateMask,
            parametersFileExtension, _fileStorageForUpload, _localInstallerSettings.FilesUserName,
            _localInstallerSettings.FilesUsersGroupName, _localInstallerSettings.InstallFolder,
            _localInstallerSettings.DotnetRunner, _messagesDataManager, _userName);
        return applicationUpdater?.UpdateParameters(projectName, environmentName, serviceName, appSettingsFileName) ??
               false;
    }

    public string? InstallProgram(string projectName, string environmentName, string programArchiveDateMask,
        string programArchiveExtension, string parametersFileDateMask, string parametersFileExtension)
    {
        var applicationUpdater = ApplicationUpdater.Create(_logger, _useConsole, programArchiveDateMask,
            programArchiveExtension, parametersFileDateMask, parametersFileExtension, _fileStorageForUpload,
            _localInstallerSettings.InstallerWorkFolder, _localInstallerSettings.FilesUserName,
            _localInstallerSettings.FilesUsersGroupName, _localInstallerSettings.ServiceUserName,
            _localInstallerSettings.DownloadTempExtension, _localInstallerSettings.InstallFolder,
            _localInstallerSettings.DotnetRunner, _messagesDataManager, _userName);
        return applicationUpdater?.UpdateProgram(projectName, environmentName);
    }

    public string? InstallService(string projectName, string environmentName, string? serviceName,
        string serviceUserName,
        string appSettingsFileName, string programArchiveDateMask, string programArchiveExtension,
        string parametersFileDateMask, string parametersFileExtension)
    {
        var applicationUpdater = ApplicationUpdater.Create(_logger, _useConsole, programArchiveDateMask,
            programArchiveExtension, parametersFileDateMask, parametersFileExtension, _fileStorageForUpload,
            _localInstallerSettings.InstallerWorkFolder, _localInstallerSettings.FilesUserName,
            _localInstallerSettings.FilesUsersGroupName, _localInstallerSettings.ServiceUserName,
            _localInstallerSettings.DownloadTempExtension, _localInstallerSettings.InstallFolder,
            _localInstallerSettings.DotnetRunner, _messagesDataManager, _userName);
        return applicationUpdater?.UpdateServiceWithParameters(projectName, environmentName, serviceUserName,
            serviceName, appSettingsFileName);
    }

    public async Task<bool> CheckValidation()
    {
        return await Task.FromResult(true);
    }
}