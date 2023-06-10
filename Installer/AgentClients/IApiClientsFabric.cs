using Microsoft.Extensions.Logging;

namespace Installer.AgentClients;

public interface IApiClientsFabric
{
    IApiClient CreateApiClient(ILogger logger, string server, string? apiKey);
}