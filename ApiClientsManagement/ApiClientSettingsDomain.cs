namespace ApiClientsManagement;

public sealed class ApiClientSettingsDomain
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public ApiClientSettingsDomain(string server, string? apiKey, bool withMessaging)
    {
        Server = server;
        ApiKey = apiKey;
        WithMessaging = withMessaging;
    }

    public string Server { get; set; }

    public string? ApiKey { get; set; }
    public bool WithMessaging { get; set; }
}