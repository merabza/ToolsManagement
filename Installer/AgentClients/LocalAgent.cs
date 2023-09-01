using System.Threading.Tasks;
using Installer.ServiceInstaller;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace Installer.AgentClients;

public sealed class ProjectsLocalAgent : IProjectsApiClient
{
    private readonly string _installFolder;
    private readonly IMessagesDataManager? _messagesDataManager;
    private readonly string? _userName;
    private readonly ILogger _logger;
    private readonly bool _useConsole;

    public ProjectsLocalAgent(ILogger logger, bool useConsole, string installFolder,
        IMessagesDataManager? messagesDataManager,
        string? userName)
    {
        _logger = logger;
        _useConsole = useConsole;
        _installFolder = installFolder;
        _messagesDataManager = messagesDataManager;
        _userName = userName;
    }

    public async Task<bool> RemoveProject(string projectName, string environmentName)
    {
        //დავადგინოთ რა პლატფორმაზეა გაშვებული პროგრამა: ვინდოუსი თუ ლინუქსი
        var serviceInstaller = InstallerFabric.CreateInstaller(_logger, _useConsole, _messagesDataManager, _userName);

        if (serviceInstaller.RemoveProject(projectName, environmentName, _installFolder))
            return await Task.FromResult(true);

        if (_messagesDataManager is not null)
            await _messagesDataManager.SendMessage(_userName, $"Project {projectName} can not removed");
        _logger.LogError("Project {projectName} can not removed", projectName);
        return await Task.FromResult(false);
    }

    public async Task<bool> RemoveProjectAndService(string projectName, string serviceName, string environmentName)
    {
        //დავადგინოთ რა პლატფორმაზეა გაშვებული პროგრამა: ვინდოუსი თუ ლინუქსი
        var serviceInstaller = InstallerFabric.CreateInstaller(_logger, _useConsole, _messagesDataManager, _userName);

        if (serviceInstaller.RemoveProjectAndService(projectName, serviceName, environmentName, _installFolder))
            return await Task.FromResult(true);

        if (_messagesDataManager is not null)
            await _messagesDataManager.SendMessage(_userName,
                $"Project {projectName} => service {serviceName}/{environmentName} can not removed");
        _logger.LogError("Project {projectName} => service {serviceName}/{environmentName} can not removed",
            projectName, serviceName, environmentName);
        return await Task.FromResult(false);
    }

    public async Task<bool> StopService(string serviceName, string environmentName)
    {
        //დავადგინოთ რა პლატფორმაზეა გაშვებული პროგრამა: ვინდოუსი თუ ლინუქსი
        var serviceInstaller = InstallerFabric.CreateInstaller(_logger, _useConsole, _messagesDataManager, _userName);

        return await Task.FromResult(serviceInstaller.Stop(serviceName, environmentName));
    }

    public async Task<bool> StartService(string serviceName, string environmentName)
    {
        //დავადგინოთ რა პლატფორმაზეა გაშვებული პროგრამა: ვინდოუსი თუ ლინუქსი
        var serviceInstaller = InstallerFabric.CreateInstaller(_logger, _useConsole, _messagesDataManager, _userName);

        return await Task.FromResult(serviceInstaller.Start(serviceName, environmentName));
    }

    public async Task<bool> CheckValidation()
    {
        return await Task.FromResult(true);
    }
}