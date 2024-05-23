using System.Threading;
using System.Threading.Tasks;
using LanguageExt;
using OneOf;
using SystemToolsShared;

namespace Installer.ProjectManagers;

public interface IIProjectsManagerWithFileStorage // : IDisposable
{
    Task<Option<Err[]>> UpdateAppParametersFile(string projectName, string environmentName, string appSettingsFileName,
        string parametersFileDateMask, string parametersFileExtension, CancellationToken cancellationToken);

    Task<OneOf<string, Err[]>> InstallProgram(string projectName, string environmentName, string programArchiveDateMask,
        string programArchiveExtension, string parametersFileDateMask, string parametersFileExtension,
        CancellationToken cancellationToken);

    Task<OneOf<string, Err[]>> InstallService(string projectName, string environmentName, string serviceUserName,
        string appSettingsFileName, string programArchiveDateMask, string programArchiveExtension,
        string parametersFileDateMask, string parametersFileExtension, string? serviceDescriptionSignature,
        string? projectDescription, CancellationToken cancellationToken);
}