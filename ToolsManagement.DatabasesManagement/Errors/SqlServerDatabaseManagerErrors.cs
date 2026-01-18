using SystemTools.SystemToolsShared.Errors;

namespace ToolsManagement.DatabasesManagement.Errors;

public static class SqlServerDatabaseManagerErrors
{
    public static readonly Err HostPlatformDoesNotDetected = new()
    {
        ErrorCode = nameof(HostPlatformDoesNotDetected), ErrorMessage = "Host platform does not detected"
    };

    public static readonly Err RestoreFilesDoesNotDetected = new()
    {
        ErrorCode = nameof(RestoreFilesDoesNotDetected), ErrorMessage = "Restore Files does not detected"
    };

    public static Err CannotCreateDbClient(string? databaseName)
    {
        return new Err
        {
            ErrorCode = nameof(CannotCreateDbClient),
            ErrorMessage = $"Cannot create DbClient for database {databaseName}"
        };
    }
}