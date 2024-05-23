using System.Threading;
using System.Threading.Tasks;
using LanguageExt;
using OneOf;
using SystemToolsShared;
using WebAgentProjectsApiContracts;

namespace Installer.ProjectManagers;

public sealed class ProjectsManagerRemoteWithFileStorage : IIProjectsManagerWithFileStorage
{
    private readonly ProjectsApiClient _projectsApiClient;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectsManagerRemoteWithFileStorage(ProjectsApiClient projectsApiClient)
    {
        _projectsApiClient = projectsApiClient;
    }

    public async Task<Option<Err[]>> UpdateAppParametersFile(string projectName, string environmentName,
        string appSettingsFileName, string parametersFileDateMask, string parametersFileExtension,
        CancellationToken cancellationToken)
    {

        return await _projectsApiClient.UpdateAppParametersFile(projectName, environmentName, appSettingsFileName,
            parametersFileDateMask, parametersFileExtension, cancellationToken);
    }

    public async Task<OneOf<string, Err[]>> InstallProgram(string projectName, string environmentName,
        string programArchiveDateMask, string programArchiveExtension, string parametersFileDateMask,
        string parametersFileExtension, CancellationToken cancellationToken)
    {
        return await _projectsApiClient.InstallProgram(projectName, environmentName, programArchiveDateMask,
            programArchiveExtension, parametersFileDateMask, parametersFileExtension, cancellationToken);
    }

    public async Task<OneOf<string, Err[]>> InstallService(string projectName, string environmentName,
        string serviceUserName, string appSettingsFileName, string programArchiveDateMask,
        string programArchiveExtension, string parametersFileDateMask, string parametersFileExtension,
        string? serviceDescriptionSignature, string? projectDescription, CancellationToken cancellationToken)
    {
        return await _projectsApiClient.InstallService(projectName, environmentName, serviceUserName,
            appSettingsFileName, programArchiveDateMask, programArchiveExtension, parametersFileDateMask,
            parametersFileExtension, serviceDescriptionSignature, projectDescription, cancellationToken);
    }
}