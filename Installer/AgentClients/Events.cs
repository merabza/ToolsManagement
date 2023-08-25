using WebAgentMessagesContracts;

namespace Installer.AgentClients;

public static class Events
{
    public static string MessageSent => nameof(IMessenger.SendMessage);
}