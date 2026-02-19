using System.Threading;
using System.Threading.Tasks;
using LanguageExt;
using OneOf;
using SystemTools.SystemToolsShared.Errors;
using WebAgentContracts.WebAgentProjectsApiContracts;

namespace ToolsManagement.Installer.ProjectManagers;

public sealed class ProjectsManagerRemoteWithFileStorage : IIProjectsManagerWithFileStorage
{
    private readonly ProjectsApiClient _projectsApiClient;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectsManagerRemoteWithFileStorage(ProjectsApiClient projectsApiClient)
    {
        _projectsApiClient = projectsApiClient;
    }

    public ValueTask<Option<Err[]>> UpdateAppParametersFile(string projectName, string environmentName,
        string appSettingsFileName, string parametersFileDateMask, string parametersFileExtension,
        CancellationToken cancellationToken = default)
    {
        return _projectsApiClient.UpdateAppParametersFile(projectName, environmentName, appSettingsFileName,
            parametersFileDateMask, parametersFileExtension, cancellationToken);
    }

    public ValueTask<OneOf<string, Err[]>> InstallProgram(string projectName, string environmentName,
        string programArchiveDateMask, string programArchiveExtension, string parametersFileDateMask,
        string parametersFileExtension, CancellationToken cancellationToken = default)
    {
        return _projectsApiClient.InstallProgram(projectName, environmentName, programArchiveDateMask,
            programArchiveExtension, parametersFileDateMask, parametersFileExtension, cancellationToken);
    }

    public ValueTask<OneOf<string, Err[]>> InstallService(string projectName, string environmentName,
        string serviceUserName, string appSettingsFileName, string programArchiveDateMask,
        string programArchiveExtension, string parametersFileDateMask, string parametersFileExtension,
        string? serviceDescriptionSignature, string? projectDescription, CancellationToken cancellationToken = default)
    {
        return _projectsApiClient.InstallService(projectName, environmentName, serviceUserName, appSettingsFileName,
            programArchiveDateMask, programArchiveExtension, parametersFileDateMask, parametersFileExtension,
            serviceDescriptionSignature, projectDescription, cancellationToken);
    }
}
