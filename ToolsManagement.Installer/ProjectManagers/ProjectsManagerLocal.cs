using System.Threading;
using System.Threading.Tasks;
using LanguageExt;
using Microsoft.Extensions.Logging;
using SystemTools.SystemToolsShared;
using SystemTools.SystemToolsShared.Errors;
using ToolsManagement.Installer.Errors;
using ToolsManagement.Installer.ServiceInstaller;

// ReSharper disable ConvertToPrimaryConstructor

namespace ToolsManagement.Installer.ProjectManagers;

public sealed class ProjectsManagerLocal : IProjectsManager
{
    private readonly string _installFolder;
    private readonly ILogger _logger;
    private readonly IMessagesDataManager? _messagesDataManager;
    private readonly bool _useConsole;
    private readonly string? _userName;

    public ProjectsManagerLocal(ILogger logger, bool useConsole, string installFolder,
        IMessagesDataManager? messagesDataManager, string? userName)
    {
        _logger = logger;
        _useConsole = useConsole;
        _installFolder = installFolder;
        _messagesDataManager = messagesDataManager;
        _userName = userName;
    }

    public async ValueTask<Option<Err[]>> RemoveProjectAndService(string projectName, string environmentName,
        bool isService, CancellationToken cancellationToken = default)
    {
        //დავადგინოთ რა პლატფორმაზეა გაშვებული პროგრამა: ვინდოუსი თუ ლინუქსი
        InstallerBase serviceInstaller = await InstallerFactory.CreateInstaller(_logger, _useConsole,
            _messagesDataManager, _userName, cancellationToken);

        Option<Err[]> removeProjectAndServiceResult = await serviceInstaller.RemoveProjectAndService(projectName,
            environmentName, isService, _installFolder, cancellationToken);

        if (removeProjectAndServiceResult.IsNone)
        {
            return null;
        }

        if (_messagesDataManager is not null)
        {
            await _messagesDataManager.SendMessage(_userName,
                $"Service {projectName}/{environmentName} can not removed", cancellationToken);
        }

        _logger.LogError("Service {ProjectName}/{EnvironmentName} can not removed", projectName, environmentName);
        return new[] { ProjectManagersErrors.ProjectServiceCanNotRemoved(projectName, environmentName) };
    }

    public async ValueTask<Option<Err[]>> StopService(string projectName, string environmentName,
        CancellationToken cancellationToken = default)
    {
        //დავადგინოთ რა პლატფორმაზეა გაშვებული პროგრამა: ვინდოუსი თუ ლინუქსი
        InstallerBase serviceInstaller = await InstallerFactory.CreateInstaller(_logger, _useConsole,
            _messagesDataManager, _userName, cancellationToken);

        Option<Err[]> stopResult = await serviceInstaller.Stop(projectName, environmentName, cancellationToken);
        return stopResult.IsNone
            ? null
            : new[] { ProjectManagersErrors.ServiceCanNotBeStopped(projectName, environmentName) };
    }

    public async ValueTask<Option<Err[]>> StartService(string projectName, string environmentName,
        CancellationToken cancellationToken = default)
    {
        //დავადგინოთ რა პლატფორმაზეა გაშვებული პროგრამა: ვინდოუსი თუ ლინუქსი
        InstallerBase serviceInstaller = await InstallerFactory.CreateInstaller(_logger, _useConsole,
            _messagesDataManager, _userName, cancellationToken);

        Option<Err[]> stopResult = await serviceInstaller.Start(projectName, environmentName, cancellationToken);
        return stopResult.IsNone
            ? null
            : new[] { ProjectManagersErrors.ServiceCanNotBeStarted(projectName, environmentName) };
    }

    public async ValueTask<Option<Err[]>> RemoveProject(string projectName, string environmentName,
        CancellationToken cancellationToken = default)
    {
        //დავადგინოთ რა პლატფორმაზეა გაშვებული პროგრამა: ვინდოუსი თუ ლინუქსი
        InstallerBase serviceInstaller = await InstallerFactory.CreateInstaller(_logger, _useConsole,
            _messagesDataManager, _userName, cancellationToken);

        Option<Err[]> removeProjectResult =
            await serviceInstaller.RemoveProject(projectName, environmentName, _installFolder, cancellationToken);
        if (removeProjectResult.IsNone)
        {
            return null;
        }

        if (_messagesDataManager is not null)
        {
            await _messagesDataManager.SendMessage(_userName, $"Project {projectName} can not removed",
                cancellationToken);
        }

        _logger.LogError("Project {ProjectName} can not removed", projectName);
        return Err.RecreateErrors((Err[])removeProjectResult,
            ProjectManagersErrors.ProjectCanNotBeRemoved(projectName));
    }
}
