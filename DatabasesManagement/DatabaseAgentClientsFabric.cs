using System;
using System.Threading;
using System.Threading.Tasks;
using DbTools;
using LibApiClientParameters;
using LibDatabaseParameters;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace DatabasesManagement;

public static class DatabaseAgentClientsFabric
{
    public static async Task<IDatabaseApiClient?> CreateDatabaseManagementClient(bool useConsole, ILogger logger,
        string? apiClientName, ApiClients apiClients, string? databaseConnectionName,
        DatabaseServerConnections databaseServerConnections, IMessagesDataManager? messagesDataManager,
        string? userName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(apiClientName) && string.IsNullOrWhiteSpace(databaseConnectionName))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName,
                    "Both apiClientName and databaseConnectionName are null, cannot create DatabaseAgentClient",
                    cancellationToken);
            logger.LogError(
                "Both apiClientName and databaseConnectionName are null, cannot create DatabaseAgentClient");
            return null;
        }

        if (!string.IsNullOrWhiteSpace(apiClientName) && !string.IsNullOrWhiteSpace(databaseConnectionName))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName,
                    "Both apiClientName and databaseConnectionName are specified. must be only one of them, cannot create DatabaseAgentClient",
                    cancellationToken);
            logger.LogError(
                "Both apiClientName and databaseConnectionName are specified. must be only one of them, cannot create DatabaseAgentClient");
            return null;
        }

        if (!string.IsNullOrWhiteSpace(apiClientName))
            return await CreateDatabaseManagementClient(logger, apiClientName, apiClients, messagesDataManager,
                userName, cancellationToken);
        if (!string.IsNullOrWhiteSpace(databaseConnectionName))
            return await CreateDatabaseManagementClient(useConsole, logger, databaseConnectionName,
                databaseServerConnections, messagesDataManager, userName, cancellationToken);
        return null;
    }

    private static async Task<IDatabaseApiClient?> CreateDatabaseManagementClient(ILogger logger, string apiClientName,
        ApiClients apiClients, IMessagesDataManager? messagesDataManager, string? userName,
        CancellationToken cancellationToken)
    {
        var apiClientSettings = apiClients.GetApiClientByKey(apiClientName);
        return await DatabaseApiClient.Create(logger, apiClientSettings, messagesDataManager, userName,
            cancellationToken);
    }

    //public იყენებს ApAgent
    public static async Task<IDatabaseApiClient?> CreateDatabaseManagementClient(bool useConsole, ILogger logger,
        string databaseConnectionName, DatabaseServerConnections databaseServerConnections,
        IMessagesDataManager? messagesDataManager, string? userName, CancellationToken cancellationToken)
    {
        var databaseServerConnection =
            databaseServerConnections.GetDatabaseServerConnectionByKey(databaseConnectionName);

        return await CreateDatabaseManagementClient(useConsole, logger, databaseServerConnection, messagesDataManager,
            userName, cancellationToken);
    }

    //public იყენებს supportTools
    // ReSharper disable once MemberCanBePrivate.Global
    public static async Task<IDatabaseApiClient?> CreateDatabaseManagementClient(bool useConsole, ILogger logger,
        DatabaseServerConnectionData? databaseServerConnection, IMessagesDataManager? messagesDataManager,
        string? userName, CancellationToken cancellationToken)
    {
        if (databaseServerConnection is null)
            throw new ArgumentOutOfRangeException(nameof(databaseServerConnection));

        return databaseServerConnection.DataProvider switch
        {
            EDataProvider.None => null,
            EDataProvider.Sql => await SqlServerManagementClient.Create(logger, useConsole, databaseServerConnection,
                messagesDataManager, userName, cancellationToken),
            EDataProvider.SqLite => null,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}