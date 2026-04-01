using SystemTools.SystemToolsShared.Errors;

namespace ToolsManagement.Installer.Errors;

public static class LinuxServiceInstallerErrors
{
    public static Error DotnetDetectError =>
        new() { Code = nameof(DotnetDetectError), Name = "Dotnet detect Errors" };

    public static Error DotnetLocationIsNotFound =>
        new() { Code = nameof(DotnetLocationIsNotFound), Name = "dotnet location can not found" };

    public static Error WhichDotnetError =>
        new() { Code = nameof(WhichDotnetError), Name = "Which Dotnet finished with Errors" };

    public static Error ServiceCanNotBeEnabled(string serviceEnvName)
    {
        return new Error
        {
            Code = nameof(ServiceCanNotBeEnabled), Name = $"Service {serviceEnvName} is not enabled"
        };
    }

    public static Error ServiceIsNotEnabled(string serviceEnvName)
    {
        return new Error
        {
            Code = nameof(ServiceIsNotEnabled), Name = $"Service {serviceEnvName} is not enabled"
        };
    }
}
