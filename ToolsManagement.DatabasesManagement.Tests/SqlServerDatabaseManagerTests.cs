using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.IO;
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

public sealed class SqlServerDatabaseManagerTests
{
    private const string UserName = "tester";
    private const string DatabaseName = "Db1";
    private const string FoldersSetName = "HDD";
    private const string WindowsBackupFolder = @"E:\BAK";
    private const string WindowsDataFolder = @"E:\Data";
    private const string WindowsDataLogFolder = @"E:\Log";
    private const string LinuxBackupFolder = "/var/backup";
    private const string LinuxDataFolder = "/var/data";
    private const string LinuxDataLogFolder = "/var/log";
    private const string BackupFileName = "Db1_Full.bak";
    private const string DateMask = "yyyyMMddHHmmss";
    private const string BackupPrefix = "Pz_Db1_Full_";
    private const string BackupSuffix = ".bak";
    private const string Windows = "Windows";
    private const string Linux = "Linux";
    private const string HostPlatformMessage = "Host platform does not detected";
    private const string RestoreFilesMessage = "Restore Files does not detected";

    private static readonly Error ClientError = Error.Problem("ClientError", "Error from db client");
    private static readonly List<RestoreFileModel> RestoreFiles = [new("Db1", "D"), new("Db1_log", "L")];

    private readonly Mock<DbClient> _dbClient =
        new(NullLogger.Instance, new DbConnectionStringBuilder(), new DbKit(), false, null!, null!);

    private readonly List<(EDatabaseProvider Provider, string? DatabaseName)> _factoryCalls = [];
    private readonly Mock<ILogger> _logger = new();
    private readonly Mock<IMessagesDataManager> _messagesDataManager = new();

    // ---------- DbClient creation ----------

    [Fact]
    public async Task ExecuteCommand_WhenDbClientCannotBeCreated_ReturnsErrorAndNotifiesUser()
    {
        // Arrange
        SqlServerDatabaseManager sut = CreateSutWithoutDbClient();

        // Act
        Result result = await sut.ExecuteCommand("SELECT 1", DatabaseName, CancellationToken.None);

        // Assert
        Assert.Equal(SqlServerDatabaseManagerErrors.CannotCreateDbClient(DatabaseName), result.Error);
        _messagesDataManager.Verify(
            m => m.SendMessage(UserName, "Cannot create DbClient for database Db1", It.IsAny<CancellationToken>()),
            Times.Once);
        VerifyErrorLogged("Cannot create DbClient for database Db1");
    }

    [Fact]
    public async Task ExecuteCommand_WhenDbClientCannotBeCreatedWithoutMessagesManager_ReturnsErrorAndLogs()
    {
        // Arrange
        SqlServerDatabaseManager sut = CreateSut(null, null, CreateFoldersSets(WindowsBackupFolder));

        // Act
        Result result = await sut.ExecuteCommand("SELECT 1", DatabaseName, CancellationToken.None);

        // Assert
        Assert.Equal(SqlServerDatabaseManagerErrors.CannotCreateDbClient(DatabaseName), result.Error);
        VerifyErrorLogged("Cannot create DbClient for database Db1");
    }

    [Fact]
    public async Task ExecuteCommand_WhenDbClientCreated_RequestsClientForDatabaseAndReturnsClientResult()
    {
        // Arrange
        SqlServerDatabaseManager sut = CreateSut();

        // Act
        Result result = await sut.ExecuteCommand("SELECT 1", DatabaseName, CancellationToken.None);

        // Assert
        //DbKit without connection makes DbClient.ExecuteCommand fail before it reaches any database
        Assert.Equal(DbClientErrors.CannotCreateDatabaseConnection, result.Error);
        VerifySingleFactoryCall(DatabaseName);
        _messagesDataManager.VerifyNoOtherCalls();
    }

    // ---------- Simple pass-through operations ----------

