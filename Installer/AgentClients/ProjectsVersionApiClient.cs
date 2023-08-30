using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace Installer.AgentClients;

public sealed class ProjectsVersionApiClient : ApiClient
{
    public ProjectsVersionApiClient(ILogger logger, string server, string? apiKey, IMessagesDataManager? messagesDataManager,
        string? userName) : base(logger, server, apiKey, messagesDataManager, userName)
    {
    }

    public async Task<string?> GetVersionByProxy(int serverSidePort, string apiVersionId)
    {
        //+
        return await GetAsyncAsString(
            $"projects/getversion/{serverSidePort}/{apiVersionId}{(string.IsNullOrWhiteSpace(ApiKey) ? "" : $"?apikey={ApiKey}")}");
    }

}