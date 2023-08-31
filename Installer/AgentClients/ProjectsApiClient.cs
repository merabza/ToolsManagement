﻿using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace Installer.AgentClients;

public sealed class ProjectsApiClient : ApiClient, IProjectsApiClient
{
    public ProjectsApiClient(ILogger logger, string server, string? apiKey) : base(logger, server, apiKey)
    {
    }

    //public async Task<bool> RemoveProject(string projectName)
    //{
    //    //+
    //    return await DeleteAsync(
    //        $"projects/remove/{projectName}");
    //}

    public async Task<bool> RemoveProjectAndService(string projectName, string serviceName)
    {
        //+
        return await DeleteAsync(
            $"projects/removeservice/{projectName}/{serviceName}");
    }

    public async Task<bool> StopService(string serviceName)
    {
        //+
        return await PostAsync(
            $"projects/stop/{serviceName}");
    }

    public async Task<bool> StartService(string serviceName)
    {
        //+
        return await PostAsync(
            $"projects/start/{serviceName}");
    }
}