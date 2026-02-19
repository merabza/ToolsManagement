namespace ToolsManagement.ApiClientsManagement;

public sealed class ApiClientSettingsDomain
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public ApiClientSettingsDomain(string server, string? apiKey)
    {
        Server = server;
        ApiKey = apiKey;
    }

    public string Server { get; set; }
    public string? ApiKey { get; set; }
}
