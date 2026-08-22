using SystemTools.SystemToolsShared.Errors;

namespace ToolsManagement.DatabasesManagement.Errors;

public static class SqlServerDatabaseManagerErrors
{
    public static readonly ErrorOmd HostPlatformDoesNotDetected = new()
    {
        Code = nameof(HostPlatformDoesNotDetected), Name = "Host platform does not detected"
    };

    public static readonly ErrorOmd RestoreFilesDoesNotDetected = new()
    {
        Code = nameof(RestoreFilesDoesNotDetected), Name = "Restore Files does not detected"
    };

    public static ErrorOmd CannotCreateDbClient(string? databaseName)
    {
        return new ErrorOmd
        {
            Code = nameof(CannotCreateDbClient), Name = $"Cannot create DbClient for database {databaseName}"
        };
    }
}
