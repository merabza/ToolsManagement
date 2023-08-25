using System.Threading.Tasks;

namespace Installer.AgentClients;

public interface IAgentClient : IApiClient
{
    Task<bool> RemoveProject(string projectName);
    Task<bool> RemoveProjectAndService(string projectName, string serviceName);
    bool StopService(string serviceName);
    bool StartService(string serviceName);
}