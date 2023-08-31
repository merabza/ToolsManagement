using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace Installer.AgentClients;

public sealed class ProjectsProxyApiClient : ApiClient
{
    public ProjectsProxyApiClient(ILogger logger, string server, string? apiKey) : base(logger, server, apiKey)
    {
    }

    public async Task<string?> GetVersionByProxy(int serverSidePort, string apiVersionId)
    {
        //+
        return await GetAsyncAsString(
            $"projects/getversion/{serverSidePort}/{apiVersionId}");
    }

    public async Task<string?> GetAppSettingsVersionByProxy(int serverSidePort, string apiVersionId)
    {
        //+
        return await GetAsyncAsString(
            $"projects/getappsettingsversion/{serverSidePort}/{apiVersionId}");
    }
}