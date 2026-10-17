using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DatabaseTools.DbTools;
using DatabaseTools.DbTools.Models;
using DatabaseTools.DbToolsFactory;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibDatabaseParameters;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;
using ToolsManagement.DatabasesManagement.Errors;
using WebAgentContracts.WebAgentDatabasesApiContracts.V1.Responses;

namespace ToolsManagement.DatabasesManagement;

//MariaDB სერვერის მენეჯერი: კავშირის შემოწმება, სერვერისა და ბაზების ინფორმაცია და ბრძანების გაშვება;
//ბექაპი, აღდგენა და ბაზის მომსახურება MariaDB-სთვის მხარდაჭერილი არ არის
public sealed class MariaDbDatabaseManager : IDatabaseManager
{
    private readonly DatabaseServerConnectionDataDomain _databaseServerConnectionDataDomain;
    private readonly Func<string?, DbClient?> _dbClientFactory;
    private readonly ILogger _logger;

    public MariaDbDatabaseManager(ILogger logger, bool useConsole,
        DatabaseServerConnectionDataDomain databaseServerConnectionDataDomain,
        IMessagesDataManager? messagesDataManager, string? userName) : this(logger,
        databaseServerConnectionDataDomain,
        databaseName => DbClientFactory.GetDbClient(logger, useConsole, EDatabaseProvider.MariaDb,
            databaseServerConnectionDataDomain.ServerAddress, databaseServerConnectionDataDomain.DbAuthSettings,
            databaseServerConnectionDataDomain.TrustServerCertificate, null, databaseName, messagesDataManager,
            userName))
    {
    }

    //internal საჭიროა ტესტებისთვის: DbClient-ის შექმნა შეიძლება ჩანაცვლდეს
    internal MariaDbDatabaseManager(ILogger logger,
        DatabaseServerConnectionDataDomain databaseServerConnectionDataDomain,
        Func<string?, DbClient?> dbClientFactory)
    {
        _logger = logger;
        _databaseServerConnectionDataDomain = databaseServerConnectionDataDomain;
        _dbClientFactory = dbClientFactory;
    }

    //მონაცემთა ბაზების სიის მიღება სერვერიდან
    public async Task<Result<List<DatabaseInfoModel>>> GetDatabaseNames(CancellationToken cancellationToken = default)
    {
        Result<DbClient> getDatabaseClientResult = GetDatabaseClient(null);
        if (getDatabaseClientResult.IsFailure)
        {
            return getDatabaseClientResult.Error;
        }

        return await getDatabaseClientResult.Value.GetDatabaseInfos(cancellationToken);
    }

    public async Task<Result<bool>> IsDatabaseExists(string databaseName, CancellationToken cancellationToken = default)
    {
        Result<DbClient> getDatabaseClientResult = GetDatabaseClient(null);
        if (getDatabaseClientResult.IsFailure)
        {
            return getDatabaseClientResult.Error;
        }

        return await getDatabaseClientResult.Value.IsDatabaseExists(databaseName, cancellationToken);
    }

    //სერვერის მხარეს მონაცემთა ბაზაში ბრძანების გაშვება
    public async ValueTask<Result> ExecuteCommand(string executeQueryCommand, string? databaseName = null,
        CancellationToken cancellationToken = default)
    {
        Result<DbClient> getDatabaseClientResult = GetDatabaseClient(databaseName);
        if (getDatabaseClientResult.IsFailure)
        {
            return getDatabaseClientResult.Error;
        }

        return await getDatabaseClientResult.Value.ExecuteCommand(executeQueryCommand, true, true, cancellationToken);
    }

    public async Task<Result<DbServerInfo>> GetDatabaseServerInfo(CancellationToken cancellationToken = default)
    {
        Result<DbClient> getDatabaseClientResult = GetDatabaseClient(null);
        if (getDatabaseClientResult.IsFailure)
        {
            return getDatabaseClientResult.Error;
        }

        return await getDatabaseClientResult.Value.GetDbServerInfo(cancellationToken);
    }

    public async Task<Result<bool>> IsServerLocal(CancellationToken cancellationToken = default)
    {
        Result<DbClient> getDatabaseClientResult = GetDatabaseClient(null);
        if (getDatabaseClientResult.IsFailure)
        {
            return getDatabaseClientResult.Error;
        }

        return await getDatabaseClientResult.Value.IsServerLocal(cancellationToken);
    }

    public async Task<Result> TestConnection(string? databaseName, CancellationToken cancellationToken = default)
    {
        Result<DbClient> getDatabaseClientResult = GetDatabaseClient(databaseName);
        if (getDatabaseClientResult.IsFailure)
        {
            return getDatabaseClientResult.Error;
        }

        return await getDatabaseClientResult.Value.TestConnection(databaseName is not null, cancellationToken);
    }

    public Task<Result<List<string>>> GetDatabaseFoldersSetNames(CancellationToken cancellationToken)
    {
        return Task.FromResult<Result<List<string>>>(_databaseServerConnectionDataDomain.DatabaseFoldersSets.Keys
            .ToList());
    }

    public ValueTask<Result<BackupFileParameters>> CreateBackup(
        DatabaseBackupParametersDomain databaseBackupParameters, string backupBaseName, string dbServerFoldersSetName,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<Result<BackupFileParameters>>(
            MariaDbDatabaseManagerErrors.OperationIsNotSupported(nameof(CreateBackup)));
    }

    public Task<Result> RestoreDatabaseFromBackup(BackupFileParameters backupFileParameters, string databaseName,
        string dbServerFoldersSetName, EDatabaseRecoveryModel databaseRecoveryModel,
        string? restoreFromFolderPath = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<Result>(
            MariaDbDatabaseManagerErrors.OperationIsNotSupported(nameof(RestoreDatabaseFromBackup)));
    }

    public ValueTask<Result> CheckRepairDatabase(string databaseName, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<Result>(
            MariaDbDatabaseManagerErrors.OperationIsNotSupported(nameof(CheckRepairDatabase)));
    }

    public ValueTask<Result> RecompileProcedures(string databaseName, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<Result>(
            MariaDbDatabaseManagerErrors.OperationIsNotSupported(nameof(RecompileProcedures)));
    }

    public ValueTask<Result> UpdateStatistics(string databaseName, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<Result>(
            MariaDbDatabaseManagerErrors.OperationIsNotSupported(nameof(UpdateStatistics)));
    }

    public Task<Result> SetDefaultFolders(string defBackupFolder, string defDataFolder, string defLogFolder,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<Result>(MariaDbDatabaseManagerErrors.OperationIsNotSupported(nameof(SetDefaultFolders)));
    }

    public ValueTask<Result> ChangeDatabaseRecoveryModel(string databaseName,
        EDatabaseRecoveryModel databaseRecoveryModel, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult<Result>(
            MariaDbDatabaseManagerErrors.OperationIsNotSupported(nameof(ChangeDatabaseRecoveryModel)));
    }

    private Result<DbClient> GetDatabaseClient(string? databaseName)
    {
        DbClient? dc = _dbClientFactory(databaseName);
        if (dc is not null)
        {
            return dc;
        }

        _logger.LogError("Cannot create MariaDB DbClient for database {DatabaseName}", databaseName);
        return MariaDbDatabaseManagerErrors.CannotCreateDbClient(databaseName);
    }
}
