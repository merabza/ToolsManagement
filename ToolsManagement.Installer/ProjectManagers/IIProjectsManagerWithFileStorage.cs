using System.Threading;
using System.Threading.Tasks;
using SystemTools.SharedKernel;

namespace ToolsManagement.Installer.ProjectManagers;

public interface IIProjectsManagerWithFileStorage // : IDisposable
{
    ValueTask<Result> UpdateAppParametersFile(string projectName, string environmentName,
        string appSettingsFileName, string parametersFileDateMask, string parametersFileExtension,
        CancellationToken cancellationToken = default);

    ValueTask<Result<string>> InstallProgram(string projectName, string environmentName,
        string programArchiveDateMask, string programArchiveExtension, string parametersFileDateMask,
        string parametersFileExtension, CancellationToken cancellationToken = default);

    ValueTask<Result<string>> InstallService(string projectName, string environmentName,
        string serviceUserName, string appSettingsFileName, string programArchiveDateMask,
        string programArchiveExtension, string parametersFileDateMask, string parametersFileExtension,
        string? serviceDescriptionSignature, string? projectDescription, CancellationToken cancellationToken = default);
}
