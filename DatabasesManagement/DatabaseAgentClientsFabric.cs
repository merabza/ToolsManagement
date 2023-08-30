using System;
using DbTools;
using LibApiClientParameters;
using LibDatabaseParameters;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace DatabasesManagement;

public static class DatabaseAgentClientsFabric
{
    public static DatabaseManagementClient? CreateDatabaseManagementClient(bool useConsole, ILogger logger,
        string? apiClientName, ApiClients apiClients, string? databaseConnectionName,
        DatabaseServerConnections databaseServerConnections, IMessagesDataManager? messagesDataManager,
        string? userName)
    {
        if (string.IsNullOrWhiteSpace(apiClientName) && string.IsNullOrWhiteSpace(databaseConnectionName))
        {
            messagesDataManager?.SendMessage(userName,
                "Both apiClientName and databaseConnectionName are null, cannot create DatabaseAgentClient").Wait();
            logger.LogError(
                "Both apiClientName and databaseConnectionName are null, cannot create DatabaseAgentClient");
            return null;
        }

        if (!string.IsNullOrWhiteSpace(apiClientName) && !string.IsNullOrWhiteSpace(databaseConnectionName))
        {
            messagesDataManager?.SendMessage(userName,
                    "Both apiClientName and databaseConnectionName are specified. must be only one of them, cannot create DatabaseAgentClient")
                .Wait();
            logger.LogError(
                "Both apiClientName and databaseConnectionName are specified. must be only one of them, cannot create DatabaseAgentClient");
            return null;
        }

        if (!string.IsNullOrWhiteSpace(apiClientName))
            return CreateDatabaseManagementClient(useConsole, logger, apiClientName, apiClients, messagesDataManager,
                userName);
        if (!string.IsNullOrWhiteSpace(databaseConnectionName))
            return CreateDatabaseManagementClient(useConsole, logger, databaseConnectionName, databaseServerConnections,
                messagesDataManager, userName);
        return null;
    }


    private static DatabaseManagementClient? CreateDatabaseManagementClient(bool useConsole, ILogger logger,
        string apiClientName, ApiClients apiClients, IMessagesDataManager? messagesDataManager, string? userName)
    {
        var apiClientSettings = apiClients.GetApiClientByKey(apiClientName);
        return DatabaseApiClient.Create(logger, useConsole, apiClientSettings, messagesDataManager, userName);
    }

    public static DatabaseManagementClient? CreateDatabaseManagementClient(bool useConsole, ILogger logger,
        string databaseConnectionName, DatabaseServerConnections databaseServerConnections,
        IMessagesDataManager? messagesDataManager, string? userName)
    {
        var databaseServerConnection =
            databaseServerConnections.GetDatabaseServerConnectionByKey(databaseConnectionName);

        return CreateDatabaseManagementClient(useConsole, logger, databaseServerConnection, messagesDataManager,
            userName);
    }

    public static DatabaseManagementClient? CreateDatabaseManagementClient(bool useConsole, ILogger logger,
        DatabaseServerConnectionData? databaseServerConnection, IMessagesDataManager? messagesDataManager,
        string? userName)
    {
        if (databaseServerConnection is null)
            throw new ArgumentOutOfRangeException();

        return databaseServerConnection.DataProvider switch
        {
            EDataProvider.None => null,
            EDataProvider.Sql => SqlServerManagementClient.Create(logger, useConsole, databaseServerConnection,
                messagesDataManager, userName),
            EDataProvider.SqLite => null,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}