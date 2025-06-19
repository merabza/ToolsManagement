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

public sealed class CreateBaseBackupParametersFactory : MessageLogger
{
    private readonly ILogger _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public CreateBaseBackupParametersFactory(ILogger logger, IMessagesDataManager? messagesDataManager,
        string? userName, bool useConsole) : base(logger, messagesDataManager, userName, useConsole)
    {
        _logger = logger;
    }

    public async Task<OneOf<BaseBackupParameters, IEnumerable<Err>>> CreateBaseBackupParameters(
        IHttpClientFactory httpClientFactory, DatabaseParameters fromDatabaseParameters,
        DatabaseServerConnections databaseServerConnections, ApiClients apiClients, FileStorages fileStorages,
        SmartSchemas smartSchemas, DatabasesBackupFilesExchangeParameters? databasesBackupFilesExchangeParameters,
        CancellationToken cancellationToken = default)
    {
        var localPath = databasesBackupFilesExchangeParameters?.LocalPath;
        var localSmartSchemaName = databasesBackupFilesExchangeParameters?.LocalSmartSchemaName;
        var exchangeFileStorageName = databasesBackupFilesExchangeParameters?.ExchangeFileStorageName;

        var uploadTempExtension = string.IsNullOrWhiteSpace(databasesBackupFilesExchangeParameters?.UploadTempExtension)
            ? DatabasesBackupFilesExchangeParameters.DefaultUploadTempExtension
            : databasesBackupFilesExchangeParameters.UploadTempExtension;

        var downloadTempExtension =
            string.IsNullOrWhiteSpace(databasesBackupFilesExchangeParameters?.DownloadTempExtension)
                ? DatabasesBackupFilesExchangeParameters.DefaultDownloadTempExtension
                : databasesBackupFilesExchangeParameters.DownloadTempExtension;

        var dbConnectionName = fromDatabaseParameters.DbConnectionName;
        var fileStorageName = fromDatabaseParameters.FileStorageName;
        var smartSchemaName = fromDatabaseParameters.SmartSchemaName;
        var databaseName = fromDatabaseParameters.DatabaseName;
        var skipBackupBeforeRestore = fromDatabaseParameters.SkipBackupBeforeRestore;
        var backupNamePrefix = fromDatabaseParameters.BackupNamePrefix ?? $"{Environment.MachineName}_";
        var dateMask = fromDatabaseParameters.DateMask ?? DatabaseParameters.DefaultDateMask;
        var backupFileExtension =
            fromDatabaseParameters.BackupFileExtension ?? DatabaseParameters.DefaultBackupFileExtension;
        var backupNameMiddlePart = fromDatabaseParameters.BackupNameMiddlePart ??
                                   DatabaseParameters.DefaultBackupNameMiddlePart;
        var databaseRecoveryModel = fromDatabaseParameters.DatabaseRecoveryModel ??
                                    DatabaseParameters.DefaultDatabaseRecoveryModel;
        var compress = fromDatabaseParameters.Compress ?? DatabaseParameters.DefaultCompress;
        var verify = fromDatabaseParameters.Verify ?? DatabaseParameters.DefaultVerify;
        var backupType = fromDatabaseParameters.BackupType ?? DatabaseParameters.DefaultBackupType;

        var errors = new List<Err>();
        if (string.IsNullOrWhiteSpace(localPath))
            errors.AddRange(
                await LogErrorAndSendMessageFromError(DatabaseManagerErrors.LocalPathIsNotSpecifiedInParameters,
                    cancellationToken));

        if (string.IsNullOrWhiteSpace(databaseName))
            errors.AddRange(await LogErrorAndSendMessageFromError(DatabaseManagerErrors.DatabaseNameDoesNotSpecified,
                cancellationToken));

        if (string.IsNullOrWhiteSpace(fromDatabaseParameters.DbServerFoldersSetName))
            errors.AddRange(await LogErrorAndSendMessageFromError(
                DatabaseManagerErrors.FromDatabaseParametersDbServerFoldersSetNameIsNotSpecified, cancellationToken));

        var smartSchema = string.IsNullOrWhiteSpace(smartSchemaName)
            ? null
            : smartSchemas.GetSmartSchemaByKey(smartSchemaName);

        //DbWebAgentName
        //პარამეტრების მიხედვით ბაზის სარეზერვო ასლის დამზადება და მოქაჩვა
        //წყაროს სერვერის აგენტის შექმნა
        var createDatabaseManagerResult = await DatabaseManagersFactory.CreateDatabaseManager(_logger, true,
            dbConnectionName, databaseServerConnections, apiClients, httpClientFactory, null, null, cancellationToken);

        if (createDatabaseManagerResult.IsT1)
        {
            errors.AddRange(createDatabaseManagerResult.AsT1);
            errors.AddRange(await LogErrorAndSendMessageFromError(
                DatabaseManagerErrors.CanNotCreateDatabaseServerClient, cancellationToken));
        }

        if (errors.Count > 0)
            return errors;

        var (fileStorage, fileManager) = await FileManagersFactoryExt.CreateFileStorageAndFileManager(true, _logger,
            localPath!, fileStorageName, fileStorages, null, null, cancellationToken);

        if (fileManager == null || fileStorage == null)
            return (Err[])await LogErrorAndSendMessageFromError(
                DatabaseManagerErrors.FileStorageAndFileManagerIsNotCreated, cancellationToken);

        var backupRestoreParameters = new BackupRestoreParameters(createDatabaseManagerResult.AsT0, fileManager,
            smartSchema, databaseName!, fromDatabaseParameters.DbServerFoldersSetName!, fileStorage);

        var needDownload = !FileStorageData.IsSameToLocal(fileStorage, localPath!);

        var localFileManager = FileManagersFactory.CreateFileManager(true, _logger, localPath!);

        if (localFileManager == null)
            return (Err[])await LogErrorAndSendMessageFromError(DatabaseManagerErrors.LocalFileManagerIsNotCreated,
                cancellationToken);

        var localSmartSchema = string.IsNullOrWhiteSpace(localSmartSchemaName)
            ? null
            : smartSchemas.GetSmartSchemaByKey(localSmartSchemaName);

        //თუ გაცვლის სერვერის პარამეტრები გვაქვს,
        //შევქმნათ შესაბამისი ფაილმენეჯერი
        Console.Write($" exchangeFileStorage - {exchangeFileStorageName}");
        var (exchangeFileStorage, exchangeFileManager) = await FileManagersFactoryExt.CreateFileStorageAndFileManager(
            true, _logger, localPath!, exchangeFileStorageName, fileStorages, null, null, cancellationToken);

        var needUploadToExchange = exchangeFileManager is not null && exchangeFileStorage is not null &&
                                   !FileStorageData.IsSameToLocal(exchangeFileStorage, localPath!);

        return new BaseBackupParameters(backupRestoreParameters, databaseRecoveryModel, needDownload,
            downloadTempExtension, localFileManager, localSmartSchema, needUploadToExchange, exchangeFileManager,
            uploadTempExtension, localPath!, skipBackupBeforeRestore, backupNamePrefix, dateMask, backupFileExtension,
            backupNameMiddlePart, compress, verify, backupType);
    }
}