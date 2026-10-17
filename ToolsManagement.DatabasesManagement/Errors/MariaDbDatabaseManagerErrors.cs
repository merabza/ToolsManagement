using SystemTools.SharedKernel;

namespace ToolsManagement.DatabasesManagement.Errors;

public static class MariaDbDatabaseManagerErrors
{
    public static Error ServerAddressIsEmpty =>
        Error.Problem(nameof(ServerAddressIsEmpty), "ServerAddress is empty, Cannot create MariaDbDatabaseManager");

    public static Error CannotCreateDbClient(string? databaseName)
    {
        return Error.Problem(nameof(CannotCreateDbClient),
            $"Cannot create MariaDB DbClient for database {databaseName}");
    }

    public static Error OperationIsNotSupported(string operationName)
    {
        return Error.Problem(nameof(OperationIsNotSupported), $"{operationName} is not supported for MariaDB");
    }
}
