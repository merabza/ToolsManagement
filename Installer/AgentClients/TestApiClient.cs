using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace Installer.AgentClients;

public class TestApiClient : ApiClient
{
    public TestApiClient(ILogger logger, string server) : base(logger, server, null, null, null)
    {
    }

    public async Task<string?> GetAppSettingsVersion()
    {
        return await GetAsyncAsString(
            $"test/getappsettingsversion", false);
    }

    public async Task<string?> GetVersion(bool useConsole = false)
    {
        try
        {
            return await GetAsyncAsString("test/getversion");
        }
        catch (Exception e)
        {
            StShared.WriteErrorLine(e.Message, useConsole);
            return null;
        }
    }
}