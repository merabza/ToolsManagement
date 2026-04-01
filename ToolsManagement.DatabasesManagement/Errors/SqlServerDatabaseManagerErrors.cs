using SystemTools.SystemToolsShared.Errors;

namespace ToolsManagement.DatabasesManagement.Errors;

public static class SqlServerDatabaseManagerErrors
{
    public static readonly Error HostPlatformDoesNotDetected = new()
    {
        Code = nameof(HostPlatformDoesNotDetected), Name = "Host platform does not detected"
    };

    public static readonly Error RestoreFilesDoesNotDetected = new()
    {
        Code = nameof(RestoreFilesDoesNotDetected), Name = "Restore Files does not detected"
    };

    public static Error CannotCreateDbClient(string? databaseName)
    {
        return new Error
        {
            Code = nameof(CannotCreateDbClient), Name = $"Cannot create DbClient for database {databaseName}"
        };
    }
}
