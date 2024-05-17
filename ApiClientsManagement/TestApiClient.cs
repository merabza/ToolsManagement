using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OneOf;
using SystemToolsShared;

namespace ApiClientsManagement;

public class TestApiClient : ApiClient
{
    //public const string ApiName = "TestApi";

    // ReSharper disable once ConvertToPrimaryConstructor
    public TestApiClient(ILogger logger, IHttpClientFactory httpClientFactory, string server) : base(logger,
        httpClientFactory, server, null, null, false)
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