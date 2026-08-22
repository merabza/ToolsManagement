using SystemTools.SystemToolsShared.Errors;

namespace ToolsManagement.Installer.Errors;

public static class LinuxServiceInstallerErrors
{
    public static ErrorOmd DotnetDetectError =>
        new() { Code = nameof(DotnetDetectError), Name = "Dotnet detect Errors" };

    public static ErrorOmd DotnetLocationIsNotFound =>
        new() { Code = nameof(DotnetLocationIsNotFound), Name = "dotnet location can not found" };

    public static ErrorOmd WhichDotnetError =>
        new() { Code = nameof(WhichDotnetError), Name = "Which Dotnet finished with Errors" };

    public static ErrorOmd ServiceCanNotBeEnabled(string serviceEnvName)
    {
        return new ErrorOmd
        {
            Code = nameof(ServiceCanNotBeEnabled), Name = $"Service {serviceEnvName} is not enabled"
        };
    }

    public static ErrorOmd ServiceIsNotEnabled(string serviceEnvName)
    {
        return new ErrorOmd { Code = nameof(ServiceIsNotEnabled), Name = $"Service {serviceEnvName} is not enabled" };
    }

    public static ErrorOmd ProcessCanNotBeKilled(int processId)
    {
        return new ErrorOmd
        {
            Code = nameof(ProcessCanNotBeKilled), Name = $"Process with PID {processId} can not be killed"
        };
    }
}
