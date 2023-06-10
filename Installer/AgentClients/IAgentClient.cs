namespace Installer.AgentClients;

public interface IAgentClient : IApiClient
{
    bool RemoveProject(string projectName);
    bool RemoveProjectAndService(string projectName, string serviceName);
    bool StopService(string serviceName);
    bool StartService(string serviceName);
}