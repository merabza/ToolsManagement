using System.Threading.Tasks;

namespace Installer.AgentClients;

public interface IApiClient
{
    Task<bool> CheckValidation();
}