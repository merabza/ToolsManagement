using System.Threading;
using System.Threading.Tasks;

namespace Installer.AgentClients;

public interface IIProjectsApiClientWithFileStorage
{
    Task<bool> UpdateAppParametersFile(string projectName, string environmentName, string? serviceName,
        string appSettingsFileName, string parametersFileDateMask, string parametersFileExtension,
        CancellationToken cancellationToken);

    Task<string?> InstallProgram(string projectName, string environmentName, string programArchiveDateMask,
        string programArchiveExtension, string parametersFileDateMask, string parametersFileExtension,
        CancellationToken cancellationToken);

    Task<string?> InstallService(string projectName, string environmentName, string? serviceName,
        string serviceUserName, string appSettingsFileName, string programArchiveDateMask,
        string programArchiveExtension, string parametersFileDateMask, string parametersFileExtension,
        CancellationToken cancellationToken);
}