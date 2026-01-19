using System.Threading;
using System.Threading.Tasks;
using LanguageExt;
using SystemTools.SystemToolsShared.Errors;
using WebAgentContracts.WebAgentProjectsApiContracts;

namespace ToolsManagement.Installer.ProjectManagers;

public sealed class ProjectsManagerRemote : IProjectsManager
{
    private readonly ProjectsApiClient _projectsApiClient;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectsManagerRemote(ProjectsApiClient projectsApiClient)
    {
        _projectsApiClient = projectsApiClient;
    }

    public ValueTask<Option<Err[]>> RemoveProjectAndService(string projectName, string environmentName, bool isService,
        CancellationToken cancellationToken = default)
    {
        return _projectsApiClient.RemoveProjectAndService(projectName, environmentName, isService, cancellationToken);
    }

    public ValueTask<Option<Err[]>> StopService(string projectName, string environmentName,
        CancellationToken cancellationToken = default)
    {
        return _projectsApiClient.StopService(projectName, environmentName, cancellationToken);
    }

    public ValueTask<Option<Err[]>> StartService(string projectName, string environmentName,
        CancellationToken cancellationToken = default)
    {
        return _projectsApiClient.StartService(projectName, environmentName, cancellationToken);
    }
}