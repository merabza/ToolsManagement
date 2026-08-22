using System.Threading;
using System.Threading.Tasks;
using LanguageExt;
using OneOf;
using SystemTools.SystemToolsShared.Errors;

namespace ToolsManagement.Installer.ProjectManagers;

public interface IIProjectsManagerWithFileStorage // : IDisposable
{
    ValueTask<Option<ErrorOmd[]>> UpdateAppParametersFile(string projectName, string environmentName,
        string appSettingsFileName, string parametersFileDateMask, string parametersFileExtension,
        CancellationToken cancellationToken = default);

    ValueTask<OneOf<string, ErrorOmd[]>> InstallProgram(string projectName, string environmentName,
        string programArchiveDateMask, string programArchiveExtension, string parametersFileDateMask,
        string parametersFileExtension, CancellationToken cancellationToken = default);

    ValueTask<OneOf<string, ErrorOmd[]>> InstallService(string projectName, string environmentName,
        string serviceUserName, string appSettingsFileName, string programArchiveDateMask,
        string programArchiveExtension, string parametersFileDateMask, string parametersFileExtension,
        string? serviceDescriptionSignature, string? projectDescription, CancellationToken cancellationToken = default);
}
