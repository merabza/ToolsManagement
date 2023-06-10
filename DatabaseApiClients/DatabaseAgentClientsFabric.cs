using System;
using DatabaseManagementClients;
using DbTools;
using LibApiClientParameters;
using LibDatabaseParameters;
using Microsoft.Extensions.Logging;

namespace DatabaseApiClients;

public static class DatabaseAgentClientsFabric
{
    public static DatabaseManagementClient? CreateDatabaseManagementClient(bool useConsole, ILogger logger,
        string? apiClientName, ApiClients apiClients, string? databaseConnectionName,
        DatabaseServerConnections databaseServerConnections)
    {
        if (string.IsNullOrWhiteSpace(apiClientName) && string.IsNullOrWhiteSpace(databaseConnectionName))
        {
            logger.LogError(
                "Both apiClientName and databaseConnectionName are null, cannot create DatabaseAgentClient");
            return null;
        }

        if (!string.IsNullOrWhiteSpace(apiClientName) && !string.IsNullOrWhiteSpace(databaseConnectionName))
        {
            logger.LogError(
                "Both apiClientName and databaseConnectionName are specified. must be only one of them, cannot create DatabaseAgentClient");
            return null;
        }

        if (!string.IsNullOrWhiteSpace(apiClientName))
            return CreateDatabaseManagementClient(useConsole, logger, apiClientName, apiClients);
        if (!string.IsNullOrWhiteSpace(databaseConnectionName))
            return CreateDatabaseManagementClient(useConsole, logger, databaseConnectionName,
                databaseServerConnections);
        return null;
    }


    private static DatabaseManagementClient? CreateDatabaseManagementClient(bool useConsole, ILogger logger,
        string apiClientName, ApiClients apiClients)
    {
        var apiClientSettings = apiClients.GetApiClientByKey(apiClientName);
        return DatabaseApiClient.Create(logger, useConsole, apiClientSettings);
    }

    public static DatabaseManagementClient? CreateDatabaseManagementClient(bool useConsole, ILogger logger,
        string databaseConnectionName,
        DatabaseServerConnections databaseServerConnections)
    {
        var databaseServerConnection =
            databaseServerConnections.GetDatabaseServerConnectionByKey(databaseConnectionName);

        return CreateDatabaseManagementClient(useConsole, logger, databaseServerConnection);
    }

    public static DatabaseManagementClient? CreateDatabaseManagementClient(bool useConsole, ILogger logger,
        DatabaseServerConnectionData? databaseServerConnection)
    {
        if (databaseServerConnection is null)
            throw new ArgumentOutOfRangeException();

        return databaseServerConnection.DataProvider switch
        {
            EDataProvider.None => null,
            EDataProvider.Sql => SqlServerManagementClient.Create(logger, useConsole, databaseServerConnection),
            EDataProvider.SqLite => null,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}