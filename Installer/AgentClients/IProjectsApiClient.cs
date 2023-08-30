using System.Threading.Tasks;

namespace Installer.AgentClients;

public interface IProjectsApiClient
{
    Task<bool> RemoveProject(string projectName);
    Task<bool> RemoveProjectAndService(string projectName, string serviceName);
    Task<bool> StopService(string serviceName);
    Task<bool> StartService(string serviceName);

}