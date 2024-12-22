using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ApiClientsManagement;
using DbTools;
using LibApiClientParameters;
using LibDatabaseParameters;
using Microsoft.Extensions.Logging;
using SystemToolsShared;
using WebAgentDatabasesApiContracts;

namespace DatabasesManagement;

public static class DatabaseAgentClientsFabric
{
    public static async Task<IDatabaseManager?> CreateDatabaseManager(bool useConsole, ILogger logger,
        IHttpClientFactory httpClientFactory, string? apiClientName, ApiClients apiClients,
        string? databaseConnectionName, DatabaseServerConnections databaseServerConnections,
        IMessagesDataManager? messagesDataManager, string? userName, CancellationToken cancellationToken = default)
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
            return await CreateDatabaseManager(logger, httpClientFactory, apiClientName, apiClients,
                messagesDataManager, userName, useConsole, cancellationToken);
        if (!string.IsNullOrWhiteSpace(databaseConnectionName))
            return await CreateDatabaseManager(useConsole, logger, databaseConnectionName,
                databaseServerConnections, messagesDataManager, userName, cancellationToken);
        return null;
    }

    private static async Task<IDatabaseManager?> CreateDatabaseManager(ILogger logger,
        IHttpClientFactory httpClientFactory, string apiClientName, ApiClients apiClients,
        IMessagesDataManager? messagesDataManager, string? userName, bool useConsole,
        CancellationToken cancellationToken = default)
    {
        var apiClientSettings = apiClients.GetApiClientByKey(apiClientName);

        if (apiClientSettings is null)
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName, "DatabaseApiClient settings is null",
                    cancellationToken);
            logger.LogError("cannot create DatabaseApiClient");
            return null;
        }

        if (string.IsNullOrWhiteSpace(apiClientSettings.Server))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName, "Server name is empty, cannot create DatabaseApiClient",
                    cancellationToken);
            logger.LogError("cannot create DatabaseApiClient");
            return null;
        }


        var databaseApiClient = new DatabaseApiClient(logger, httpClientFactory, apiClientSettings.Server,
            apiClientSettings.ApiKey, useConsole);

        return new RemoteDatabaseManager(logger, databaseApiClient);

        //return await DatabaseApiClient.Create(logger, httpClientFactory, apiClientSettings, messagesDataManager,
        //    userName, cancellationToken);
    }

    //public იყენებს ApAgent
    // ReSharper disable once MemberCanBePrivate.Global
    public static async Task<IDatabaseManager?> CreateDatabaseManager(bool useConsole, ILogger logger,
        string databaseConnectionName, DatabaseServerConnections databaseServerConnections,
        IMessagesDataManager? messagesDataManager, string? userName, CancellationToken cancellationToken = default)
    {
        var databaseServerConnection =
            databaseServerConnections.GetDatabaseServerConnectionByKey(databaseConnectionName);

        return await CreateDatabaseManager(useConsole, logger, databaseServerConnection, messagesDataManager,
            userName, cancellationToken);
    }

    //public იყენებს supportTools
    // ReSharper disable once MemberCanBePrivate.Global
    public static async Task<IDatabaseManager?> CreateDatabaseManager(bool useConsole, ILogger logger,
        DatabaseServerConnectionData? databaseServerConnection, IMessagesDataManager? messagesDataManager,
        string? userName, CancellationToken cancellationToken = default)
    {
        if (databaseServerConnection is null)
            throw new ArgumentOutOfRangeException(nameof(databaseServerConnection));

        return databaseServerConnection.DataProvider switch
        {
            EDataProvider.None => null,
            EDataProvider.Sql => await SqlServerDatabaseManager.Create(logger, useConsole, databaseServerConnection,
                messagesDataManager, userName, cancellationToken),
            EDataProvider.SqLite => null,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static async Task<IDatabaseManager?> CreateDatabaseManager(ILogger logger,
        IHttpClientFactory httpClientFactory, ApiClientSettings? apiClientSettings,
        IMessagesDataManager? messagesDataManager, string? userName, bool useConsole,
        CancellationToken cancellationToken = default)
    {
        if (apiClientSettings is null || string.IsNullOrWhiteSpace(apiClientSettings.Server))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName, "cannot create DatabaseApiClient", cancellationToken);
            logger.LogError("cannot create DatabaseApiClient");
            return null;
        }

        ApiClientSettingsDomain apiClientSettingsDomain = new(apiClientSettings.Server, apiClientSettings.ApiKey);

        var databaseApiClient = new DatabaseApiClient(logger, httpClientFactory, apiClientSettingsDomain.Server,
            apiClientSettingsDomain.ApiKey, useConsole);

        return new RemoteDatabaseManager(logger, databaseApiClient);
    }
}