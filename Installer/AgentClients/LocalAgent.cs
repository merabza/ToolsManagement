using System.Threading.Tasks;
using Installer.ServiceInstaller;
using Microsoft.Extensions.Logging;

namespace Installer.AgentClients;

public sealed class LocalAgent : IAgentClient
{
    private readonly string _installFolder;
    private readonly ILogger _logger;
    private readonly bool _useConsole;

    public LocalAgent(ILogger logger, bool useConsole, string installFolder)
    {
        _logger = logger;
        _useConsole = useConsole;
        _installFolder = installFolder;
    }

    public bool RemoveProject(string projectName)
    {
        //დავადგინოთ რა პლატფორმაზეა გაშვებული პროგრამა: ვინდოუსი თუ ლინუქსი
        var serviceInstaller = InstallerFabric.CreateInstaller(_logger, _useConsole);

        if (serviceInstaller.RemoveProject(projectName, _installFolder))
            return true;

        _logger.LogError($"Project {projectName} can not removed");
        return false;
    }

    public bool RemoveProjectAndService(string projectName, string serviceName)
    {
        //დავადგინოთ რა პლატფორმაზეა გაშვებული პროგრამა: ვინდოუსი თუ ლინუქსი
        var serviceInstaller = InstallerFabric.CreateInstaller(_logger, _useConsole);

        if (serviceInstaller.RemoveProjectAndService(projectName, serviceName, _installFolder))
            return true;

        _logger.LogError($"Project {projectName} => service {serviceName} can not removed");
        return false;
    }

    public bool StopService(string serviceName)
    {
        //დავადგინოთ რა პლატფორმაზეა გაშვებული პროგრამა: ვინდოუსი თუ ლინუქსი
        var serviceInstaller = InstallerFabric.CreateInstaller(_logger, _useConsole);

        return serviceInstaller.Stop(serviceName);
    }

    public bool StartService(string serviceName)
    {
        //დავადგინოთ რა პლატფორმაზეა გაშვებული პროგრამა: ვინდოუსი თუ ლინუქსი
        var serviceInstaller = InstallerFabric.CreateInstaller(_logger, _useConsole);

        return serviceInstaller.Start(serviceName);
    }

    public async Task<bool> CheckValidation()
    {
        return await Task.FromResult(true);
    }
}