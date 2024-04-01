using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OneOf;
using SystemToolsShared;

namespace Installer.AgentClients;

public sealed class ProjectsProxyApiClient : ApiClient
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectsProxyApiClient(ILogger logger, string server, string? apiKey, bool withMessaging) : base(logger,
        server, apiKey, null, withMessaging)
    {
    }

    public async Task<OneOf<string, Err[]>> GetVersionByProxy(int serverSidePort, string apiVersionId,
        CancellationToken cancellationToken)
    {
        return await GetAsyncAsString(
            $"projects/getversion/{serverSidePort}/{apiVersionId}", cancellationToken);
    }

    public async Task<OneOf<string, Err[]>> GetAppSettingsVersionByProxy(int serverSidePort, string apiVersionId,
        CancellationToken cancellationToken)
    {
        return await GetAsyncAsString(
            $"projects/getappsettingsversion/{serverSidePort}/{apiVersionId}", cancellationToken);
    }
}