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
    private readonly ILogger _logger;
    private readonly IMessagesDataManager? _messagesDataManager;
    private readonly bool _useConsole;
    private readonly string? _userName;

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

    public async Task<Option<Err[]>> RemoveProjectAndService(string projectName, string environmentName, bool isService,
        CancellationToken cancellationToken)
    {
        //დავადგინოთ რა პლატფორმაზეა გაშვებული პროგრამა: ვინდოუსი თუ ლინუქსი
        var serviceInstaller = await InstallerFabric.CreateInstaller(_logger, _useConsole, _messagesDataManager,
            _userName, cancellationToken);

        var removeProjectAndServiceResult = await serviceInstaller.RemoveProjectAndService(projectName, environmentName,
            isService, _installFolder, cancellationToken);

        if (removeProjectAndServiceResult.IsNone)
            return null;

        if (_messagesDataManager is not null)
            await _messagesDataManager.SendMessage(_userName,
                $"Service {projectName}/{environmentName} can not removed", cancellationToken);
        _logger.LogError("Service {projectName}/{environmentName} can not removed", projectName, environmentName);
        return new Err[]
        {
            new()
            {
                ErrorCode = "ProjectServiceCanNotRemoved",
                ErrorMessage = $"Project {projectName} => service {projectName}/{environmentName} can not removed"
            }
        };
    }

    public async Task<Option<Err[]>> StopService(string projectName, string environmentName,
        CancellationToken cancellationToken)
    {
        //დავადგინოთ რა პლატფორმაზეა გაშვებული პროგრამა: ვინდოუსი თუ ლინუქსი
        var serviceInstaller = await InstallerFabric.CreateInstaller(_logger, _useConsole, _messagesDataManager,
            _userName, cancellationToken);

        var stopResult = await serviceInstaller.Stop(projectName, environmentName, cancellationToken);
        if (stopResult.IsNone)
            return null;
        return new Err[]
        {
            new()
            {
                ErrorCode = "ServiceCanNotBeStopped",
                ErrorMessage = $"service {projectName}/{environmentName} can not be stopped"
            }
        };
    }

    public async Task<Option<Err[]>> StartService(string projectName, string environmentName,
        CancellationToken cancellationToken)
    {
        //დავადგინოთ რა პლატფორმაზეა გაშვებული პროგრამა: ვინდოუსი თუ ლინუქსი
        var serviceInstaller = await InstallerFabric.CreateInstaller(_logger, _useConsole, _messagesDataManager,
            _userName, cancellationToken);


        var stopResult = await serviceInstaller.Start(projectName, environmentName, cancellationToken);
        if (stopResult.IsNone)
            return null;
        return new Err[]
        {
            new()
            {
                ErrorCode = "ServiceCanNotBeStarted",
                ErrorMessage = $"service {projectName}/{environmentName} can not be started"
            }
        };
    }

    public async Task<Option<Err[]>> RemoveProject(string projectName, string environmentName,
        CancellationToken cancellationToken)
    {
        //დავადგინოთ რა პლატფორმაზეა გაშვებული პროგრამა: ვინდოუსი თუ ლინუქსი
        var serviceInstaller = await InstallerFabric.CreateInstaller(_logger, _useConsole, _messagesDataManager,
            _userName, cancellationToken);

        var removeProjectResult =
            await serviceInstaller.RemoveProject(projectName, environmentName, _installFolder, cancellationToken);
        if (removeProjectResult.IsNone)
            return null;

        if (_messagesDataManager is not null)
            await _messagesDataManager.SendMessage(_userName, $"Project {projectName} can not removed",
                cancellationToken);
        _logger.LogError("Project {projectName} can not removed", projectName);
        return Err.RecreateErrors((Err[])removeProjectResult,
            new Err
            {
                ErrorCode = "ProjectCanNotBeRemoved", ErrorMessage = $"Project {projectName} can not be removed"
            });
    }
}