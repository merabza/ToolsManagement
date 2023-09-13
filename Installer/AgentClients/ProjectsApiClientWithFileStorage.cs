using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SystemToolsShared;
using WebAgentProjectsApiContracts.V1.Requests;

namespace Installer.AgentClients;

public sealed class ProjectsApiClientWithFileStorage : ApiClient, IIProjectsApiClientWithFileStorage
{
    public ProjectsApiClientWithFileStorage(ILogger logger, string server, string? apiKey) : base(logger, server,
        apiKey)
    {
    }

    public async Task<bool> UpdateAppParametersFile(string projectName, string environmentName, string? serviceName,
        string appSettingsFileName, string parametersFileDateMask, string parametersFileExtension,
        CancellationToken cancellationToken)
    {
        var body = new UpdateSettingsRequest
        {
            ProjectName = projectName,
            EnvironmentName = environmentName,
            ServiceName = serviceName,
            AppSettingsFileName = appSettingsFileName,
            ParametersFileDateMask = parametersFileDateMask,
            ParametersFileExtension = parametersFileExtension
        };
        var bodyJsonData = JsonConvert.SerializeObject(body);

        return await PostAsync("projects/updatesettings", cancellationToken, bodyJsonData);
    }

    public async Task<string?> InstallProgram(string projectName, string environmentName, string programArchiveDateMask,
        string programArchiveExtension, string parametersFileDateMask, string parametersFileExtension,
        CancellationToken cancellationToken)
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

    public async Task<string?> InstallService(string projectName, string environmentName, string? serviceName,
        string serviceUserName, string appSettingsFileName, string programArchiveDateMask,
        string programArchiveExtension, string parametersFileDateMask, string parametersFileExtension,
        string? serviceDescriptionSignature, string? projectDescription, CancellationToken cancellationToken)
    {
        var body = new UpdateServiceRequest
        {
            ProjectName = projectName,
            EnvironmentName = environmentName,
            ServiceUserName = serviceUserName,
            ServiceName = serviceName,
            AppSettingsFileName = appSettingsFileName,
            ProgramArchiveDateMask = programArchiveDateMask,
            ProgramArchiveExtension = programArchiveExtension,
            ParametersFileDateMask = parametersFileDateMask,
            ParametersFileExtension = parametersFileExtension
        };

        var bodyJsonData = JsonConvert.SerializeObject(body);

        return await PostAsyncReturnString("projects/updateservice", cancellationToken, bodyJsonData);
    }
}