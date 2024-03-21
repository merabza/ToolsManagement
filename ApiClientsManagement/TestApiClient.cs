using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OneOf;
using SystemToolsShared;

namespace ApiClientsManagement;

public class TestApiClient : ApiClient
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public TestApiClient(ILogger logger, string server) : base(logger, server, null, false)
    {
    }

    public async Task<OneOf<string, Err[]>> GetAppSettingsVersion(CancellationToken cancellationToken)
    {
        return await GetAsyncAsString("test/getappsettingsversion", cancellationToken);
    }

    public async Task<OneOf<string, Err[]>> GetVersion(CancellationToken cancellationToken)
    {
        return await GetAsyncAsString("test/getversion", cancellationToken);
    }
}