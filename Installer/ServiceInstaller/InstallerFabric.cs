using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace Installer.ServiceInstaller;

public static class InstallerFabric
{
    public static InstallerBase? CreateInstaller(ILogger logger, bool useConsole, string? dotnetRunner,
        IMessagesDataManager? messagesDataManager, string? userName)
    {
        messagesDataManager?.SendMessage(userName, "Creating installer", CancellationToken.None).Wait();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            messagesDataManager?.SendMessage(userName, "Creating WindowsServiceInstaller", CancellationToken.None)
                .Wait();
            return new WindowsServiceInstaller(useConsole, logger, messagesDataManager, userName);
        }

        if (!string.IsNullOrWhiteSpace(dotnetRunner))
        {
            messagesDataManager?.SendMessage(userName, "Creating LinuxServiceInstaller", CancellationToken.None).Wait();
            return new LinuxServiceInstaller(useConsole, logger, dotnetRunner, messagesDataManager, userName);
        }

        messagesDataManager?.SendMessage(userName, "Installer dotnetRunner does not specified", CancellationToken.None)
            .Wait();
        logger.LogError("Installer dotnetRunner does not specified");
        return null;
    }

    public static InstallerBase CreateInstaller(ILogger logger, bool useConsole,
        IMessagesDataManager? messagesDataManager, string? userName)
    {
        messagesDataManager?.SendMessage(userName, "Creating installer (without dotnetRunner)", CancellationToken.None)
            .Wait();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            messagesDataManager?.SendMessage(userName, "Creating WindowsServiceInstaller", CancellationToken.None)
                .Wait();
            return new WindowsServiceInstaller(useConsole, logger, messagesDataManager, userName);
        }

        messagesDataManager?.SendMessage(userName, "Creating LinuxServiceInstaller (without dotnetRunner)",
            CancellationToken.None).Wait();
        return new LinuxServiceInstaller(useConsole, logger, messagesDataManager, userName);
    }
}