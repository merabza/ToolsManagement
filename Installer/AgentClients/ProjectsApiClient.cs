using System.Threading;
using System.Threading.Tasks;
using LanguageExt;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace Installer.AgentClients;

public sealed class ProjectsApiClient : ApiClient, IProjectsApiClient
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectsApiClient(ILogger logger, string server, string? apiKey, bool withMessaging) : base(logger, server,
        apiKey, null, withMessaging)
    {
    }

    public async Task<Option<Err[]>> RemoveProjectAndService(string projectName, string environmentName, bool isService,
        CancellationToken cancellationToken)
    {
        return await DeleteAsync($"projects/removeprojectservice/{projectName}/{environmentName}/{isService}",
            cancellationToken);
    }

    public async Task<Option<Err[]>> StopService(string projectName, string environmentName,
        CancellationToken cancellationToken)
    {
        return await PostAsync($"projects/stop/{projectName}/{environmentName}", cancellationToken);
    }

    public async Task<Option<Err[]>> StartService(string projectName, string environmentName,
        CancellationToken cancellationToken)
    {
        return await PostAsync($"projects/start/{projectName}/{environmentName}", cancellationToken);
    }
}