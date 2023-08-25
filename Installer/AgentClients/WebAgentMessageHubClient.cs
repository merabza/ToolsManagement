using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace Installer.AgentClients;

public sealed class WebAgentMessageHubClient
{
    private readonly string _server;

    private readonly string? _apiKey;
    //private static WebAgentMessageHubClient? _instance;
    //private static readonly object SyncRoot = new();


    //private readonly HubConnection _connection;

    // ReSharper disable once MemberCanBePrivate.Global
    public WebAgentMessageHubClient(string server, string? apiKey)
    {
        _server = server;
        _apiKey = apiKey;
        //_connection = new HubConnectionBuilder()
        //    .WithUrl($"{server}messages")
        //    .Build();
    }

    //        Uri uri = new($"{Server}projects/remove/{projectName}{(string.IsNullOrWhiteSpace(ApiKey) ? "" : $"?apikey={ApiKey}")}");

    public Task RunMessages()
    {
        return Task.Run(async () =>
        {
            var connection = new HubConnectionBuilder()
                .WithUrl($"{_server}messages{(string.IsNullOrWhiteSpace(_apiKey) ? "" : $"?apikey={_apiKey}")}")
                .Build();


            connection.On<string>(Events.MessageSent, Console.WriteLine);

            await connection.StartAsync();
        });
    }

    //public static WebAgentMessageHubClient Instance
    //{
    //    get
    //    {
    //        //ეს ატრიბუტები სესიაზე არ არის დამოკიდებული და იქმნება პროგრამის გაშვებისთანავე, 
    //        //შემდგომში მასში ცვლილებები არ შედის,
    //        //მაგრამ შეიძლება პროგრამამ თავისი მუშაობის განმავლობაში რამდენჯერმე გამოიყენოს აქ არსებული ინფორმაცია
    //        if (_instance != null)
    //            return _instance;
    //        lock (SyncRoot) //thread safe singleton
    //        {
    //            _instance ??= new WebAgentMessageHubClient();
    //        }

    //        return _instance;
    //    }
    //}

    //public static void SetTestInstance(WebAgentMessageHubClient newInstance)
    //{
    //    _instance = newInstance;
    //}
}