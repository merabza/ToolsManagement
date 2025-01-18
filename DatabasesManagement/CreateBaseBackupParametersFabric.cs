using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DatabasesManagement.Models;
using FileManagersMain;
using LibApiClientParameters;
using LibDatabaseParameters;
using LibFileParameters.Models;
using Microsoft.Extensions.Logging;
using SystemToolsShared;
using SystemToolsShared.Errors;

namespace DatabasesManagement;

public static class CreateBaseBackupParametersFabric
{
    public static async Task<BaseBackupParameters?> CreateBaseBackupParameters(ILogger logger,
        IHttpClientFactory httpClientFactory, DatabasesParameters fromDatabaseParameters,
        DatabaseServerConnections databaseServerConnections, ApiClients apiClients, FileStorages fileStorages,
        SmartSchemas smartSchemas, string? localPath, string? downloadTempExtension, string? localSmartSchemaName,
        string? exchangeFileStorageName, string? uploadTempExtension)
    {
        var sourceDbConnectionName = fromDatabaseParameters.DbConnectionName;
        var sourceFileStorageName = fromDatabaseParameters.FileStorageName;
        var sourceSmartSchemaName = fromDatabaseParameters.SmartSchemaName;
        var sourceDatabaseName = fromDatabaseParameters.DatabaseName;

        if (string.IsNullOrWhiteSpace(localPath))
        {
            StShared.WriteErrorLine("localPath does not specified in databasesBackupFilesExchangeParameters", true);
            return null;
        }

        if (string.IsNullOrWhiteSpace(sourceDatabaseName))
        {
            logger.LogError("sourceDatabaseName does not specified");
            return null;
        }

        if (string.IsNullOrWhiteSpace(fromDatabaseParameters.DbServerFoldersSetName))
        {
            logger.LogError("fromDatabaseParameters.DbServerFoldersSetName is not specified");
            return null;
        }

        var sourceSmartSchema = string.IsNullOrWhiteSpace(sourceSmartSchemaName)
            ? null
            : smartSchemas.GetSmartSchemaByKey(sourceSmartSchemaName);

        //sourceDbWebAgentName
        //პარამეტრების მიხედვით ბაზის სარეზერვო ასლის დამზადება და მოქაჩვა
        //წყაროს სერვერის აგენტის შექმნა
        var createDatabaseManagerResultForSource = await DatabaseManagersFabric.CreateDatabaseManager(logger,
            httpClientFactory, true, sourceDbConnectionName, databaseServerConnections, apiClients, null, null,
            CancellationToken.None);

        if (createDatabaseManagerResultForSource.IsT1)
        {
            Err.PrintErrorsOnConsole(createDatabaseManagerResultForSource.AsT1);
            logger.LogError("Can not create client for source Database server");
            return null;
        }

        var (sourceFileStorage, sourceFileManager) = await FileManagersFabricExt.CreateFileStorageAndFileManager(true,
            logger, localPath, sourceFileStorageName, fileStorages, null, null, CancellationToken.None);

        if (sourceFileManager == null)
        {
            logger.LogError("sourceFileManager does Not Created");
            return null;
        }

        if (sourceFileStorage == null)
        {
            logger.LogError("sourceFileStorage does Not Created");
            return null;
        }

        var sourceBackupRestoreParameters = new BackupRestoreParameters(createDatabaseManagerResultForSource.AsT0,
            sourceFileManager, sourceSmartSchema, sourceDatabaseName, fromDatabaseParameters.DbServerFoldersSetName,
            sourceFileStorage);

        var needDownloadFromSource = !FileStorageData.IsSameToLocal(sourceFileStorage, localPath);

        var localFileManager = FileManagersFabric.CreateFileManager(true, logger, localPath);

        if (localFileManager == null)
        {
            logger.LogError("localFileManager does not created");
            return null;
        }

        var localSmartSchema = string.IsNullOrWhiteSpace(localSmartSchemaName)
            ? null
            : smartSchemas.GetSmartSchemaByKey(localSmartSchemaName);


        //თუ გაცვლის სერვერის პარამეტრები გვაქვს,
        //შევქმნათ შესაბამისი ფაილმენეჯერი
        Console.Write($" exchangeFileStorage - {exchangeFileStorageName}");
        var (exchangeFileStorage, exchangeFileManager) = await FileManagersFabricExt.CreateFileStorageAndFileManager(
            true, logger, localPath, exchangeFileStorageName, fileStorages, null, null, CancellationToken.None);

        var needUploadToExchange = exchangeFileManager is not null && exchangeFileStorage is not null &&
                                   !FileStorageData.IsSameToLocal(exchangeFileStorage, localPath);


        return new BaseBackupParameters(sourceBackupRestoreParameters, needDownloadFromSource,
            string.IsNullOrWhiteSpace(downloadTempExtension) ? "down!" : downloadTempExtension, localFileManager,
            localSmartSchema, needUploadToExchange, exchangeFileManager,
            string.IsNullOrWhiteSpace(uploadTempExtension) ? "up!" : uploadTempExtension, localPath);
    }
}