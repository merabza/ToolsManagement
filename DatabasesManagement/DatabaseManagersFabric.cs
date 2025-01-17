using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ApiClientsManagement;
using DbTools;
using DbTools.Errors;
using LibApiClientParameters;
using LibDatabaseParameters;
using Microsoft.Extensions.Logging;
using OneOf;
using SystemToolsShared;
using SystemToolsShared.Errors;
using WebAgentDatabasesApiContracts;

namespace DatabasesManagement;

public static class DatabaseManagersFabric
{
    public static async ValueTask<OneOf<IDatabaseManager, IEnumerable<Err>>> CreateDatabaseManager(ILogger logger,
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
            return new[] { DbToolsErrors.DatabaseConnectionNameIsNotSpecified };
        }

        var databaseServerConnection =
            databaseServerConnections.GetDatabaseServerConnectionByKey(databaseConnectionName);

        return await CreateDatabaseManager(logger, httpClientFactory, useConsole, databaseServerConnection, apiClients,
            messagesDataManager, userName, cancellationToken);
    }

    //public იყენებს supportTools
    // ReSharper disable once MemberCanBePrivate.Global
    public static async ValueTask<OneOf<IDatabaseManager, IEnumerable<Err>>> CreateDatabaseManager(ILogger logger,
        IHttpClientFactory httpClientFactory, bool useConsole, DatabaseServerConnectionData? databaseServerConnection,
        ApiClients apiClients, IMessagesDataManager? messagesDataManager, string? userName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(databaseServerConnection);

        var dbBackupParameters = DatabaseBackupParametersDomain.Create(databaseServerConnection.FullDbBackupParameters);

        return databaseServerConnection.DatabaseServerProvider switch
        {
            EDatabaseProvider.SqlServer => await CreateSqlServerDatabaseManager(logger, useConsole,
                databaseServerConnection, dbBackupParameters, messagesDataManager, userName, cancellationToken),
            EDatabaseProvider.None => new[] { DbToolsErrors.DatabaseProviderIsNone },
            EDatabaseProvider.SqLite => CreateSqLiteDatabaseManager(),
            EDatabaseProvider.OleDb => CreateOleDatabaseManager(),
            EDatabaseProvider.WebAgent => await CreateRemoteDatabaseManager(logger, httpClientFactory, useConsole,
                databaseServerConnection.DbWebAgentName, apiClients, messagesDataManager, userName, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(databaseServerConnection.DatabaseServerProvider))
        };
    }

    private static OneOf<IDatabaseManager, IEnumerable<Err>> CreateSqLiteDatabaseManager()
    {
        return new[] { DbToolsErrors.CreateSqLiteDatabaseManagerIsNotImplemented };
    }

    private static OneOf<IDatabaseManager, IEnumerable<Err>> CreateOleDatabaseManager()
    {
        return new[] { DbToolsErrors.CreateOleDatabaseManagerIsNotImplemented };
    }

    private static async ValueTask<OneOf<IDatabaseManager, IEnumerable<Err>>> CreateRemoteDatabaseManager(
        ILogger logger, IHttpClientFactory httpClientFactory, bool useConsole, string? apiClientName,
        ApiClients apiClients, IMessagesDataManager? messagesDataManager, string? userName,
        CancellationToken cancellationToken = default)
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

        ApiClientSettingsDomain apiClientSettingsDomain = new(apiClientSettings.Server, apiClientSettings.ApiKey);

        var databaseApiClient = new DatabaseApiClient(logger, httpClientFactory, apiClientSettingsDomain.Server,
            apiClientSettingsDomain.ApiKey, useConsole);

        return new RemoteDatabaseManager(logger, databaseApiClient);
    }


    private static async ValueTask<OneOf<IDatabaseManager, IEnumerable<Err>>> CreateSqlServerDatabaseManager(
        ILogger logger, bool useConsole, DatabaseServerConnectionData databaseServerConnectionData,
        DatabaseBackupParametersDomain databaseBackupParameters, IMessagesDataManager? messagesDataManager,
        string? userName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(databaseServerConnectionData.ServerAddress))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName,
                    "ServerAddress is empty, Cannot create SqlServerManagementClient", cancellationToken);
            logger.LogError("ServerAddress is empty, Cannot create SqlServerManagementClient");
            return new[] { DbToolsErrors.ServerAddressIsEmptyCannotCreateSqlServerManagementClient };
        }

        var dbAuthSettingsCreatorCreateResult = DbAuthSettingsCreator.Create(
            databaseServerConnectionData.WindowsNtIntegratedSecurity, databaseServerConnectionData.User,
            databaseServerConnectionData.Password, useConsole);

        if (dbAuthSettingsCreatorCreateResult.IsT1)
            return (Err[])dbAuthSettingsCreatorCreateResult.AsT1;


        DatabaseServerConnectionDataDomain databaseServerConnectionDataDomain = new(
            databaseServerConnectionData.DatabaseServerProvider, databaseServerConnectionData.ServerAddress,
            dbAuthSettingsCreatorCreateResult.AsT0, databaseServerConnectionData.TrustServerCertificate,
            databaseServerConnectionData.DatabaseFoldersSets);

        return new SqlServerDatabaseManager(logger, useConsole, databaseServerConnectionDataDomain,
            databaseBackupParameters, messagesDataManager, userName);
    }
}