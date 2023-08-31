using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace Installer.AgentClients;

public sealed class ProjectsApiClient : ApiClient, IProjectsApiClient
{
    public ProjectsApiClient(ILogger logger, string server, string? apiKey) : base(logger, server, apiKey)
    {
    }

    //public async Task<bool> RemoveProject(string projectName, string environmentName)
    //{
    //    //+
    //    return await DeleteAsync(
    //        $"projects/remove/{projectName}/{environmentName}");
    //}

    public async Task<bool> RemoveProjectAndService(string projectName, string serviceName, string environmentName)
    {
        //+
        return await DeleteAsync(
            $"projects/removeservice/{projectName}/{serviceName}/{environmentName}");
    }

    public async Task<bool> StopService(string serviceName, string environmentName)
    {
        //+
        return await PostAsync(
            $"projects/stop/{serviceName}/{environmentName}");
    }

    public async Task<bool> StartService(string serviceName, string environmentName)
    {
        //+
        return await PostAsync(
            $"projects/start/{serviceName}/{environmentName}");
    }
}