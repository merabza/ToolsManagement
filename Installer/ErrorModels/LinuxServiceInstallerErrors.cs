using SystemToolsShared;

namespace Installer.ErrorModels;

public static class LinuxServiceInstallerErrors
{

    public static Err DotnetDetectError => new()
    {
        ErrorCode = nameof(DotnetDetectError),
        ErrorMessage = "Dotnet detect Errors"
    };

    public static Err DotnetLocationIsNotFound => new()
    {
        ErrorCode = nameof(DotnetLocationIsNotFound),
        ErrorMessage = "dotnet location can not found"
    };
    
    public static Err ServiceCanNotBeEnabled(string serviceEnvName) => new()
    {
        ErrorCode = nameof(ServiceCanNotBeEnabled),
        ErrorMessage = $"Service {serviceEnvName} is not enabled"
    };

    public static Err ServiceIsNotEnabled(string serviceEnvName) => new()
    {
        ErrorCode = nameof(ServiceIsNotEnabled),
        ErrorMessage = $"Service {serviceEnvName} is not enabled"
    };

    public static Err WhichDotnetError => new()
    {
        ErrorCode = nameof(WhichDotnetError),
        ErrorMessage = "Which Dotnet finished with Errors"
    };

}

