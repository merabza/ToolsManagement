using FileManagersMain;
using LibDatabaseParameters;
using LibFileParameters.Models;

namespace DatabasesManagement.Models;

public class BaseBackupParameters
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public BaseBackupParameters(BackupRestoreParameters sourceBackupRestoreParameters, bool needDownloadFromSource,
        string downloadTempExtension, FileManager localFileManager, SmartSchema? localSmartSchema,
        bool needUploadToExchange, FileManager? exchangeFileManager, string uploadTempExtension, string localPath,
        bool skipBackupBeforeRestore, DatabaseBackupParametersDomain databaseBackupParameters)
    {
        BackupRestoreParameters = sourceBackupRestoreParameters;
        NeedDownloadFromSource = needDownloadFromSource;
        DownloadTempExtension = downloadTempExtension;
        LocalFileManager = localFileManager;
        LocalSmartSchema = localSmartSchema;
        NeedUploadToExchange = needUploadToExchange;
        ExchangeFileManager = exchangeFileManager;
        UploadTempExtension = uploadTempExtension;
        LocalPath = localPath;
        SkipBackupBeforeRestore = skipBackupBeforeRestore;
        DatabaseBackupParameters = databaseBackupParameters;
    }

    public BackupRestoreParameters BackupRestoreParameters { get; }
    public bool NeedDownloadFromSource { get; }
    public string DownloadTempExtension { get; }
    public FileManager LocalFileManager { get; }
    public SmartSchema? LocalSmartSchema { get; }
    public bool NeedUploadToExchange { get; }
    public FileManager? ExchangeFileManager { get; }
    public string UploadTempExtension { get; }
    public string LocalPath { get; }
    public bool SkipBackupBeforeRestore { get; }
    public DatabaseBackupParametersDomain DatabaseBackupParameters { get; }
}