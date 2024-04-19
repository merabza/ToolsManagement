using System.Threading;
using System.Threading.Tasks;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OneOf;
using SystemToolsShared;
using WebAgentProjectsApiContracts.V1.Requests;

namespace Installer.AgentClients;

public sealed class ProjectsApiClientWithFileStorage : ApiClient, IIProjectsApiClientWithFileStorage
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectsApiClientWithFileStorage(ILogger logger, string server, string? apiKey, bool withMessaging) : base(
        logger, server, apiKey, null, withMessaging)
    {
    }

    public async Task<Option<Err[]>> UpdateAppParametersFile(string projectName, string environmentName,
        string appSettingsFileName, string parametersFileDateMask, string parametersFileExtension,
        CancellationToken cancellationToken)
    {
        var body = new UpdateSettingsRequest
        {
            ProjectName = projectName,
            EnvironmentName = environmentName,
            AppSettingsFileName = appSettingsFileName,
            ParametersFileDateMask = parametersFileDateMask,
            ParametersFileExtension = parametersFileExtension
        };
        var bodyJsonData = JsonConvert.SerializeObject(body);

        return await PostAsync("projects/updatesettings", cancellationToken, bodyJsonData);
    }

    public async Task<OneOf<string, Err[]>> InstallProgram(string projectName, string environmentName,
        string programArchiveDateMask, string programArchiveExtension, string parametersFileDateMask,
        string parametersFileExtension, CancellationToken cancellationToken)
    {
        var body = new ProjectUpdateRequest
        {
            ProjectName = projectName,
            EnvironmentName = environmentName,
            ProgramArchiveDateMask = programArchiveDateMask,
            ProgramArchiveExtension = programArchiveExtension,
            ParametersFileDateMask = parametersFileDateMask,
            ParametersFileExtension = parametersFileExtension
        };

        var bodyJsonData = JsonConvert.SerializeObject(body);

        return await PostAsyncReturnString("projects/update", cancellationToken, bodyJsonData);
    }

    public async Task<OneOf<string, Err[]>> InstallService(string projectName, string environmentName,
        string serviceUserName, string appSettingsFileName, string programArchiveDateMask,
        string programArchiveExtension, string parametersFileDateMask, string parametersFileExtension,
        string? serviceDescriptionSignature, string? projectDescription, CancellationToken cancellationToken)
    {
        var body = new UpdateServiceRequest
        {
            ProjectName = projectName,
            EnvironmentName = environmentName,
            ServiceUserName = serviceUserName,
            AppSettingsFileName = appSettingsFileName,
            ProgramArchiveDateMask = programArchiveDateMask,
            ProgramArchiveExtension = programArchiveExtension,
            ParametersFileDateMask = parametersFileDateMask,
            ParametersFileExtension = parametersFileExtension,
            ServiceDescriptionSignature = serviceDescriptionSignature,
            ProjectDescription = projectDescription
        };

        var bodyJsonData = JsonConvert.SerializeObject(body);

        return await PostAsyncReturnString("projects/updateservice", cancellationToken, bodyJsonData);
    }
}