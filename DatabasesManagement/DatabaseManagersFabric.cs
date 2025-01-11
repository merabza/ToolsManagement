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

public static class DatabaseManagersFabric
{
    public static async ValueTask<IDatabaseManager?> CreateDatabaseManager(ILogger logger,
        IHttpClientFactory httpClientFactory, bool useConsole, string? databaseConnectionName,
        DatabaseServerConnections databaseServerConnections, ApiClients apiClients,
        IMessagesDataManager? messagesDataManager, string? userName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(databaseConnectionName))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName,
                    "databaseConnectionName is not specified, cannot create DatabaseApiClient", cancellationToken);
            logger.LogError("databaseConnectionName is not specified, cannot create DatabaseApiClient");
            return null;
        }

        var databaseServerConnection =
            databaseServerConnections.GetDatabaseServerConnectionByKey(databaseConnectionName);

        return await CreateDatabaseManager(logger, httpClientFactory, useConsole, databaseServerConnection, apiClients,
            messagesDataManager, userName, cancellationToken);
    }

    //public იყენებს supportTools
    // ReSharper disable once MemberCanBePrivate.Global
    public static async ValueTask<IDatabaseManager?> CreateDatabaseManager(ILogger logger,
        IHttpClientFactory httpClientFactory, bool useConsole, DatabaseServerConnectionData? databaseServerConnection,
        ApiClients apiClients, IMessagesDataManager? messagesDataManager, string? userName,
        CancellationToken cancellationToken = default)
    {
        if (databaseServerConnection is null)
            throw new ArgumentOutOfRangeException(nameof(databaseServerConnection));

        var dbBackupParameters = DatabaseBackupParametersDomain.Create(databaseServerConnection.FullDbBackupParameters);

        return databaseServerConnection.DatabaseServerProvider switch
        {
            EDatabaseProvider.SqlServer => await CreateSqlServerDatabaseManager(logger, useConsole,
                databaseServerConnection, dbBackupParameters, messagesDataManager, userName, cancellationToken),
            EDatabaseProvider.None => null,
            EDatabaseProvider.SqLite => CreateSqLiteDatabaseManager(),
            EDatabaseProvider.OleDb => CreateOleDatabaseManager(),
            EDatabaseProvider.WebAgent => await CreateRemoteDatabaseManager(logger, httpClientFactory, useConsole,
                databaseServerConnection.DbWebAgentName, apiClients, messagesDataManager, userName, cancellationToken),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private static IDatabaseManager? CreateSqLiteDatabaseManager()
    {
        return null;
    }

    private static IDatabaseManager? CreateOleDatabaseManager()
    {
        return null;
    }

    public static async ValueTask<IDatabaseManager?> CreateRemoteDatabaseManager(ILogger logger,
        IHttpClientFactory httpClientFactory, bool useConsole, string? apiClientName, ApiClients apiClients,
        IMessagesDataManager? messagesDataManager, string? userName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiClientName))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName,
                    "apiClientName is not specified, cannot create DatabaseApiClient", cancellationToken);
            logger.LogError("apiClientName is not specified, cannot create DatabaseApiClient");
            return null;
        }

        var apiClientSettings = apiClients.GetApiClientByKey(apiClientName);

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


    private static async ValueTask<SqlServerDatabaseManager?> CreateSqlServerDatabaseManager(ILogger logger,
        bool useConsole, DatabaseServerConnectionData databaseServerConnectionData,
        DatabaseBackupParametersDomain databaseBackupParameters, IMessagesDataManager? messagesDataManager,
        string? userName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(databaseServerConnectionData.ServerAddress))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName,
                    "ServerAddress is empty, Cannot create SqlServerManagementClient", cancellationToken);
            logger.LogError("ServerAddress is empty, Cannot create SqlServerManagementClient");
            return null;
        }

        var dbAuthSettings = DbAuthSettingsCreator.Create(databaseServerConnectionData.WindowsNtIntegratedSecurity,
            databaseServerConnectionData.User, databaseServerConnectionData.Password);

        if (dbAuthSettings is null)
            return null;

        DatabaseServerConnectionDataDomain databaseServerConnectionDataDomain = new(
            databaseServerConnectionData.DatabaseServerProvider, databaseServerConnectionData.ServerAddress,
            dbAuthSettings, databaseServerConnectionData.TrustServerCertificate,
            databaseServerConnectionData.DatabaseFoldersSets);

        return new SqlServerDatabaseManager(logger, useConsole, databaseServerConnectionDataDomain,
            databaseBackupParameters, messagesDataManager, userName);
    }
}