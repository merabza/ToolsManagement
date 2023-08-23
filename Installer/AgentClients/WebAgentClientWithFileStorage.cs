using System;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SystemToolsShared;
using WebAgentProjectsApiContracts.V1.Requests;

namespace Installer.AgentClients;

public sealed class WebAgentClientWithFileStorage : ApiClient, IAgentClientWithFileStorage
{
    public WebAgentClientWithFileStorage(ILogger logger, string server, string? apiKey) : base(logger, server, apiKey)
    {
    }

    public bool UpdateAppParametersFile(string projectName, string environmentName, string? serviceName,
        string appSettingsFileName, string parametersFileDateMask, string parametersFileExtension)
    {
        var uri = new Uri(
            $"{Server}projects/updatesettings{(string.IsNullOrWhiteSpace(ApiKey) ? "" : $"?apikey={ApiKey}")}");

        var body = new UpdateSettingsRequest
        {
            ProjectName = projectName,
            EnvironmentName = environmentName,
            ServiceName = serviceName,
            AppSettingsFileName = appSettingsFileName,
            ParametersFileDateMask = parametersFileDateMask,
            ParametersFileExtension = parametersFileExtension
        };

        var bodyApiKeyJsonData = JsonConvert.SerializeObject(body);

        var response = Client
            .PostAsync(uri, new StringContent(bodyApiKeyJsonData, Encoding.UTF8, "application/json")).Result;

        if (response.IsSuccessStatusCode)
            return true;

        LogResponseErrorMessage(response);
        return false;
    }

    public string? InstallProgram(string projectName, string environmentName, string programArchiveDateMask,
        string programArchiveExtension, string parametersFileDateMask, string parametersFileExtension)
    {
        var uri = new Uri($"{Server}projects/update");
        var body = new ProjectUpdateRequest
        {
            ProjectName = projectName,
            EnvironmentName = environmentName,
            ProgramArchiveDateMask = programArchiveDateMask,
            ProgramArchiveExtension = programArchiveExtension,
            ParametersFileDateMask = parametersFileDateMask,
            ParametersFileExtension = parametersFileExtension
        };

        var bodyApiKeyJsonData = JsonConvert.SerializeObject(body);

        var response = Client
            .PostAsync(uri, new StringContent(bodyApiKeyJsonData, Encoding.UTF8, "application/json")).Result;

        if (response.IsSuccessStatusCode)
            return response.Content.ReadAsStringAsync().Result;

        LogResponseErrorMessage(response);
        return null;
    }

    public string? InstallService(string projectName, string environmentName, string? serviceName,
        string serviceUserName, string appSettingsFileName, string programArchiveDateMask,
        string programArchiveExtension, string parametersFileDateMask, string parametersFileExtension)
    {
        var uri = new Uri(
            $"{Server}projects/updateservice{(string.IsNullOrWhiteSpace(ApiKey) ? "" : $"?apikey={ApiKey}")}");
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

        var bodyApiKeyJsonData = JsonConvert.SerializeObject(body);

        var response = Client
            .PostAsync(uri, new StringContent(bodyApiKeyJsonData, Encoding.UTF8, "application/json")).Result;

        if (response.IsSuccessStatusCode)
            return response.Content.ReadAsStringAsync().Result;

        LogResponseErrorMessage(response);
        return null;
    }
}