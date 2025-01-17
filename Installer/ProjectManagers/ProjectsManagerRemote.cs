using LanguageExt;
using System.Collections.Generic;
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

    public ValueTask<Option<IEnumerable<Err>>> RemoveProjectAndService(string projectName, string environmentName,
        bool isService, CancellationToken cancellationToken = default)
    {
        return _projectsApiClient.RemoveProjectAndService(projectName, environmentName, isService, cancellationToken);
    }

    public ValueTask<Option<IEnumerable<Err>>> StopService(string projectName, string environmentName,
        CancellationToken cancellationToken = default)
    {
        return _projectsApiClient.StopService(projectName, environmentName, cancellationToken);
    }

    public ValueTask<Option<IEnumerable<Err>>> StartService(string projectName, string environmentName,
        CancellationToken cancellationToken = default)
    {
        return _projectsApiClient.StartService(projectName, environmentName, cancellationToken);
    }
}