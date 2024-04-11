using System.Threading;
using System.Threading.Tasks;
using LanguageExt;
using OneOf;
using SystemToolsShared;

namespace Installer.AgentClients;

public interface IIProjectsApiClientWithFileStorage// : IDisposable
{
    Task<Option<Err[]>> UpdateAppParametersFile(string projectName, string environmentName, string? serviceName,
        string appSettingsFileName, string parametersFileDateMask, string parametersFileExtension,
        CancellationToken cancellationToken);

    Task<OneOf<string, Err[]>> InstallProgram(string projectName, string environmentName, string programArchiveDateMask,
        string programArchiveExtension, string parametersFileDateMask, string parametersFileExtension,
        CancellationToken cancellationToken);

    Task<OneOf<string, Err[]>> InstallService(string projectName, string environmentName, string? serviceName,
        string serviceUserName, string appSettingsFileName, string programArchiveDateMask,
        string programArchiveExtension, string parametersFileDateMask, string parametersFileExtension,
        string? serviceDescriptionSignature, string? projectDescription, CancellationToken cancellationToken);
}