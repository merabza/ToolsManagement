// ReSharper disable ConvertToPrimaryConstructor

namespace ApiClientsManagement;

public sealed class ApiClientSettingsDomain
{
    public ApiClientSettingsDomain(string server, string? apiKey) //, string? remoteServerName)
    {
        Server = server;
        ApiKey = apiKey;
        //RemoteServerName = remoteServerName;
    }

    public string Server { get; set; }

    public string? ApiKey { get; set; }
    //public string? RemoteServerName { get; }
}