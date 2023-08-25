using System.ServiceProcess;
using System.Threading.Tasks;
using Installer.ServiceInstaller;
using LibWebAgentMessages;
using Microsoft.Extensions.Logging;

namespace Installer.AgentClients;

public sealed class LocalAgent : IAgentClient
{
    private readonly string _installFolder;
    private readonly IMessagesDataManager _messagesDataManager;
    private readonly string? _userName;
    private readonly ILogger _logger;
    private readonly bool _useConsole;

    public LocalAgent(ILogger logger, bool useConsole, string installFolder, IMessagesDataManager messagesDataManager,
        string? userName)
    {
        _logger = logger;
        _useConsole = useConsole;
        _installFolder = installFolder;
        _messagesDataManager = messagesDataManager;
        _userName = userName;
    }

    public async Task<bool> RemoveProject(string projectName)
    {
        //დავადგინოთ რა პლატფორმაზეა გაშვებული პროგრამა: ვინდოუსი თუ ლინუქსი
        var serviceInstaller = InstallerFabric.CreateInstaller(_logger, _useConsole, _messagesDataManager, _userName);

        if (serviceInstaller.RemoveProject(projectName, _installFolder))
            return await Task.FromResult(true);

        await _messagesDataManager.SendMessage(_userName, $"Project {projectName} can not removed");
        _logger.LogError("Project {projectName} can not removed", projectName);
        return await Task.FromResult(false);
    }

    public async Task<bool> RemoveProjectAndService(string projectName, string serviceName)
    {
        //დავადგინოთ რა პლატფორმაზეა გაშვებული პროგრამა: ვინდოუსი თუ ლინუქსი
        var serviceInstaller = InstallerFabric.CreateInstaller(_logger, _useConsole, _messagesDataManager, _userName);

        if (serviceInstaller.RemoveProjectAndService(projectName, serviceName, _installFolder))
            return await Task.FromResult(true);

        await _messagesDataManager.SendMessage(_userName,
            $"Project {projectName} => service {serviceName} can not removed");
        _logger.LogError("Project {projectName} => service {serviceName} can not removed", projectName, serviceName);
        return await Task.FromResult(false);
    }

    public bool StopService(string serviceName)
    {
        //დავადგინოთ რა პლატფორმაზეა გაშვებული პროგრამა: ვინდოუსი თუ ლინუქსი
        var serviceInstaller = InstallerFabric.CreateInstaller(_logger, _useConsole, _messagesDataManager, _userName);

        return serviceInstaller.Stop(serviceName);
    }

    public bool StartService(string serviceName)
    {
        //დავადგინოთ რა პლატფორმაზეა გაშვებული პროგრამა: ვინდოუსი თუ ლინუქსი
        var serviceInstaller = InstallerFabric.CreateInstaller(_logger, _useConsole, _messagesDataManager, _userName);

        return serviceInstaller.Start(serviceName);
    }

    public async Task<bool> CheckValidation()
    {
        return await Task.FromResult(true);
    }
}