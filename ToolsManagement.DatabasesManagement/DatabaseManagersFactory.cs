using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DatabaseTools.DbTools;
using DatabaseTools.DbTools.Errors;
using Microsoft.Extensions.Logging;
using OneOf;
using ParametersManagement.LibApiClientParameters;
using ParametersManagement.LibDatabaseParameters;
using SystemTools.SystemToolsShared;
using SystemTools.SystemToolsShared.Errors;
using ToolsManagement.ApiClientsManagement;
using WebAgentContracts.WebAgentDatabasesApiContracts;

namespace ToolsManagement.DatabasesManagement;

public static class DatabaseManagersFactory
{
    public static Task<OneOf<IDatabaseManager, Err[]>> CreateDatabaseManager(ILogger logger, bool useConsole,
        string? databaseConnectionName, DatabaseServerConnections databaseServerConnections,
        CancellationToken cancellationToken = default)
    {
        return CreateDatabaseManager(logger, useConsole, databaseConnectionName, databaseServerConnections, null, null,
            null, null, cancellationToken);
    }

    public static async Task<OneOf<IDatabaseManager, Err[]>> CreateDatabaseManager(ILogger logger, bool useConsole,
        string? databaseConnectionName, DatabaseServerConnections databaseServerConnections, ApiClients? apiClients,
        IHttpClientFactory? httpClientFactory, IMessagesDataManager? messagesDataManager, string? userName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(databaseConnectionName))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName,
                    "databaseConnectionName is not specified, cannot create DatabaseApiClient", cancellationToken);
            logger.LogError("databaseConnectionName is not specified, cannot create DatabaseApiClient");
            return new[] { DbToolsErrors.DatabaseConnectionNameIsNotSpecified };
        }

        if (messagesDataManager is not null)
            await messagesDataManager.SendMessage(userName,
                $"CreateDatabaseManager databaseConnectionName={databaseConnectionName}", cancellationToken);

        var databaseServerConnection =
            databaseServerConnections.GetDatabaseServerConnectionByKey(databaseConnectionName);

        return await CreateDatabaseManager(logger, useConsole, databaseServerConnection, apiClients, httpClientFactory,
            messagesDataManager, userName, cancellationToken);
    }

    //public იყენებს supportTools
    // ReSharper disable once MemberCanBePrivate.Global
    public static async ValueTask<OneOf<IDatabaseManager, Err[]>> CreateDatabaseManager(ILogger logger, bool useConsole,
        DatabaseServerConnectionData? databaseServerConnection, ApiClients? apiClients,
        IHttpClientFactory? httpClientFactory, IMessagesDataManager? messagesDataManager, string? userName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(databaseServerConnection);

        //var dbBackupParameters = DatabaseBackupParametersDomain.Create(databaseServerConnection.FullDbBackupParameters);

        switch (databaseServerConnection.DatabaseServerProvider)
        {
            case EDatabaseProvider.SqlServer:
                return await CreateSqlServerDatabaseManager(logger, useConsole, databaseServerConnection,
                    messagesDataManager, userName, cancellationToken);
            case EDatabaseProvider.None:
                return new[] { DbToolsErrors.DatabaseProviderIsNone };
            case EDatabaseProvider.SqLite:
                return CreateSqLiteDatabaseManager();
            case EDatabaseProvider.OleDb:
                return CreateOleDatabaseManager();
            case EDatabaseProvider.WebAgent:
                if (apiClients is not null && httpClientFactory is not null)
                    return await CreateRemoteDatabaseManager(logger, httpClientFactory, useConsole,
                        databaseServerConnection.DbWebAgentName, apiClients, messagesDataManager, userName,
                        cancellationToken);
                throw new ArgumentOutOfRangeException();
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static OneOf<IDatabaseManager, Err[]> CreateSqLiteDatabaseManager()
    {
        return new[] { DbToolsErrors.CreateSqLiteDatabaseManagerIsNotImplemented };
    }

    private static OneOf<IDatabaseManager, Err[]> CreateOleDatabaseManager()
    {
        return new[] { DbToolsErrors.CreateOleDatabaseManagerIsNotImplemented };
    }

    //public იყენებს supportTools
    // ReSharper disable once MemberCanBePrivate.Global
    public static async Task<OneOf<IDatabaseManager, Err[]>> CreateRemoteDatabaseManager(ILogger logger,
        IHttpClientFactory httpClientFactory, bool useConsole, string? apiClientName, ApiClients apiClients,
        IMessagesDataManager? messagesDataManager, string? userName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiClientName))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName,
                    "apiClientName is not specified, cannot create DatabaseApiClient", cancellationToken);
            logger.LogError("apiClientName is not specified, cannot create DatabaseApiClient");
            return new[] { DbToolsErrors.ApiClientNameIsNotSpecifiedCannotCreateDatabaseApiClient };
        }

        var apiClientSettings = apiClients.GetApiClientByKey(apiClientName);

        if (apiClientSettings is null)
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName,
                    "apiClientSettings is null, cannot create DatabaseApiClient", cancellationToken);
            logger.LogError("apiClientSettings is null, cannot create DatabaseApiClient");
            return new[] { DbToolsErrors.ApiClientSettingsIsNull };
        }

        if (string.IsNullOrWhiteSpace(apiClientSettings.Server))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName,
                    "Server is not specified in apiClientSettings, cannot create DatabaseApiClient", cancellationToken);
            logger.LogError("cannot create DatabaseApiClient");
            return new[] { DbToolsErrors.ServerIsNotSpecifiedInApiClientSettings };
        }

        var apiClientSettingsDomain = new ApiClientSettingsDomain(apiClientSettings.Server, apiClientSettings.ApiKey);

        var databaseApiClient = new DatabaseApiClient(logger, httpClientFactory, apiClientSettingsDomain.Server,
            apiClientSettingsDomain.ApiKey, useConsole);

        return new RemoteDatabaseManager(logger, databaseApiClient);
    }

    private static async ValueTask<OneOf<IDatabaseManager, Err[]>> CreateSqlServerDatabaseManager(ILogger logger,
        bool useConsole, DatabaseServerConnectionData databaseServerConnectionData,
        IMessagesDataManager? messagesDataManager, string? userName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(databaseServerConnectionData.ServerAddress))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName,
                    "ServerAddress is empty, Cannot create SqlServerManagementClient", cancellationToken);
            logger.LogError("ServerAddress is empty, Cannot create SqlServerManagementClient");
            return new[] { DbToolsErrors.ServerAddressIsEmptyCannotCreateSqlServerManagementClient };
        }

        //if (messagesDataManager is not null)
        //{
        //    await messagesDataManager.SendMessage(userName,
        //        $"DbAuthSettingsCreator.Create databaseServerConnectionData.WindowsNtIntegratedSecurity={databaseServerConnectionData.WindowsNtIntegratedSecurity}",
        //        cancellationToken);
        //    await messagesDataManager.SendMessage(userName,
        //        $"databaseServerConnectionData.User={databaseServerConnectionData.User}", cancellationToken);
        //    await messagesDataManager.SendMessage(userName,
        //        $"databaseServerConnectionData.Password={databaseServerConnectionData.Password}", cancellationToken);
        //}

        var dbAuthSettingsCreatorCreateResult = DbAuthSettingsCreator.Create(
            databaseServerConnectionData.WindowsNtIntegratedSecurity, databaseServerConnectionData.ServerUser,
            databaseServerConnectionData.ServerPass, useConsole);

        if (dbAuthSettingsCreatorCreateResult.IsT1)

            return dbAuthSettingsCreatorCreateResult.AsT1;

        var databaseServerConnectionDataDomain = new DatabaseServerConnectionDataDomain(
            databaseServerConnectionData.DatabaseServerProvider, databaseServerConnectionData.ServerAddress,
            dbAuthSettingsCreatorCreateResult.AsT0, databaseServerConnectionData.TrustServerCertificate,
            databaseServerConnectionData.DatabaseFoldersSets ?? []);

        return new SqlServerDatabaseManager(logger, useConsole, databaseServerConnectionDataDomain, messagesDataManager,
            userName);
    }
}