using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Installer.ServiceInstaller;

public static class InstallerFabric
{
    public static InstallerBase? CreateInstaller(ILogger logger, bool useConsole, string? dotnetRunner)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new WindowsServiceInstaller(useConsole, logger);
        if (!string.IsNullOrWhiteSpace(dotnetRunner))
            return new LinuxServiceInstaller(useConsole, logger, dotnetRunner);
        logger.LogError("Installer dotnetRunner does not specified");
        return null;
    }

    public static InstallerBase CreateInstaller(ILogger logger, bool useConsole)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new WindowsServiceInstaller(useConsole, logger);
        return new LinuxServiceInstaller(useConsole, logger);
    }
}