using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OneOf;
using SystemToolsShared;

namespace Installer.AgentClients;

public class TestApiClient : ApiClient
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public TestApiClient(ILogger logger, string server) : base(logger, server, null)
    {
    }

    public async Task<OneOf<string, Err[]>> GetAppSettingsVersion(CancellationToken cancellationToken)
    {
        return await GetAsyncAsString("test/getappsettingsversion", cancellationToken, false);
    }

    public async Task<OneOf<string, Err[]>> GetVersion(CancellationToken cancellationToken)
    {
        return await GetAsyncAsString("test/getversion", cancellationToken, false);
    }
}