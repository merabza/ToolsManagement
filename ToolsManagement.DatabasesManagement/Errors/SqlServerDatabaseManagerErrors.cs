using SystemTools.SharedKernel;

namespace ToolsManagement.DatabasesManagement.Errors;

public static class SqlServerDatabaseManagerErrors
{
    public static readonly Error HostPlatformDoesNotDetected =
        Error.Problem(nameof(HostPlatformDoesNotDetected), "Host platform does not detected");

    public static Error RestoreFilesDoesNotDetected =>
        Error.Problem(nameof(RestoreFilesDoesNotDetected), "Restore Files does not detected");

    public static Error CannotCreateDbClient(string? databaseName)
    {
        return Error.Problem(nameof(CannotCreateDbClient), $"Cannot create DbClient for database {databaseName}");
    }
}
