using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace Installer.AgentClients;

public sealed class WebAgentMessageHubClient
{
    private readonly string _server;

    private readonly string? _apiKey;

    private HubConnection? _connection;

    // ReSharper disable once MemberCanBePrivate.Global
    public WebAgentMessageHubClient(string server, string? apiKey)
    {
        _server = server;
        _apiKey = apiKey;
    }

    public async Task RunMessages()
    {
        _connection = new HubConnectionBuilder()
            .WithUrl($"{_server}messages{(string.IsNullOrWhiteSpace(_apiKey) ? "" : $"?apikey={_apiKey}")}")
            .Build();

        _connection.On<string>(Events.MessageSent, message => Console.WriteLine($"[{_server}]: {message}"));

        await _connection.StartAsync();
    }

    public async Task StopMessages()
    {
        if (_connection is not null)
            await _connection.StopAsync();
    }
}