﻿using System.Threading;
using System.Threading.Tasks;
using LanguageExt;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace Installer.AgentClients;

public sealed class ProjectsApiClient : ApiClient, IProjectsApiClient
{
    public ProjectsApiClient(ILogger logger, string server, string? apiKey) : base(logger, server, apiKey)
    {
    }

    public async Task<Option<Err[]>> RemoveProjectAndService(string projectName, string serviceName,
        string environmentName,
        CancellationToken cancellationToken)
    {
        //+
        return await DeleteAsync($"projects/removeservice/{projectName}/{serviceName}/{environmentName}",
            cancellationToken);
    }

    public async Task<Option<Err[]>> StopService(string serviceName, string environmentName,
        CancellationToken cancellationToken)
    {
        //+
        return await PostAsync($"projects/stop/{serviceName}/{environmentName}", cancellationToken);
    }

    public async Task<Option<Err[]>> StartService(string serviceName, string environmentName,
        CancellationToken cancellationToken)
    {
        //+
        return await PostAsync($"projects/start/{serviceName}/{environmentName}", cancellationToken);
    }
}