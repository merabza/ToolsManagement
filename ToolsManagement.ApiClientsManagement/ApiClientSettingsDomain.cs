namespace ToolsManagement.ApiClientsManagement;

public sealed class ApiClientSettingsDomain
{
    public ApiClientSettingsDomain(string server, string? apiKey)
    {
        Server = server;
        ApiKey = apiKey;
    }

    public string Server { get; set; }
    public string? ApiKey { get; set; }
}
