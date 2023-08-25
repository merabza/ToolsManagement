using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using WebAgentMessagesContracts;

namespace Installer.ServiceInstaller;

public static class InstallerFabric
{
    public static InstallerBase? CreateInstaller(ILogger logger, bool useConsole, string? dotnetRunner,
        IMessagesDataManager? messagesDataManager, string? userName)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new WindowsServiceInstaller(useConsole, logger, messagesDataManager, userName);
        if (!string.IsNullOrWhiteSpace(dotnetRunner))
            return new LinuxServiceInstaller(useConsole, logger, dotnetRunner, messagesDataManager, userName);
        logger.LogError("Installer dotnetRunner does not specified");
        return null;
    }

    public static InstallerBase CreateInstaller(ILogger logger, bool useConsole,
        IMessagesDataManager? messagesDataManager, string? userName)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new WindowsServiceInstaller(useConsole, logger, messagesDataManager, userName);
        return new LinuxServiceInstaller(useConsole, logger, messagesDataManager, userName);
    }
}