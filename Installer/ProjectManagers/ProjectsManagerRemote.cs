using LanguageExt;
using System.Threading;
using System.Threading.Tasks;
using SystemToolsShared.Errors;
using WebAgentProjectsApiContracts;

namespace Installer.ProjectManagers;

public sealed class ProjectsManagerRemote : IProjectsManager
{
    private readonly ProjectsApiClient _projectsApiClient;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectsManagerRemote(ProjectsApiClient projectsApiClient)
    {
        _projectsApiClient = projectsApiClient;
    }

    public async Task<Option<Err[]>> RemoveProjectAndService(string projectName, string environmentName, bool isService,
        CancellationToken cancellationToken)
    {
        return await _projectsApiClient.RemoveProjectAndService(projectName, environmentName, isService,
            cancellationToken);
    }

    public async Task<Option<Err[]>> StopService(string projectName, string environmentName,
        CancellationToken cancellationToken)
    {
        return await _projectsApiClient.StopService(projectName, environmentName, cancellationToken);
    }

    public async Task<Option<Err[]>> StartService(string projectName, string environmentName,
        CancellationToken cancellationToken)
    {
        return await _projectsApiClient.StartService(projectName, environmentName, cancellationToken);
    }
}