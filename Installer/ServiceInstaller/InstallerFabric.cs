using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace Installer.ServiceInstaller;

public static class InstallerFabric
{
    public static InstallerBase? CreateInstaller(ILogger logger, bool useConsole, string? dotnetRunner,
        IMessagesDataManager? messagesDataManager, string? userName)
    {
        messagesDataManager?.SendMessage(userName, "Creating installer").Wait();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            messagesDataManager?.SendMessage(userName, "Creating WindowsServiceInstaller").Wait();
            return new WindowsServiceInstaller(useConsole, logger, messagesDataManager, userName);
        }

        if (!string.IsNullOrWhiteSpace(dotnetRunner))
        {
            messagesDataManager?.SendMessage(userName, "Creating LinuxServiceInstaller").Wait();
            return new LinuxServiceInstaller(useConsole, logger, dotnetRunner, messagesDataManager, userName);
        }

        messagesDataManager?.SendMessage(userName, "Installer dotnetRunner does not specified").Wait();
        logger.LogError("Installer dotnetRunner does not specified");
        return null;
    }

    public static InstallerBase CreateInstaller(ILogger logger, bool useConsole,
        IMessagesDataManager? messagesDataManager, string? userName)
    {
        messagesDataManager?.SendMessage(userName, "Creating installer (without dotnetRunner)").Wait();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            messagesDataManager?.SendMessage(userName, "Creating WindowsServiceInstaller").Wait();
            return new WindowsServiceInstaller(useConsole, logger, messagesDataManager, userName);
        }

        messagesDataManager?.SendMessage(userName, "Creating LinuxServiceInstaller (without dotnetRunner)").Wait();
        return new LinuxServiceInstaller(useConsole, logger, messagesDataManager, userName);
    }
}