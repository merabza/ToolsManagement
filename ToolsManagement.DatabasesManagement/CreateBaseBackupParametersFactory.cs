using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DatabaseTools.DbTools;
using Microsoft.Extensions.Logging;
using OneOf;
using ParametersManagement.LibApiClientParameters;
using ParametersManagement.LibDatabaseParameters;
using ParametersManagement.LibFileParameters.Models;
using SystemTools.SystemToolsShared;
using SystemTools.SystemToolsShared.Errors;
using ToolsManagement.DatabasesManagement.Errors;
using ToolsManagement.DatabasesManagement.Models;
using ToolsManagement.FileManagersMain;

namespace ToolsManagement.DatabasesManagement;

public sealed class CreateBaseBackupParametersFactory : MessageLogger
{
    private readonly ILogger _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public CreateBaseBackupParametersFactory(ILogger logger, IMessagesDataManager? messagesDataManager,
        string? userName, bool useConsole) : base(logger, messagesDataManager, userName, useConsole)
    {
        _logger = logger;
    }

    public async Task<OneOf<BaseBackupParameters, Err[]>> CreateBaseBackupParameters(
        IHttpClientFactory httpClientFactory, DatabaseParameters fromDatabaseParameters,
        DatabaseServerConnections databaseServerConnections, ApiClients apiClients, FileStorages fileStorages,
        SmartSchemas smartSchemas, DatabasesBackupFilesExchangeParameters? databasesBackupFilesExchangeParameters,
        CancellationToken cancellationToken = default)
    {
        string? localPath = databasesBackupFilesExchangeParameters?.LocalPath;
        string? localSmartSchemaName = databasesBackupFilesExchangeParameters?.LocalSmartSchemaName;
        string? exchangeFileStorageName = databasesBackupFilesExchangeParameters?.ExchangeFileStorageName;

        string? uploadTempExtension =
            string.IsNullOrWhiteSpace(databasesBackupFilesExchangeParameters?.UploadTempExtension)
                ? DatabasesBackupFilesExchangeParameters.DefaultUploadTempExtension
                : databasesBackupFilesExchangeParameters.UploadTempExtension;

        string? downloadTempExtension =
            string.IsNullOrWhiteSpace(databasesBackupFilesExchangeParameters?.DownloadTempExtension)
                ? DatabasesBackupFilesExchangeParameters.DefaultDownloadTempExtension
                : databasesBackupFilesExchangeParameters.DownloadTempExtension;

        string? dbConnectionName = fromDatabaseParameters.DbConnectionName;
        string? fileStorageName = fromDatabaseParameters.FileStorageName;
        string? smartSchemaName = fromDatabaseParameters.SmartSchemaName;
        string? databaseName = fromDatabaseParameters.DatabaseName;
        bool skipBackupBeforeRestore = fromDatabaseParameters.SkipBackupBeforeRestore;
        string backupNamePrefix = fromDatabaseParameters.BackupNamePrefix ?? $"{Environment.MachineName}_";
        string dateMask = fromDatabaseParameters.DateMask ?? DatabaseParameters.DefaultDateMask;
        string backupFileExtension =
            fromDatabaseParameters.BackupFileExtension ?? DatabaseParameters.DefaultBackupFileExtension;
        string backupNameMiddlePart = fromDatabaseParameters.BackupNameMiddlePart ??
                                      DatabaseParameters.DefaultBackupNameMiddlePart;
        EDatabaseRecoveryModel databaseRecoveryModel = fromDatabaseParameters.DatabaseRecoveryModel ??
                                                       DatabaseParameters.DefaultDatabaseRecoveryModel;
        bool compress = fromDatabaseParameters.Compress ?? DatabaseParameters.DefaultCompress;
        bool verify = fromDatabaseParameters.Verify ?? DatabaseParameters.DefaultVerify;
        EBackupType backupType = fromDatabaseParameters.BackupType ?? DatabaseParameters.DefaultBackupType;

        var errors = new List<Err>();
        if (string.IsNullOrWhiteSpace(localPath))
        {
            errors.AddRange(
                await LogErrorAndSendMessageFromError(DatabaseManagerErrors.LocalPathIsNotSpecifiedInParameters,
                    cancellationToken));
        }

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            errors.AddRange(await LogErrorAndSendMessageFromError(DatabaseManagerErrors.DatabaseNameDoesNotSpecified,
                cancellationToken));
        }

