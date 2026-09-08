using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;
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

    public async ValueTask<Result> RemoveProjectAndService(string projectName, string environmentName, bool isService,
        CancellationToken cancellationToken = default)
    {
        //დავადგინოთ რა პლატფორმაზეა გაშვებული პროგრამა: ვინდოუსი თუ ლინუქსი
        InstallerBase serviceInstaller = await InstallerFactory.CreateInstaller(_logger, _useConsole,
            _messagesDataManager, _userName, cancellationToken);

        Result removeProjectAndServiceResult = await serviceInstaller.RemoveProjectAndService(projectName,
            environmentName, isService, _installFolder, cancellationToken);

        if (removeProjectAndServiceResult.IsSuccess)
        {
            return Result.Success();
        }

        if (_messagesDataManager is not null)
        {
            await _messagesDataManager.SendMessage(_userName,
                $"Service {projectName}/{environmentName} can not removed", cancellationToken);
        }

        _logger.LogError("Service {ProjectName}/{EnvironmentName} can not removed", projectName, environmentName);
        return ProjectManagersErrors.ProjectServiceCanNotRemoved(projectName, environmentName);
    }

    public async ValueTask<Result> StopService(string projectName, string environmentName,
        CancellationToken cancellationToken = default)
    {
        //დავადგინოთ რა პლატფორმაზეა გაშვებული პროგრამა: ვინდოუსი თუ ლინუქსი
        InstallerBase serviceInstaller = await InstallerFactory.CreateInstaller(_logger, _useConsole,
            _messagesDataManager, _userName, cancellationToken);

        Result stopResult = await serviceInstaller.Stop(projectName, environmentName, cancellationToken);
        return stopResult.IsSuccess
            ? Result.Success()
            : Result.Failure(ProjectManagersErrors.ServiceCanNotBeStopped(projectName, environmentName));
    }

    public async ValueTask<Result> StartService(string projectName, string environmentName,
        CancellationToken cancellationToken = default)
    {
        //დავადგინოთ რა პლატფორმაზეა გაშვებული პროგრამა: ვინდოუსი თუ ლინუქსი
        InstallerBase serviceInstaller = await InstallerFactory.CreateInstaller(_logger, _useConsole,
            _messagesDataManager, _userName, cancellationToken);

        Result startResult = await serviceInstaller.Start(projectName, environmentName, cancellationToken);
        return startResult.IsSuccess
            ? Result.Success()
            : Result.Failure(ProjectManagersErrors.ServiceCanNotBeStarted(projectName, environmentName));
    }

    public async ValueTask<Result> RemoveProject(string projectName, string environmentName,
        CancellationToken cancellationToken = default)
    {
        //დავადგინოთ რა პლატფორმაზეა გაშვებული პროგრამა: ვინდოუსი თუ ლინუქსი
        InstallerBase serviceInstaller = await InstallerFactory.CreateInstaller(_logger, _useConsole,
            _messagesDataManager, _userName, cancellationToken);

        Result removeProjectResult =
            await serviceInstaller.RemoveProject(projectName, environmentName, _installFolder, cancellationToken);
        if (removeProjectResult.IsSuccess)
        {
            return Result.Success();
        }

        if (_messagesDataManager is not null)
        {
            await _messagesDataManager.SendMessage(_userName, $"Project {projectName} can not removed",
                cancellationToken);
        }

        _logger.LogError("Project {ProjectName} can not removed", projectName);
        return Result.CreateValidationError([
            .. removeProjectResult.Error.ToErrorArray(), ProjectManagersErrors.ProjectCanNotBeRemoved(projectName)
        ]);
    }
}
