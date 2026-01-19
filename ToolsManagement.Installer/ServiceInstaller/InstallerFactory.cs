using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SystemTools.SystemToolsShared;

namespace ToolsManagement.Installer.ServiceInstaller;

public static class InstallerFactory
{
    public static async ValueTask<InstallerBase?> CreateInstaller(ILogger logger, bool useConsole, string? dotnetRunner,
        IMessagesDataManager? messagesDataManager, string? userName, CancellationToken cancellationToken = default)
    {
        if (messagesDataManager is not null)
            await messagesDataManager.SendMessage(userName, "Creating installer", cancellationToken);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName, "Creating WindowsServiceInstaller", cancellationToken);
            return new WindowsServiceInstaller(useConsole, logger, messagesDataManager, userName);
        }

        if (!string.IsNullOrWhiteSpace(dotnetRunner))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName, "Creating LinuxServiceInstaller", cancellationToken);
            return new LinuxServiceInstaller(useConsole, logger, dotnetRunner, messagesDataManager, userName);
        }

        if (messagesDataManager is not null)
            await messagesDataManager.SendMessage(userName, "Installer dotnetRunner does not specified",
                cancellationToken);
        logger.LogError("Installer dotnetRunner does not specified");
        return null;
    }

    public static async ValueTask<InstallerBase> CreateInstaller(ILogger logger, bool useConsole,
        IMessagesDataManager? messagesDataManager, string? userName, CancellationToken cancellationToken = default)
    {
        if (messagesDataManager is not null)
            await messagesDataManager.SendMessage(userName, "Creating installer (without dotnetRunner)",
                cancellationToken);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName, "Creating WindowsServiceInstaller", cancellationToken);
            return new WindowsServiceInstaller(useConsole, logger, messagesDataManager, userName);
        }

        if (messagesDataManager is not null)
            await messagesDataManager.SendMessage(userName, "Creating LinuxServiceInstaller (without dotnetRunner)",
                cancellationToken);
        return new LinuxServiceInstaller(useConsole, logger, messagesDataManager, userName);
    }
}