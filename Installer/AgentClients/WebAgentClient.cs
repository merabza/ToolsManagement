using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace Installer.AgentClients;

public sealed class WebAgentClient : ApiClient, IAgentClient
{
    public WebAgentClient(ILogger logger, string server, string? apiKey) : base(logger, server, apiKey)
    {
    }


    public bool RemoveProject(string projectName)
    {
        Uri uri = new(
            $"{Server}projects/remove/{projectName}{(string.IsNullOrWhiteSpace(ApiKey) ? "" : $"?apikey={ApiKey}")}");

        var response = Client.DeleteAsync(uri).Result;

        if (response.IsSuccessStatusCode)
            return true;

        LogResponseErrorMessage(response);
        return false;
    }

    public bool RemoveProjectAndService(string projectName, string serviceName)
    {
        Uri uri = new(
            $"{Server}projects/removeservice/{projectName}/{serviceName}{(string.IsNullOrWhiteSpace(ApiKey) ? "" : $"?apikey={ApiKey}")}");

        var response = Client.DeleteAsync(uri).Result;

        if (response.IsSuccessStatusCode)
            return true;

        LogResponseErrorMessage(response);
        return false;
    }

    public bool StopService(string serviceName)
    {
        return PostAsync($"projects/stop/{serviceName}{(string.IsNullOrWhiteSpace(ApiKey) ? "" : $"?apikey={ApiKey}")}")
            .Result;
    }

    public bool StartService(string serviceName)
    {
        return PostAsync(
            $"projects/start/{serviceName}{(string.IsNullOrWhiteSpace(ApiKey) ? "" : $"?apikey={ApiKey}")}").Result;
    }

    public async Task<string?> GetAppSettingsVersionByProxy(int serverSidePort, string apiVersionId)
    {
        return await GetAsyncAsString(
            $"projects/getappsettingsversion/{serverSidePort}/{apiVersionId}{(string.IsNullOrWhiteSpace(ApiKey) ? "" : $"?apikey={ApiKey}")}");
    }

    public async Task<string?> GetAppSettingsVersion()
    {
        return await GetAsyncAsString(
            $"test/getappsettingsversion{(string.IsNullOrWhiteSpace(ApiKey) ? "" : $"?apikey ={ApiKey}")}");
    }

    public async Task<string?> GetVersionByProxy(int serverSidePort, string apiVersionId)
    {
        return await GetAsyncAsString(
            $"projects/getversion/{serverSidePort}/{apiVersionId}{(string.IsNullOrWhiteSpace(ApiKey) ? "" : $"?apikey={ApiKey}")}");
    }

    //public bool CheckValidation()
    //{
    //    throw new NotImplementedException();
    //}
}