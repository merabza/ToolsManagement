using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DatabasesManagement.Errors;
using DatabasesManagement.Models;
using FileManagersMain;
using LibApiClientParameters;
using LibDatabaseParameters;
using LibFileParameters.Models;
using Microsoft.Extensions.Logging;
using OneOf;
using SystemToolsShared;
using SystemToolsShared.Errors;

namespace DatabasesManagement;

public class CreateBaseBackupParametersFabric : MessageLogger
{
    private readonly ILogger _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public CreateBaseBackupParametersFabric(ILogger logger, IMessagesDataManager? messagesDataManager, string? userName,
        bool useConsole) : base(logger, messagesDataManager, userName, useConsole)
    {
        _logger = logger;
    }

    public async Task<OneOf<BaseBackupParameters, IEnumerable<Err>>> CreateBaseBackupParameters(
        IHttpClientFactory httpClientFactory, DatabaseParameters fromDatabaseParameters,
        DatabaseServerConnections databaseServerConnections, ApiClients apiClients, FileStorages fileStorages,
        SmartSchemas smartSchemas, string? localPath, string? downloadTempExtension, string? localSmartSchemaName,
        string? exchangeFileStorageName, string? uploadTempExtension, CancellationToken cancellationToken = default)
    {
        var sourceDbConnectionName = fromDatabaseParameters.DbConnectionName;
        var sourceFileStorageName = fromDatabaseParameters.FileStorageName;
        var sourceSmartSchemaName = fromDatabaseParameters.SmartSchemaName;
        var sourceDatabaseName = fromDatabaseParameters.DatabaseName;
        var skipBackupBeforeRestore = fromDatabaseParameters.SkipBackupBeforeRestore;


        var errors = new List<Err>();
        if (string.IsNullOrWhiteSpace(localPath))
            errors.AddRange(
                await LogErrorAndSendMessageFromError(DatabaseManagerErrors.LocalPathIsNotSpecifiedInParameters,
                    cancellationToken));

        if (string.IsNullOrWhiteSpace(sourceDatabaseName))
            errors.AddRange(
                await LogErrorAndSendMessageFromError(DatabaseManagerErrors.SourceDatabaseNameDoesNotSpecified,
                    cancellationToken));

        if (string.IsNullOrWhiteSpace(fromDatabaseParameters.DbServerFoldersSetName))
            errors.AddRange(await LogErrorAndSendMessageFromError(
                DatabaseManagerErrors.FromDatabaseParametersDbServerFoldersSetNameIsNotSpecified, cancellationToken));

        var sourceSmartSchema = string.IsNullOrWhiteSpace(sourceSmartSchemaName)
            ? null
            : smartSchemas.GetSmartSchemaByKey(sourceSmartSchemaName);

        //sourceDbWebAgentName
        //პარამეტრების მიხედვით ბაზის სარეზერვო ასლის დამზადება და მოქაჩვა
        //წყაროს სერვერის აგენტის შექმნა
        var createDatabaseManagerResultForSource = await DatabaseManagersFabric.CreateDatabaseManager(_logger, true,
            sourceDbConnectionName, databaseServerConnections, apiClients, httpClientFactory, null, null,
            CancellationToken.None);

        if (createDatabaseManagerResultForSource.IsT1)
        {
            errors.AddRange(createDatabaseManagerResultForSource.AsT1);
            errors.AddRange(await LogErrorAndSendMessageFromError(
                DatabaseManagerErrors.CanNotCreateDatabaseServerClient, cancellationToken));
        }

        if (errors.Count > 0)
            return errors;

        var (sourceFileStorage, sourceFileManager) = await FileManagersFabricExt.CreateFileStorageAndFileManager(true,
            _logger, localPath!, sourceFileStorageName, fileStorages, null, null, CancellationToken.None);

        if (sourceFileManager == null || sourceFileStorage == null)
            return await LogErrorAndSendMessageFromError(
                DatabaseManagerErrors.SourceFileStorageAndSourceFileManagerIsNotCreated, cancellationToken);

        var sourceBackupRestoreParameters = new BackupRestoreParameters(createDatabaseManagerResultForSource.AsT0,
            sourceFileManager, sourceSmartSchema, sourceDatabaseName!, fromDatabaseParameters.DbServerFoldersSetName!,
            sourceFileStorage);

        var needDownloadFromSource = !FileStorageData.IsSameToLocal(sourceFileStorage, localPath!);

        var localFileManager = FileManagersFabric.CreateFileManager(true, _logger, localPath!);

        if (localFileManager == null)
            return await LogErrorAndSendMessageFromError(DatabaseManagerErrors.LocalFileManagerIsNotCreated,
                cancellationToken);

        var localSmartSchema = string.IsNullOrWhiteSpace(localSmartSchemaName)
            ? null
            : smartSchemas.GetSmartSchemaByKey(localSmartSchemaName);


        //თუ გაცვლის სერვერის პარამეტრები გვაქვს,
        //შევქმნათ შესაბამისი ფაილმენეჯერი
        Console.Write($" exchangeFileStorage - {exchangeFileStorageName}");
        var (exchangeFileStorage, exchangeFileManager) = await FileManagersFabricExt.CreateFileStorageAndFileManager(
            true, _logger, localPath!, exchangeFileStorageName, fileStorages, null, null, CancellationToken.None);

        var needUploadToExchange = exchangeFileManager is not null && exchangeFileStorage is not null &&
                                   !FileStorageData.IsSameToLocal(exchangeFileStorage, localPath!);


        return new BaseBackupParameters(sourceBackupRestoreParameters, needDownloadFromSource,
            string.IsNullOrWhiteSpace(downloadTempExtension) ? "down!" : downloadTempExtension, localFileManager,
            localSmartSchema, needUploadToExchange, exchangeFileManager,
            string.IsNullOrWhiteSpace(uploadTempExtension) ? "up!" : uploadTempExtension, localPath!,
            skipBackupBeforeRestore);
    }
}