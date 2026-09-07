using SystemTools.SharedKernel;

namespace ToolsManagement.Installer.Errors;

public static class LinuxServiceInstallerErrors
{
    public static Error DotnetDetectError => Error.Problem(nameof(DotnetDetectError), "Dotnet detect Errors");

    public static Error DotnetLocationIsNotFound =>
        Error.Problem(nameof(DotnetLocationIsNotFound), "dotnet location can not found");

    public static Error WhichDotnetError =>
        Error.Problem(nameof(WhichDotnetError), "Which Dotnet finished with Errors");

    public static Error ServiceCanNotBeEnabled(string serviceEnvName)
    {
        return Error.Problem(nameof(ServiceCanNotBeEnabled), $"Service {serviceEnvName} is not enabled");
    }

    public static Error ServiceIsNotEnabled(string serviceEnvName)
    {
        return Error.Problem(nameof(ServiceIsNotEnabled), $"Service {serviceEnvName} is not enabled");
    }

    public static Error ProcessCanNotBeKilled(int processId)
    {
        return Error.Problem(nameof(ProcessCanNotBeKilled), $"Process with PID {processId} can not be killed");
    }
}
