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
            $"test/getappsettingsversion{(string.IsNullOrWhiteSpace(ApiKey) ? "" : $"?apikey ={ApiKey}")}", false);
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

    public async Task<bool> CheckValidation()
    {
        Console.WriteLine("Try connect to Web Agent...");

        var version = await GetVersion();

        if (string.IsNullOrWhiteSpace(version))
            return false;

        Console.WriteLine($"Connected successfully, Web Agent version is {version}");

        return true;
    }
}