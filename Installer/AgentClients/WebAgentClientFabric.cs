using Microsoft.Extensions.Logging;

namespace Installer.AgentClients;

public sealed class WebAgentClientFabric : IApiClientsFabric
{
    public IApiClient CreateApiClient(ILogger logger, string server, string? apiKey)
    {
        return new WebAgentClient(logger, server, apiKey);
    }
}