using System.Threading;
using System.Threading.Tasks;
using DatabaseTools.DbTools.Errors;
using Microsoft.Extensions.Logging.Abstractions;
using ParametersManagement.LibDatabaseParameters;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;
using ToolsManagement.DatabasesManagement.Errors;
using Xunit;

namespace ToolsManagement.DatabasesManagement.Tests;

public sealed class DatabaseManagersFactoryTests
{
    [Fact]
    public async Task CreateDatabaseManager_ForMariaDbConnection_CreatesMariaDbDatabaseManager()
    {
        // Act
        Result<IDatabaseManager> result = await CreateDatabaseManager(CreateMariaDbConnection());

        // Assert
        Assert.IsType<MariaDbDatabaseManager>(result.Value);
    }

    //MariaDB-ს Windows-ის ავთენტიფიკაცია არ აქვს — დროშა იგნორირდება და მომხმარებლით უკავშირდება
    [Fact]
    public async Task CreateDatabaseManager_ForMariaDbConnectionWithWindowsSecurity_IgnoresTheFlag()
    {
        // Arrange
        DatabaseServerConnectionData connection = CreateMariaDbConnection();
        connection.WindowsNtIntegratedSecurity = true;

        // Act
        Result<IDatabaseManager> result = await CreateDatabaseManager(connection);

        // Assert
        Assert.IsType<MariaDbDatabaseManager>(result.Value);
    }

    [Fact]
    public async Task CreateDatabaseManager_ForMariaDbConnectionWithoutAddress_ReturnsServerAddressIsEmpty()
    {
        // Arrange
        DatabaseServerConnectionData connection = CreateMariaDbConnection();
        connection.ServerAddress = " ";

        // Act
        Result<IDatabaseManager> result = await CreateDatabaseManager(connection);

        // Assert
        Assert.Equal(MariaDbDatabaseManagerErrors.ServerAddressIsEmpty, result.Error);
    }

    [Fact]
    public async Task CreateDatabaseManager_ForMariaDbConnectionWithoutPassword_ReturnsCredentialsError()
    {
        // Arrange
        DatabaseServerConnectionData connection = CreateMariaDbConnection();
        connection.ServerPass = null;

        // Act
        Result<IDatabaseManager> result = await CreateDatabaseManager(connection);

        // Assert
        Assert.Equal(
            DbToolsErrors.WindowsNtIntegratedSecurityIsOffAndServerUserDoesNotSpecifiedOrServerPassDoesNotSpecified,
            result.Error);
    }

    private static DatabaseServerConnectionData CreateMariaDbConnection()
    {
        return new DatabaseServerConnectionData
        {
            DatabaseServerProvider = EDatabaseProvider.MariaDb,
            ServerAddress = "localhost:3306",
            ServerUser = "root",
            ServerPass = "fake-password"
        };
    }

    private static async Task<Result<IDatabaseManager>> CreateDatabaseManager(DatabaseServerConnectionData connection)
    {
        return await DatabaseManagersFactory.CreateDatabaseManager("Tests", NullLogger.Instance, false, connection,
            null, null, null, null, CancellationToken.None);
    }
}
