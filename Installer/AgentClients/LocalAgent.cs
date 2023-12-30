using System.Threading;
using System.Threading.Tasks;
using Installer.ServiceInstaller;
using LanguageExt;
using Microsoft.Extensions.Logging;
using SystemToolsShared;
// ReSharper disable ConvertToPrimaryConstructor

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

    public async Task<Option<Err[]>> RemoveProject(string projectName, string environmentName, CancellationToken cancellationToken)
    {
        //დავადგინოთ რა პლატფორმაზეა გაშვებული პროგრამა: ვინდოუსი თუ ლინუქსი
        var serviceInstaller = await InstallerFabric.CreateInstaller(_logger, _useConsole, _messagesDataManager, _userName, cancellationToken);

        var removeProjectResult = await serviceInstaller.RemoveProject(projectName, environmentName, _installFolder, cancellationToken);
        if (removeProjectResult.IsNone)
            return null;

        if (_messagesDataManager is not null)
            await _messagesDataManager.SendMessage(_userName, $"Project {projectName} can not removed", cancellationToken);
        _logger.LogError("Project {projectName} can not removed", projectName);
        return Err.RecreateErrors((Err[])removeProjectResult, new Err {ErrorCode = "ProjectCanNotBeRemoved", ErrorMessage = $"Project {projectName} can not be removed"}) ;
    }

    public async Task<Option<Err[]>> RemoveProjectAndService(string projectName, string serviceName, string environmentName,
        CancellationToken cancellationToken)
    {
        //დავადგინოთ რა პლატფორმაზეა გაშვებული პროგრამა: ვინდოუსი თუ ლინუქსი
        var serviceInstaller = await InstallerFabric.CreateInstaller(_logger, _useConsole, _messagesDataManager, _userName, cancellationToken);


        //public async Task<Option<Err[]>> RemoveProjectAndService(string projectName, string serviceName,
        //    string environmentName, string installFolder, CancellationToken cancellationToken)

        var removeProjectAndServiceResult = await serviceInstaller.RemoveProjectAndService(projectName, serviceName,
            environmentName, _installFolder, cancellationToken);

        if (removeProjectAndServiceResult.IsNone)
            return null;

        if (_messagesDataManager is not null)
            await _messagesDataManager.SendMessage(_userName,
                $"Project {projectName} => service {serviceName}/{environmentName} can not removed", cancellationToken);
        _logger.LogError("Project {projectName} => service {serviceName}/{environmentName} can not removed",
            projectName, serviceName, environmentName);
        return new Err[]
        {
            new()
            {
                ErrorCode = "ProjectServiceCanNotRemoved",
                ErrorMessage = $"Project {projectName} => service {serviceName}/{environmentName} can not removed"
            }
        };
    }

    public async Task<Option<Err[]>> StopService(string serviceName, string environmentName, CancellationToken cancellationToken)
    {
        //დავადგინოთ რა პლატფორმაზეა გაშვებული პროგრამა: ვინდოუსი თუ ლინუქსი
        var serviceInstaller = await InstallerFabric.CreateInstaller(_logger, _useConsole, _messagesDataManager, _userName, cancellationToken);

        var stopResult = await serviceInstaller.Stop(serviceName, environmentName, cancellationToken);
        if (stopResult.IsNone)
            return null;
        return new Err[]
        {
            new()
            {
                ErrorCode = "ServiceCanNotBeStopped",
                ErrorMessage = $"service {serviceName}/{environmentName} can not be stopped"
            }
        };
    }

    public async Task<Option<Err[]>> StartService(string serviceName, string environmentName, CancellationToken cancellationToken)
    {
        //დავადგინოთ რა პლატფორმაზეა გაშვებული პროგრამა: ვინდოუსი თუ ლინუქსი
        var serviceInstaller = await InstallerFabric.CreateInstaller(_logger, _useConsole, _messagesDataManager, _userName, cancellationToken);


        var stopResult = await serviceInstaller.Start(serviceName, environmentName, cancellationToken);
        if (stopResult.IsNone)
            return null;
        return new Err[]
        {
            new()
            {
                ErrorCode = "ServiceCanNotBeStarted",
                ErrorMessage = $"service {serviceName}/{environmentName} can not be started"
            }
        };
    }

    public async Task<bool> CheckValidation(CancellationToken cancellationToken)
    {
        return await Task.FromResult(true);
    }
}