    [Fact]
    public async Task CheckRepairDatabase_WhenDbClientCreated_ReturnsClientResult()
    {
        // Arrange
        _dbClient.Setup(c => c.CheckRepairDatabase(DatabaseName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(ClientError));

        // Act
        Result result = await CreateSut().CheckRepairDatabase(DatabaseName, CancellationToken.None);

        // Assert
        Assert.Equal(ClientError, result.Error);
        VerifySingleFactoryCall(null);
    }

    [Fact]
    public async Task CheckRepairDatabase_WhenDbClientCannotBeCreated_ReturnsError()
    {
        // Act
        Result result = await CreateSutWithoutDbClient().CheckRepairDatabase(DatabaseName, CancellationToken.None);

        // Assert
        Assert.Equal(SqlServerDatabaseManagerErrors.CannotCreateDbClient(null), result.Error);
    }

    [Fact]
    public async Task GetDatabaseNames_WhenDbClientCreated_ReturnsDatabaseInfos()
    {
        // Arrange
        List<DatabaseInfoModel> databases = [new(DatabaseName, EDatabaseRecoveryModel.Full, false)];
        _dbClient.Setup(c => c.GetDatabaseInfos(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(databases));

        // Act
        Result<List<DatabaseInfoModel>> result = await CreateSut().GetDatabaseNames(CancellationToken.None);

        // Assert
        Assert.Same(databases, result.Value);
        VerifySingleFactoryCall(null);
    }

    [Fact]
    public async Task GetDatabaseNames_WhenDbClientCannotBeCreated_ReturnsError()
    {
        // Act
        Result<List<DatabaseInfoModel>> result =
            await CreateSutWithoutDbClient().GetDatabaseNames(CancellationToken.None);

        // Assert
        Assert.Equal(SqlServerDatabaseManagerErrors.CannotCreateDbClient(null), result.Error);
    }

    [Fact]
    public async Task IsDatabaseExists_WhenDbClientCreated_ReturnsClientAnswer()
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

    [Fact]
    public async Task IsDatabaseExists_WhenDbClientCannotBeCreated_ReturnsError()
    {
        // Act
        Result<bool> result = await CreateSutWithoutDbClient().IsDatabaseExists(DatabaseName, CancellationToken.None);

        // Assert
        Assert.Equal(SqlServerDatabaseManagerErrors.CannotCreateDbClient(null), result.Error);
    }

    [Fact]
    public async Task RecompileProcedures_WhenDbClientCreated_ReturnsClientResult()
    {
        // Arrange
        _dbClient.Setup(c => c.RecompileProcedures(DatabaseName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(ClientError));

        // Act
        Result result = await CreateSut().RecompileProcedures(DatabaseName, CancellationToken.None);

        // Assert
        Assert.Equal(ClientError, result.Error);
        VerifySingleFactoryCall(null);
    }

    [Fact]
    public async Task RecompileProcedures_WhenDbClientCannotBeCreated_ReturnsError()
    {
        // Act
        Result result = await CreateSutWithoutDbClient().RecompileProcedures(DatabaseName, CancellationToken.None);

        // Assert
        Assert.Equal(SqlServerDatabaseManagerErrors.CannotCreateDbClient(null), result.Error);
    }

    [Fact]
    public async Task TestConnection_WithDatabaseName_ConnectsToDatabaseAndChecksIt()
    {
        // Arrange
        _dbClient.Setup(c => c.TestConnection(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(ClientError));

        // Act
        Result result = await CreateSut().TestConnection(DatabaseName, CancellationToken.None);

        // Assert
        Assert.Equal(ClientError, result.Error);
        VerifySingleFactoryCall(DatabaseName);
    }

    [Fact]
    public async Task TestConnection_WithoutDatabaseName_ChecksServerOnly()
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
    public async Task TestConnection_WhenDbClientCannotBeCreated_ReturnsError()
    {
        // Act
        Result result = await CreateSutWithoutDbClient().TestConnection(DatabaseName, CancellationToken.None);

        // Assert
        Assert.Equal(SqlServerDatabaseManagerErrors.CannotCreateDbClient(DatabaseName), result.Error);
    }

    [Fact]
    public async Task UpdateStatistics_WhenDbClientCreated_ReturnsClientResult()
    {
        // Arrange
        _dbClient.Setup(c => c.UpdateStatistics(DatabaseName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(ClientError));

        // Act
        Result result = await CreateSut().UpdateStatistics(DatabaseName, CancellationToken.None);

        // Assert
        Assert.Equal(ClientError, result.Error);
        VerifySingleFactoryCall(null);
    }

    [Fact]
    public async Task UpdateStatistics_WhenDbClientCannotBeCreated_ReturnsError()
    {
        // Act
        Result result = await CreateSutWithoutDbClient().UpdateStatistics(DatabaseName, CancellationToken.None);

        // Assert
        Assert.Equal(SqlServerDatabaseManagerErrors.CannotCreateDbClient(null), result.Error);
    }

    [Fact]
    public async Task SetDefaultFolders_WhenDbClientCreated_PassesFoldersToClient()
    {
        // Arrange
        _dbClient.Setup(c => c.SetDefaultFolders(WindowsBackupFolder, WindowsDataFolder, WindowsDataLogFolder,
            It.IsAny<CancellationToken>())).ReturnsAsync(Result.Failure(ClientError));

        // Act
        Result result = await CreateSut().SetDefaultFolders(WindowsBackupFolder, WindowsDataFolder,
            WindowsDataLogFolder, CancellationToken.None);

        // Assert
        Assert.Equal(ClientError, result.Error);
        VerifySingleFactoryCall(null);
    }

    [Fact]
    public async Task SetDefaultFolders_WhenDbClientCannotBeCreated_ReturnsError()
    {
        // Act
        Result result = await CreateSutWithoutDbClient().SetDefaultFolders(WindowsBackupFolder, WindowsDataFolder,
            WindowsDataLogFolder, CancellationToken.None);

        // Assert
        Assert.Equal(SqlServerDatabaseManagerErrors.CannotCreateDbClient(null), result.Error);
    }

    [Fact]
    public async Task GetDatabaseFoldersSetNames_ReturnsConfiguredSetNamesWithoutConnecting()
    {
        // Arrange
        var foldersSets = new Dictionary<string, DatabaseFoldersSet>
        {
            ["Default"] = new(), [FoldersSetName] = new()
        };
        SqlServerDatabaseManager sut = CreateSut(_dbClient.Object, null, foldersSets);
        string[] expected = ["Default", FoldersSetName];

        // Act
        Result<List<string>> result = await sut.GetDatabaseFoldersSetNames(CancellationToken.None);

        // Assert
        Assert.Equal(expected, result.Value);
        Assert.Empty(_factoryCalls);
    }

    [Fact]
    public async Task ChangeDatabaseRecoveryModel_WhenDbClientCreated_PassesModelToClient()
    {
        // Arrange
        _dbClient.Setup(c => c.ChangeDatabaseRecoveryModel(DatabaseName, EDatabaseRecoveryModel.Simple,
            It.IsAny<CancellationToken>())).ReturnsAsync(Result.Failure(ClientError));

        // Act
        Result result = await CreateSut().ChangeDatabaseRecoveryModel(DatabaseName, EDatabaseRecoveryModel.Simple,
            CancellationToken.None);

        // Assert
        Assert.Equal(ClientError, result.Error);
        VerifySingleFactoryCall(null);
    }

    [Fact]
    public async Task ChangeDatabaseRecoveryModel_WhenDbClientCannotBeCreated_ReturnsError()
    {
        // Act
        Result result = await CreateSutWithoutDbClient().ChangeDatabaseRecoveryModel(DatabaseName,
            EDatabaseRecoveryModel.Simple, CancellationToken.None);

        // Assert
        Assert.Equal(SqlServerDatabaseManagerErrors.CannotCreateDbClient(null), result.Error);
    }

    [Fact]
    public async Task GetDatabaseServerInfo_WhenDbClientCreated_ReturnsServerInfo()
    {
        // Arrange
        var serverInfo = new DbServerInfo("16.0", "MSSQLSERVER", WindowsBackupFolder, WindowsDataFolder,
            WindowsDataLogFolder, true, "Server");
        _dbClient.Setup(c => c.GetDbServerInfo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(serverInfo));

        // Act
        Result<DbServerInfo> result = await CreateSut().GetDatabaseServerInfo(CancellationToken.None);

        // Assert
        Assert.Same(serverInfo, result.Value);
        VerifySingleFactoryCall(null);
    }

    [Fact]
    public async Task GetDatabaseServerInfo_WhenDbClientCannotBeCreated_ReturnsError()
    {
        // Act
        Result<DbServerInfo> result = await CreateSutWithoutDbClient().GetDatabaseServerInfo(CancellationToken.None);

        // Assert
        Assert.Equal(SqlServerDatabaseManagerErrors.CannotCreateDbClient(null), result.Error);
    }

    [Fact]
    public async Task IsServerLocal_WhenDbClientCreated_ReturnsClientAnswer()
    {
        // Arrange
        _dbClient.Setup(c => c.IsServerLocal(It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(true));

        // Act
        Result<bool> result = await CreateSut().IsServerLocal(CancellationToken.None);

        // Assert
        Assert.True(result.Value);
        VerifySingleFactoryCall(null);
    }

    [Fact]
    public async Task IsServerLocal_WhenDbClientCannotBeCreated_ReturnsError()
    {
        // Act
        Result<bool> result = await CreateSutWithoutDbClient().IsServerLocal(CancellationToken.None);

        // Assert
        Assert.Equal(SqlServerDatabaseManagerErrors.CannotCreateDbClient(null), result.Error);
    }

    // ---------- RestoreDatabaseFromBackup ----------

    [Fact]
    public async Task RestoreDatabaseFromBackup_OnWindowsWithFullRecoveryModel_RestoresFromBackupFolder()
    {
        // Arrange
        const string backupFileFullName = @"E:\BAK\Db1_Full.bak";
        SetupHostPlatform(Windows);
        SetupRestoreFiles(backupFileFullName);
        SetupRestoreDatabase(backupFileFullName, WindowsDataFolder, WindowsDataLogFolder, @"\", Result.Success());

        // Act
        Result result = await CreateSut().RestoreDatabaseFromBackup(CreateBackupFileParameters(), DatabaseName,
            FoldersSetName, EDatabaseRecoveryModel.Full, null, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        VerifyRestoreDatabase(backupFileFullName, WindowsDataFolder, WindowsDataLogFolder, @"\");
        VerifyRecoveryModelNeverChanged();
        VerifySingleFactoryCall(null);
    }

    [Fact]
    public async Task RestoreDatabaseFromBackup_OnLinux_UsesForwardSlashSeparator()
    {
        // Arrange
        const string backupFileFullName = "/var/backup/Db1_Full.bak";
        SetupHostPlatform(Linux);
        SetupRestoreFiles(backupFileFullName);
        SetupRestoreDatabase(backupFileFullName, LinuxDataFolder, LinuxDataLogFolder, "/", Result.Success());
        SqlServerDatabaseManager sut = CreateSut(_dbClient.Object, _messagesDataManager.Object,
            CreateFoldersSets(LinuxBackupFolder, LinuxDataFolder, LinuxDataLogFolder));

        // Act
        Result result = await sut.RestoreDatabaseFromBackup(CreateBackupFileParameters(), DatabaseName,
            FoldersSetName, EDatabaseRecoveryModel.Full, null, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        VerifyRestoreDatabase(backupFileFullName, LinuxDataFolder, LinuxDataLogFolder, "/");
    }

    [Fact]
    public async Task RestoreDatabaseFromBackup_WhenRestoreFromFolderExists_RestoresFromThatFolder()
    {
        // Arrange
        string existingFolder = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        string backupFileFullName = existingFolder + @"\" + BackupFileName;
        SetupHostPlatform(Windows);
        SetupRestoreFiles(backupFileFullName);
        SetupRestoreDatabase(backupFileFullName, WindowsDataFolder, WindowsDataLogFolder, @"\", Result.Success());

        // Act
        Result result = await CreateSut().RestoreDatabaseFromBackup(CreateBackupFileParameters(), DatabaseName,
            FoldersSetName, EDatabaseRecoveryModel.Full, existingFolder, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        VerifyRestoreDatabase(backupFileFullName, WindowsDataFolder, WindowsDataLogFolder, @"\");
    }

    [Fact]
    public async Task RestoreDatabaseFromBackup_WhenRestoreFromFolderDoesNotExist_RestoresFromBackupFolder()
    {
        // Arrange
        const string backupFileFullName = @"E:\BAK\Db1_Full.bak";
        SetupHostPlatform(Windows);
        SetupRestoreFiles(backupFileFullName);
        SetupRestoreDatabase(backupFileFullName, WindowsDataFolder, WindowsDataLogFolder, @"\", Result.Success());
        string missingFolder = Path.Combine(AppContext.BaseDirectory, Guid.NewGuid().ToString("N"));

        // Act
        Result result = await CreateSut().RestoreDatabaseFromBackup(CreateBackupFileParameters(), DatabaseName,
            FoldersSetName, EDatabaseRecoveryModel.Full, missingFolder, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        VerifyRestoreDatabase(backupFileFullName, WindowsDataFolder, WindowsDataLogFolder, @"\");
    }

    [Fact]
    public async Task RestoreDatabaseFromBackup_WhenHostPlatformUnknown_ReturnsErrorAndNotifiesUser()
    {
        // Arrange
        _dbClient.Setup(c => c.HostPlatform(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<string>(ClientError));

        // Act
        Result result = await CreateSut().RestoreDatabaseFromBackup(CreateBackupFileParameters(), DatabaseName,
            FoldersSetName, EDatabaseRecoveryModel.Full, null, CancellationToken.None);

        // Assert
        Assert.Equal(ClientError, result.Error);
        _messagesDataManager.Verify(m => m.SendMessage(UserName, HostPlatformMessage, It.IsAny<CancellationToken>()),
            Times.Once);
        VerifyErrorLogged(HostPlatformMessage);
        _dbClient.Verify(c => c.GetRestoreFiles(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RestoreDatabaseFromBackup_WhenHostPlatformUnknownWithoutMessagesManager_ReturnsError()
    {
        // Arrange
        _dbClient.Setup(c => c.HostPlatform(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<string>(ClientError));
        SqlServerDatabaseManager sut = CreateSut(_dbClient.Object, null, CreateFoldersSets(WindowsBackupFolder));

        // Act
        Result result = await sut.RestoreDatabaseFromBackup(CreateBackupFileParameters(), DatabaseName,
            FoldersSetName, EDatabaseRecoveryModel.Full, null, CancellationToken.None);

        // Assert
        Assert.Equal(ClientError, result.Error);
        VerifyErrorLogged(HostPlatformMessage);
    }

    [Fact]
    public async Task RestoreDatabaseFromBackup_WhenDbClientCannotBeCreated_ReturnsError()
    {
        // Act
        Result result = await CreateSutWithoutDbClient().RestoreDatabaseFromBackup(CreateBackupFileParameters(),
            DatabaseName, FoldersSetName, EDatabaseRecoveryModel.Full, null, CancellationToken.None);

        // Assert
        Assert.Equal(SqlServerDatabaseManagerErrors.CannotCreateDbClient(null), result.Error);
    }

    [Fact]
    public async Task RestoreDatabaseFromBackup_WhenBackupFolderIsNotSpecified_ReturnsNoRestoreFrom()
    {
        // Arrange
        SetupHostPlatform(Windows);
        SqlServerDatabaseManager sut =
            CreateSut(_dbClient.Object, _messagesDataManager.Object, CreateFoldersSets(" "));

        // Act
        Result result = await sut.RestoreDatabaseFromBackup(CreateBackupFileParameters(), DatabaseName,
            FoldersSetName, EDatabaseRecoveryModel.Full, null, CancellationToken.None);

        // Assert
        Assert.Equal(DbClientErrors.NoRestoreFrom, result.Error);
        _dbClient.Verify(c => c.GetRestoreFiles(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RestoreDatabaseFromBackup_WhenRestoreFilesUnknown_ReturnsErrorAndNotifiesUser()
    {
        // Arrange
        SetupHostPlatform(Windows);
        _dbClient.Setup(c => c.GetRestoreFiles(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<List<RestoreFileModel>>(ClientError));

        // Act
        Result result = await CreateSut().RestoreDatabaseFromBackup(CreateBackupFileParameters(), DatabaseName,
            FoldersSetName, EDatabaseRecoveryModel.Full, null, CancellationToken.None);

        // Assert
        Assert.Equal(ClientError, result.Error);
        _messagesDataManager.Verify(m => m.SendMessage(UserName, RestoreFilesMessage, It.IsAny<CancellationToken>()),
            Times.Once);
        VerifyErrorLogged(RestoreFilesMessage);
        VerifyRestoreDatabaseNeverCalled();
    }

    [Fact]
    public async Task RestoreDatabaseFromBackup_WhenRestoreFilesUnknownWithoutMessagesManager_ReturnsError()
    {
        // Arrange
        SetupHostPlatform(Windows);
        _dbClient.Setup(c => c.GetRestoreFiles(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<List<RestoreFileModel>>(ClientError));
        SqlServerDatabaseManager sut = CreateSut(_dbClient.Object, null, CreateFoldersSets(WindowsBackupFolder));

        // Act
        Result result = await sut.RestoreDatabaseFromBackup(CreateBackupFileParameters(), DatabaseName,
            FoldersSetName, EDatabaseRecoveryModel.Full, null, CancellationToken.None);

        // Assert
        Assert.Equal(ClientError, result.Error);
        VerifyErrorLogged(RestoreFilesMessage);
    }

    [Fact]
    public async Task RestoreDatabaseFromBackup_WhenDataFolderIsNotSpecified_ReturnsNoDataFolder()
    {
        // Arrange
        SetupHostPlatform(Windows);
        SetupRestoreFiles(@"E:\BAK\Db1_Full.bak");
        SqlServerDatabaseManager sut = CreateSut(_dbClient.Object, _messagesDataManager.Object,
            CreateFoldersSets(WindowsBackupFolder, " ", WindowsDataLogFolder));

        // Act
        Result result = await sut.RestoreDatabaseFromBackup(CreateBackupFileParameters(), DatabaseName,
            FoldersSetName, EDatabaseRecoveryModel.Full, null, CancellationToken.None);

        // Assert
        Assert.Equal(DbClientErrors.NoDataFolder, result.Error);
        VerifyRestoreDatabaseNeverCalled();
    }

    [Fact]
    public async Task RestoreDatabaseFromBackup_WhenDataLogFolderIsNotSpecified_ReturnsNoDataLogFolder()
    {
        // Arrange
        SetupHostPlatform(Windows);
        SetupRestoreFiles(@"E:\BAK\Db1_Full.bak");
        SqlServerDatabaseManager sut = CreateSut(_dbClient.Object, _messagesDataManager.Object,
            CreateFoldersSets(WindowsBackupFolder, WindowsDataFolder, " "));

        // Act
        Result result = await sut.RestoreDatabaseFromBackup(CreateBackupFileParameters(), DatabaseName,
            FoldersSetName, EDatabaseRecoveryModel.Full, null, CancellationToken.None);

        // Assert
        Assert.Equal(DbClientErrors.NoDataLogFolder, result.Error);
        VerifyRestoreDatabaseNeverCalled();
    }

    [Fact]
    public async Task RestoreDatabaseFromBackup_WhenRestoreFails_ReturnsRestoreErrorWithoutChangingRecoveryModel()
    {
        // Arrange
        const string backupFileFullName = @"E:\BAK\Db1_Full.bak";
        SetupHostPlatform(Windows);
        SetupRestoreFiles(backupFileFullName);
        SetupRestoreDatabase(backupFileFullName, WindowsDataFolder, WindowsDataLogFolder, @"\",
            Result.Failure(ClientError));

        // Act
        Result result = await CreateSut().RestoreDatabaseFromBackup(CreateBackupFileParameters(), DatabaseName,
            FoldersSetName, EDatabaseRecoveryModel.Simple, null, CancellationToken.None);

        // Assert
        Assert.Equal(ClientError, result.Error);
        VerifyRecoveryModelNeverChanged();
    }

    [Fact]
    public async Task RestoreDatabaseFromBackup_WithNonFullRecoveryModel_ChangesRecoveryModelAfterRestore()
    {
        // Arrange
        const string backupFileFullName = @"E:\BAK\Db1_Full.bak";
        SetupHostPlatform(Windows);
        SetupRestoreFiles(backupFileFullName);
        SetupRestoreDatabase(backupFileFullName, WindowsDataFolder, WindowsDataLogFolder, @"\", Result.Success());
        _dbClient.Setup(c => c.ChangeDatabaseRecoveryModel(DatabaseName, EDatabaseRecoveryModel.Simple,
            It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        // Act
        Result result = await CreateSut().RestoreDatabaseFromBackup(CreateBackupFileParameters(), DatabaseName,
            FoldersSetName, EDatabaseRecoveryModel.Simple, null, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _dbClient.Verify(c => c.ChangeDatabaseRecoveryModel(DatabaseName, EDatabaseRecoveryModel.Simple,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RestoreDatabaseFromBackup_WhenRecoveryModelChangeFails_ReturnsThatError()
    {
        // Arrange
        const string backupFileFullName = @"E:\BAK\Db1_Full.bak";
        SetupHostPlatform(Windows);
        SetupRestoreFiles(backupFileFullName);
        SetupRestoreDatabase(backupFileFullName, WindowsDataFolder, WindowsDataLogFolder, @"\", Result.Success());
        _dbClient.Setup(c => c.ChangeDatabaseRecoveryModel(DatabaseName, EDatabaseRecoveryModel.BulkLogged,
            It.IsAny<CancellationToken>())).ReturnsAsync(Result.Failure(ClientError));

        // Act
        Result result = await CreateSut().RestoreDatabaseFromBackup(CreateBackupFileParameters(), DatabaseName,
            FoldersSetName, EDatabaseRecoveryModel.BulkLogged, null, CancellationToken.None);

        // Assert
        Assert.Equal(ClientError, result.Error);
    }

    // ---------- CreateBackup ----------

    [Fact]
    public async Task CreateBackup_OnWindowsFullBackup_CreatesCompressedVerifiedBackupInFoldersSetBackupFolder()
    {
        // Arrange
        SetupHostPlatform(Windows);
        SetupBackupDatabase(Result.Success());
        _dbClient.Setup(c => c.VerifyBackup(DatabaseName, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        DateTime startedAt = DateTime.Now;

        // Act
        Result<BackupFileParameters> result = await CreateSut().CreateBackup(
            CreateBackupParameters(EBackupType.Full, true, true), DatabaseName, FoldersSetName,
            CancellationToken.None);

        // Assert
        BackupFileParameters backupFileParameters = result.Value;
        Assert.Equal(WindowsBackupFolder, backupFileParameters.FolderName);
        Assert.Equal(BackupPrefix, backupFileParameters.Prefix);
        Assert.Equal(BackupSuffix, backupFileParameters.Suffix);
        Assert.Equal(DateMask, backupFileParameters.DateMask);
        Assert.InRange(GetBackupFileDate(backupFileParameters.Name), startedAt.AddSeconds(-1), DateTime.Now);

        string backupFileFullName = @"E:\BAK\" + backupFileParameters.Name;
        _dbClient.Verify(c => c.BackupDatabase(DatabaseName, backupFileFullName, "Db1-full", EBackupType.Full, true,
            It.IsAny<CancellationToken>()), Times.Once);
        _dbClient.Verify(c => c.VerifyBackup(DatabaseName, backupFileFullName, It.IsAny<CancellationToken>()),
            Times.Once);
        VerifySingleFactoryCall(null);
    }

    [Fact]
    public async Task CreateBackup_OnLinux_UsesForwardSlashSeparator()
    {
        // Arrange
        SetupHostPlatform(Linux);
        SetupBackupDatabase(Result.Success());
        SqlServerDatabaseManager sut = CreateSut(_dbClient.Object, _messagesDataManager.Object,
            CreateFoldersSets(LinuxBackupFolder));

        // Act
        Result<BackupFileParameters> result = await sut.CreateBackup(
            CreateBackupParameters(EBackupType.Full, true, false), DatabaseName, FoldersSetName,
            CancellationToken.None);

        // Assert
        _dbClient.Verify(c => c.BackupDatabase(DatabaseName, "/var/backup/" + result.Value.Name, "Db1-full",
            EBackupType.Full, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateBackup_TransactionLogBackup_UsesConfiguredBackupTypeAndSkipsVerification()
    {
        // Arrange
        SetupHostPlatform(Windows);
        SetupBackupDatabase(Result.Success());

        // Act
        Result<BackupFileParameters> result = await CreateSut().CreateBackup(
            CreateBackupParameters(EBackupType.TrLog, false, false), DatabaseName, FoldersSetName,
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _dbClient.Verify(c => c.BackupDatabase(DatabaseName, @"E:\BAK\" + result.Value.Name, DatabaseName,
            EBackupType.TrLog, false, It.IsAny<CancellationToken>()), Times.Once);
        VerifyBackupNeverVerified();
    }

    [Fact]
    public async Task CreateBackup_WhenBackupFolderIsNotSpecified_ReturnsNoBackupFolder()
    {
        // Arrange
        SetupHostPlatform(Windows);
        SqlServerDatabaseManager sut =
            CreateSut(_dbClient.Object, _messagesDataManager.Object, CreateFoldersSets(" "));

        // Act
        Result<BackupFileParameters> result = await sut.CreateBackup(
            CreateBackupParameters(EBackupType.Full, true, true), DatabaseName, FoldersSetName,
            CancellationToken.None);

        // Assert
        Assert.Equal(DbClientErrors.NoBackupFolder, result.Error);
        VerifyBackupDatabaseNeverCalled();
    }

    [Fact]
    public async Task CreateBackup_WhenBackupFails_ReturnsBackupErrorWithoutVerification()
    {
        // Arrange
        SetupHostPlatform(Windows);
        SetupBackupDatabase(Result.Failure(ClientError));

        // Act
        Result<BackupFileParameters> result = await CreateSut().CreateBackup(
            CreateBackupParameters(EBackupType.Full, true, true), DatabaseName, FoldersSetName,
            CancellationToken.None);

        // Assert
        Assert.Equal(ClientError, result.Error);
        VerifyBackupNeverVerified();
    }

    [Fact]
    public async Task CreateBackup_WhenVerificationFails_ReturnsVerificationError()
    {
        // Arrange
        SetupHostPlatform(Windows);
        SetupBackupDatabase(Result.Success());
        _dbClient.Setup(c => c.VerifyBackup(DatabaseName, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(ClientError));

        // Act
        Result<BackupFileParameters> result = await CreateSut().CreateBackup(
            CreateBackupParameters(EBackupType.Full, true, true), DatabaseName, FoldersSetName,
            CancellationToken.None);

        // Assert
        Assert.Equal(ClientError, result.Error);
    }

    [Fact]
    public async Task CreateBackup_WhenHostPlatformUnknown_ReturnsErrorWithoutBackup()
    {
        // Arrange
        _dbClient.Setup(c => c.HostPlatform(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<string>(ClientError));

        // Act
        Result<BackupFileParameters> result = await CreateSut().CreateBackup(
            CreateBackupParameters(EBackupType.Full, true, true), DatabaseName, FoldersSetName,
            CancellationToken.None);

        // Assert
        Assert.Equal(ClientError, result.Error);
        VerifyBackupDatabaseNeverCalled();
    }

    [Fact]
    public async Task CreateBackup_WhenDbClientCannotBeCreated_ReturnsError()
    {
        // Act
        Result<BackupFileParameters> result = await CreateSutWithoutDbClient().CreateBackup(
            CreateBackupParameters(EBackupType.Full, true, true), DatabaseName, FoldersSetName,
            CancellationToken.None);

        // Assert
        Assert.Equal(SqlServerDatabaseManagerErrors.CannotCreateDbClient(null), result.Error);
    }

    // ---------- Helpers ----------

    private SqlServerDatabaseManager CreateSut()
    {
        return CreateSut(_dbClient.Object, _messagesDataManager.Object, CreateFoldersSets(WindowsBackupFolder));
    }

    private SqlServerDatabaseManager CreateSutWithoutDbClient()
    {
        return CreateSut(null, _messagesDataManager.Object, CreateFoldersSets(WindowsBackupFolder));
    }

    private SqlServerDatabaseManager CreateSut(DbClient? dbClient, IMessagesDataManager? messagesDataManager,
        Dictionary<string, DatabaseFoldersSet> databaseFoldersSets)
    {
        var databaseServerConnectionDataDomain = new DatabaseServerConnectionDataDomain(EDatabaseProvider.SqlServer,
            "server", new DbAuthSettingsBase(), true, databaseFoldersSets);

        return new SqlServerDatabaseManager(_logger.Object, databaseServerConnectionDataDomain, messagesDataManager,
            UserName, (provider, databaseName) =>
            {
                _factoryCalls.Add((provider, databaseName));
                return dbClient;
            });
    }

    private static Dictionary<string, DatabaseFoldersSet> CreateFoldersSets(string backupFolder)
    {
        return CreateFoldersSets(backupFolder, WindowsDataFolder, WindowsDataLogFolder);
    }

    private static Dictionary<string, DatabaseFoldersSet> CreateFoldersSets(string backupFolder, string dataFolder,
        string dataLogFolder)
    {
        return new Dictionary<string, DatabaseFoldersSet>
        {
            [FoldersSetName] = new() { Backup = backupFolder, Data = dataFolder, DataLog = dataLogFolder }
        };
    }

    private static BackupFileParameters CreateBackupFileParameters()
    {
        return new BackupFileParameters(null, BackupFileName, "Db1_", BackupSuffix, DateMask);
    }

    private static DatabaseBackupParametersDomain CreateBackupParameters(EBackupType backupType, bool compress,
        bool verify)
    {
        return new DatabaseBackupParametersDomain("Pz_", DateMask, "bak", "_Full_", compress, verify, backupType);
    }

    private static DateTime GetBackupFileDate(string backupFileName)
    {
        Assert.StartsWith(BackupPrefix, backupFileName, StringComparison.Ordinal);
        Assert.EndsWith(BackupSuffix, backupFileName, StringComparison.Ordinal);
        return DateTime.ParseExact(backupFileName[BackupPrefix.Length..^BackupSuffix.Length], DateMask,
            CultureInfo.InvariantCulture);
    }

    private void SetupHostPlatform(string hostPlatform)
    {
        _dbClient.Setup(c => c.HostPlatform(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(hostPlatform));
    }

    private void SetupRestoreFiles(string backupFileFullName)
    {
        _dbClient.Setup(c => c.GetRestoreFiles(backupFileFullName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(RestoreFiles));
    }

    private void SetupRestoreDatabase(string backupFileFullName, string dataFolder, string dataLogFolder,
        string dirSeparator, Result result)
    {
        _dbClient.Setup(c => c.RestoreDatabase(DatabaseName, backupFileFullName, RestoreFiles, dataFolder,
            dataLogFolder, dirSeparator, It.IsAny<CancellationToken>())).ReturnsAsync(result);
    }

    private void SetupBackupDatabase(Result result)
    {
        _dbClient.Setup(c => c.BackupDatabase(DatabaseName, It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<EBackupType>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(result);
    }

    private void VerifyRestoreDatabase(string backupFileFullName, string dataFolder, string dataLogFolder,
        string dirSeparator)
    {
        _dbClient.Verify(c => c.RestoreDatabase(DatabaseName, backupFileFullName, RestoreFiles, dataFolder,
            dataLogFolder, dirSeparator, It.IsAny<CancellationToken>()), Times.Once);
    }

    private void VerifyRestoreDatabaseNeverCalled()
    {
        _dbClient.Verify(c => c.RestoreDatabase(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<List<RestoreFileModel>?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private void VerifyRecoveryModelNeverChanged()
    {
        _dbClient.Verify(c => c.ChangeDatabaseRecoveryModel(It.IsAny<string>(), It.IsAny<EDatabaseRecoveryModel>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private void VerifyBackupDatabaseNeverCalled()
    {
        _dbClient.Verify(c => c.BackupDatabase(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<EBackupType>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private void VerifyBackupNeverVerified()
    {
        _dbClient.Verify(c => c.VerifyBackup(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private void VerifySingleFactoryCall(string? expectedDatabaseName)
    {
        (EDatabaseProvider provider, string? databaseName) = Assert.Single(_factoryCalls);
        Assert.Equal(EDatabaseProvider.SqlServer, provider);
        Assert.Equal(expectedDatabaseName, databaseName);
    }

    private void VerifyErrorLogged(string message)
    {
        _logger.Verify(l => l.Log(LogLevel.Error, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, _) => state!.ToString() == message), It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}
