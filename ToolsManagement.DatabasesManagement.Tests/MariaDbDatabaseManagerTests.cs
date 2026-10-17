using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using DatabaseTools.DbTools;
using DatabaseTools.DbTools.Errors;
using DatabaseTools.DbTools.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ParametersManagement.LibDatabaseParameters;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;
using ToolsManagement.DatabasesManagement.Errors;
using WebAgentContracts.WebAgentDatabasesApiContracts.V1.Responses;
using Xunit;

namespace ToolsManagement.DatabasesManagement.Tests;

public sealed class MariaDbDatabaseManagerTests
{
    private const string DatabaseName = "erratadb";
    private const string FoldersSetName = "Default";
    private const string DateMask = "yyyyMMddHHmmss";

    private static readonly Error ClientError = Error.Problem("ClientError", "Error from db client");

    private readonly Mock<DbClient> _dbClient =
        new(NullLogger.Instance, new DbConnectionStringBuilder(), new DbKit(), false, null!, null!);

    private readonly List<string?> _factoryCalls = [];
    private readonly Mock<ILogger> _logger = new();

    // ---------- DbClient creation ----------

    [Fact]
    public async Task TestConnection_WhenDbClientCannotBeCreated_ReturnsErrorAndLogsIt()
    {
        // Arrange
        MariaDbDatabaseManager sut = CreateSut(null);

        // Act
        Result result = await sut.TestConnection(DatabaseName, CancellationToken.None);

        // Assert
        Assert.Equal(MariaDbDatabaseManagerErrors.CannotCreateDbClient(DatabaseName), result.Error);
        _logger.Verify(l => l.Log(LogLevel.Error, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, _) =>
                state!.ToString() == "Cannot create MariaDB DbClient for database erratadb"), It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    // ---------- operations delegated to the DbClient ----------

    [Fact]
    public async Task TestConnection_WithDatabaseName_TestsTheConnectionToThatDatabase()
    {
        // Arrange
        _dbClient.Setup(c => c.TestConnection(true, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        // Act
        Result result = await CreateSut().TestConnection(DatabaseName, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        VerifySingleFactoryCall(DatabaseName);
    }

    [Fact]
    public async Task TestConnection_WithoutDatabaseName_TestsTheServerConnectionOnly()
    {
        // Arrange
        _dbClient.Setup(c => c.TestConnection(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(ClientError));

        // Act
        Result result = await CreateSut().TestConnection(null, CancellationToken.None);

        // Assert
        Assert.Equal(ClientError, result.Error);
        VerifySingleFactoryCall(null);
    }

    [Fact]
    public async Task GetDatabaseNames_ReturnsTheDatabasesOfTheServer()
    {
        // Arrange
        List<DatabaseInfoModel> databases =
        [
            new(DatabaseName, EDatabaseRecoveryModel.Simple, false), new("mysql", EDatabaseRecoveryModel.Simple, true)
        ];
        _dbClient.Setup(c => c.GetDatabaseInfos(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(databases));

        // Act
        Result<List<DatabaseInfoModel>> result = await CreateSut().GetDatabaseNames(CancellationToken.None);

        // Assert
        Assert.Same(databases, result.Value);
        VerifySingleFactoryCall(null);
    }

    [Fact]
    public async Task GetDatabaseServerInfo_ReturnsTheServerInfoOfTheClient()
    {
        // Arrange
        var serverInfo = new DbServerInfo("12.3.3-MariaDB", string.Empty, null, @"C:\MariaDB\data\", null, false,
            "pazisi");
        _dbClient.Setup(c => c.GetDbServerInfo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(serverInfo));

        // Act
        Result<DbServerInfo> result = await CreateSut().GetDatabaseServerInfo(CancellationToken.None);

        // Assert
        Assert.Same(serverInfo, result.Value);
        VerifySingleFactoryCall(null);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IsServerLocal_ReturnsTheAnswerOfTheClient(bool isLocal)
    {
        // Arrange
        _dbClient.Setup(c => c.IsServerLocal(It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(isLocal));

        // Act
        Result<bool> result = await CreateSut().IsServerLocal(CancellationToken.None);

        // Assert
        Assert.Equal(isLocal, result.Value);
        VerifySingleFactoryCall(null);
    }

    [Fact]
    public async Task IsDatabaseExists_AsksTheServerWithoutSelectingADatabase()
    {
        // Arrange
        _dbClient.Setup(c => c.IsDatabaseExists(DatabaseName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        // Act
        Result<bool> result = await CreateSut().IsDatabaseExists(DatabaseName, CancellationToken.None);

        // Assert
        Assert.True(result.Value);
        VerifySingleFactoryCall(null);
    }

    //DbClient.ExecuteCommand ვირტუალური არ არის — მოკის DbKit კავშირს ვერ ქმნის, ამიტომ კლიენტის შეცდომა ბრუნდება
    [Fact]
    public async Task ExecuteCommand_RunsTheCommandWithAClientOfTheRequestedDatabase()
    {
        // Act
        Result result = await CreateSut().ExecuteCommand("SELECT 1", DatabaseName, CancellationToken.None);

        // Assert
        Assert.Equal(DbClientErrors.CannotCreateDatabaseConnection, result.Error);
        VerifySingleFactoryCall(DatabaseName);
    }

    [Fact]
    public async Task GetDatabaseFoldersSetNames_ReturnsTheFoldersSetsOfTheConnectionWithoutConnecting()
    {
        // Act
        Result<List<string>> result = await CreateSut().GetDatabaseFoldersSetNames(CancellationToken.None);

        // Assert
        Assert.Equal([FoldersSetName], result.Value);
        Assert.Empty(_factoryCalls);
    }

    // ---------- operations MariaDB does not support ----------

    [Theory]
    [InlineData(nameof(MariaDbDatabaseManager.CreateBackup))]
    [InlineData(nameof(MariaDbDatabaseManager.RestoreDatabaseFromBackup))]
    [InlineData(nameof(MariaDbDatabaseManager.CheckRepairDatabase))]
    [InlineData(nameof(MariaDbDatabaseManager.RecompileProcedures))]
    [InlineData(nameof(MariaDbDatabaseManager.UpdateStatistics))]
    [InlineData(nameof(MariaDbDatabaseManager.SetDefaultFolders))]
    [InlineData(nameof(MariaDbDatabaseManager.ChangeDatabaseRecoveryModel))]
    public async Task UnsupportedOperation_ReturnsNotSupportedErrorWithoutConnecting(string operationName)
    {
        // Act
        Error error = await RunUnsupportedOperation(CreateSut(), operationName);

        // Assert
        Assert.Equal(MariaDbDatabaseManagerErrors.OperationIsNotSupported(operationName), error);
        Assert.Empty(_factoryCalls);
    }

    private static async Task<Error> RunUnsupportedOperation(MariaDbDatabaseManager sut, string operationName)
    {
        return operationName switch
        {
            nameof(MariaDbDatabaseManager.CreateBackup) => (await sut.CreateBackup(
                new DatabaseBackupParametersDomain("Pz_", DateMask, "bak", "_Full_", false, false, EBackupType.Full),
                DatabaseName, FoldersSetName, CancellationToken.None)).Error,
            nameof(MariaDbDatabaseManager.RestoreDatabaseFromBackup) => (await sut.RestoreDatabaseFromBackup(
                new BackupFileParameters(null, "erratadb.bak", "erratadb_", ".bak", DateMask), DatabaseName,
                FoldersSetName, EDatabaseRecoveryModel.Simple, null, CancellationToken.None)).Error,
            nameof(MariaDbDatabaseManager.CheckRepairDatabase) =>
                (await sut.CheckRepairDatabase(DatabaseName, CancellationToken.None)).Error,
            nameof(MariaDbDatabaseManager.RecompileProcedures) =>
                (await sut.RecompileProcedures(DatabaseName, CancellationToken.None)).Error,
            nameof(MariaDbDatabaseManager.UpdateStatistics) =>
                (await sut.UpdateStatistics(DatabaseName, CancellationToken.None)).Error,
            nameof(MariaDbDatabaseManager.SetDefaultFolders) => (await sut.SetDefaultFolders(@"C:\Bak", @"C:\Data",
                @"C:\Log", CancellationToken.None)).Error,
            nameof(MariaDbDatabaseManager.ChangeDatabaseRecoveryModel) => (await sut.ChangeDatabaseRecoveryModel(
                DatabaseName, EDatabaseRecoveryModel.Full, CancellationToken.None)).Error,
            _ => throw new ArgumentOutOfRangeException(nameof(operationName), operationName, null)
        };
    }

    private MariaDbDatabaseManager CreateSut()
    {
        return CreateSut(_dbClient.Object);
    }

    private MariaDbDatabaseManager CreateSut(DbClient? dbClient)
    {
        var databaseServerConnectionDataDomain = new DatabaseServerConnectionDataDomain(EDatabaseProvider.MariaDb,
            "localhost", new DbAuthSettings("root", "fake-password"), false,
            new Dictionary<string, DatabaseFoldersSet> { [FoldersSetName] = new() });

        return new MariaDbDatabaseManager(_logger.Object, databaseServerConnectionDataDomain, databaseName =>
        {
            _factoryCalls.Add(databaseName);
            return dbClient;
        });
    }

    private void VerifySingleFactoryCall(string? expectedDatabaseName)
    {
        Assert.Equal(expectedDatabaseName, Assert.Single(_factoryCalls));
    }
}
