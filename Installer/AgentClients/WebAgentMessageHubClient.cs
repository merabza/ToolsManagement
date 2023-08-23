using Microsoft.AspNetCore.SignalR.Client;

namespace Installer.AgentClients;

public sealed class WebAgentMessageHubClient
{
    //private static WebAgentMessageHubClient? _instance;
    //private static readonly object SyncRoot = new();


    private readonly HubConnection _connection;

    // ReSharper disable once MemberCanBePrivate.Global
    public WebAgentMessageHubClient(string server)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl($"{server}messages")
            .Build();
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