        if (string.IsNullOrWhiteSpace(fromDatabaseParameters.DbServerFoldersSetName))
        {
            errors.AddRange(await LogErrorAndSendMessageFromError(
                DatabaseManagerErrors.FromDatabaseParametersDbServerFoldersSetNameIsNotSpecified, cancellationToken));
        }

        SmartSchema? smartSchema = string.IsNullOrWhiteSpace(smartSchemaName)
            ? null
            : smartSchemas.GetSmartSchemaByKey(smartSchemaName);

        //DbWebAgentName
        //პარამეტრების მიხედვით ბაზის სარეზერვო ასლის დამზადება და მოქაჩვა
        //წყაროს სერვერის აგენტის შექმნა
        OneOf<IDatabaseManager, Err[]> createDatabaseManagerResult =
            await DatabaseManagersFactory.CreateDatabaseManager(_logger, UseConsole, dbConnectionName,
                databaseServerConnections, apiClients, httpClientFactory, null, null, cancellationToken);

        if (createDatabaseManagerResult.IsT1)
        {
            errors.AddRange(createDatabaseManagerResult.AsT1);
            errors.AddRange(await LogErrorAndSendMessageFromError(
                DatabaseManagerErrors.CanNotCreateDatabaseServerClient, cancellationToken));
        }

        if (errors.Count > 0)
        {
            return errors.ToArray();
        }

        (FileStorageData? fileStorage, FileManager? fileManager) =
            await FileManagersFactoryExt.CreateFileStorageAndFileManager(true, _logger, localPath!, fileStorageName,
                fileStorages, null, null, cancellationToken);

        if (fileManager == null || fileStorage == null)
        {
            return await LogErrorAndSendMessageFromError(DatabaseManagerErrors.FileStorageAndFileManagerIsNotCreated,
                cancellationToken);
        }

        var backupRestoreParameters = new BackupRestoreParameters(createDatabaseManagerResult.AsT0, fileManager,
            smartSchema, databaseName!, fromDatabaseParameters.DbServerFoldersSetName!, fileStorage);

        bool needDownload = !FileStorageData.IsSameToLocal(fileStorage, localPath!);

        FileManager? localFileManager = FileManagersFactory.CreateFileManager(true, _logger, localPath!);

        if (localFileManager == null)
        {
            return await LogErrorAndSendMessageFromError(DatabaseManagerErrors.LocalFileManagerIsNotCreated,
                cancellationToken);
        }

        SmartSchema? localSmartSchema = string.IsNullOrWhiteSpace(localSmartSchemaName)
            ? null
            : smartSchemas.GetSmartSchemaByKey(localSmartSchemaName);

        //თუ გაცვლის სერვერის პარამეტრები გვაქვს,
        //შევქმნათ შესაბამისი ფაილმენეჯერი
        Console.Write($" exchangeFileStorage - {exchangeFileStorageName}");
        (FileStorageData? exchangeFileStorage, FileManager? exchangeFileManager) =
            await FileManagersFactoryExt.CreateFileStorageAndFileManager(true, _logger, localPath!,
                exchangeFileStorageName, fileStorages, null, null, cancellationToken);

        bool needUploadToExchange = exchangeFileManager is not null && exchangeFileStorage is not null &&
                                    !FileStorageData.IsSameToLocal(exchangeFileStorage, localPath!);

        return new BaseBackupParameters(backupRestoreParameters, databaseRecoveryModel, needDownload,
            downloadTempExtension, localFileManager, localSmartSchema, needUploadToExchange, exchangeFileManager,
            uploadTempExtension, localPath!, skipBackupBeforeRestore, backupNamePrefix, dateMask, backupFileExtension,
            backupNameMiddlePart, compress, verify, backupType);
    }
}
