using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace Installer.AgentClients;

public sealed class WebAgentClient : ApiClient, IAgentClient
{
    public WebAgentClient(ILogger logger, string server, string? apiKey, IMessagesDataManager? messagesDataManager,
        string? userName) : base(logger, server, apiKey, messagesDataManager, userName)
    {
    }

    public async Task<bool> RemoveProject(string projectName)
    {
        //+
        return await DeleteAsync(
            $"projects/remove/{projectName}{(string.IsNullOrWhiteSpace(ApiKey) ? "" : $"?apikey={ApiKey}")}");
    }

    public async Task<bool> RemoveProjectAndService(string projectName, string serviceName)
    {
        //+
        return await DeleteAsync(
            $"projects/removeservice/{projectName}/{serviceName}{(string.IsNullOrWhiteSpace(ApiKey) ? "" : $"?apikey={ApiKey}")}");
    }

    public async Task<bool> StopService(string serviceName)
    {
        //+
        return await PostAsync(
            $"projects/stop/{serviceName}{(string.IsNullOrWhiteSpace(ApiKey) ? "" : $"?apikey={ApiKey}")}");
    }

    public async Task<bool> StartService(string serviceName)
    {
        //+
        return await PostAsync(
            $"projects/start/{serviceName}{(string.IsNullOrWhiteSpace(ApiKey) ? "" : $"?apikey={ApiKey}")}");
    }

    public async Task<string?> GetAppSettingsVersionByProxy(int serverSidePort, string apiVersionId)
    {
        //+
        return await GetAsyncAsString(
            $"projects/getappsettingsversion/{serverSidePort}/{apiVersionId}{(string.IsNullOrWhiteSpace(ApiKey) ? "" : $"?apikey={ApiKey}")}");
    }

    public async Task<string?> GetAppSettingsVersion()
    {
        //+
        return await GetAsyncAsString(
            $"test/getappsettingsversion{(string.IsNullOrWhiteSpace(ApiKey) ? "" : $"?apikey ={ApiKey}")}", false);
    }

    public async Task<string?> GetVersionByProxy(int serverSidePort, string apiVersionId)
    {
        //+
        return await GetAsyncAsString(
            $"projects/getversion/{serverSidePort}/{apiVersionId}{(string.IsNullOrWhiteSpace(ApiKey) ? "" : $"?apikey={ApiKey}")}");
    }

    //public bool CheckValidation()
    //{
    //    throw new NotImplementedException();
    //}
}