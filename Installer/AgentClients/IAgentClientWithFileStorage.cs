namespace Installer.AgentClients;

public interface IAgentClientWithFileStorage : IApiClient
{
    bool UpdateAppParametersFile(string projectName, string? serviceName, string appSettingsFileName,
        string parametersFileDateMask, string parametersFileExtension);

    string? InstallProgram(string projectName, string programArchiveDateMask, string programArchiveExtension,
        string parametersFileDateMask, string parametersFileExtension);

    string? InstallService(string projectName, string? serviceName, string serviceUserName, string appSettingsFileName,
        string programArchiveDateMask, string programArchiveExtension, string parametersFileDateMask,
        string parametersFileExtension);
}