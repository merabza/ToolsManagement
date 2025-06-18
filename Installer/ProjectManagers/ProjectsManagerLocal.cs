using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Installer.Errors;
using Installer.ServiceInstaller;
using LanguageExt;
using Microsoft.Extensions.Logging;
using SystemToolsShared;
using SystemToolsShared.Errors;

// ReSharper disable ConvertToPrimaryConstructor

namespace Installer.ProjectManagers;

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

    public async ValueTask<Option<IEnumerable<Err>>> RemoveProjectAndService(string projectName, string environmentName,
        bool isService, CancellationToken cancellationToken = default)
    {
        //დავადგინოთ რა პლატფორმაზეა გაშვებული პროგრამა: ვინდოუსი თუ ლინუქსი
        var serviceInstaller = await InstallerFactory.CreateInstaller(_logger, _useConsole, _messagesDataManager,
            _userName, cancellationToken);

        var removeProjectAndServiceResult = await serviceInstaller.RemoveProjectAndService(projectName, environmentName,
            isService, _installFolder, cancellationToken);

        if (removeProjectAndServiceResult.IsNone)
            return null;

        if (_messagesDataManager is not null)
            await _messagesDataManager.SendMessage(_userName,
                $"Service {projectName}/{environmentName} can not removed", cancellationToken);
        _logger.LogError("Service {projectName}/{environmentName} can not removed", projectName, environmentName);
        return new[] { ProjectManagersErrors.ProjectServiceCanNotRemoved(projectName, environmentName) };
    }

    public async ValueTask<Option<IEnumerable<Err>>> StopService(string projectName, string environmentName,
        CancellationToken cancellationToken = default)
    {
        //დავადგინოთ რა პლატფორმაზეა გაშვებული პროგრამა: ვინდოუსი თუ ლინუქსი
        var serviceInstaller = await InstallerFactory.CreateInstaller(_logger, _useConsole, _messagesDataManager,
            _userName, cancellationToken);

        var stopResult = await serviceInstaller.Stop(projectName, environmentName, cancellationToken);
        return stopResult.IsNone
            ? null
            : new[] { ProjectManagersErrors.ServiceCanNotBeStopped(projectName, environmentName) };
    }

    public async ValueTask<Option<IEnumerable<Err>>> StartService(string projectName, string environmentName,
        CancellationToken cancellationToken = default)
    {
        //დავადგინოთ რა პლატფორმაზეა გაშვებული პროგრამა: ვინდოუსი თუ ლინუქსი
        var serviceInstaller = await InstallerFactory.CreateInstaller(_logger, _useConsole, _messagesDataManager,
            _userName, cancellationToken);

        var stopResult = await serviceInstaller.Start(projectName, environmentName, cancellationToken);
        return stopResult.IsNone
            ? null
            : new[] { ProjectManagersErrors.ServiceCanNotBeStarted(projectName, environmentName) };
    }

    public async ValueTask<Option<IEnumerable<Err>>> RemoveProject(string projectName, string environmentName,
        CancellationToken cancellationToken = default)
    {
        //დავადგინოთ რა პლატფორმაზეა გაშვებული პროგრამა: ვინდოუსი თუ ლინუქსი
        var serviceInstaller = await InstallerFactory.CreateInstaller(_logger, _useConsole, _messagesDataManager,
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
            ProjectManagersErrors.ProjectCanNotBeRemoved(projectName));
    }
}