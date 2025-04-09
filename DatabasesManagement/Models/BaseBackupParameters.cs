using DbTools;
using FileManagersMain;
using LibFileParameters.Models;

namespace DatabasesManagement.Models;

public sealed class BaseBackupParameters
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public BaseBackupParameters(BackupRestoreParameters backupRestoreParameters,
        EDatabaseRecoveryModel databaseRecoveryModel, bool needDownload, string downloadTempExtension,
        FileManager localFileManager, SmartSchema? localSmartSchema, bool needUploadToExchange,
        FileManager? exchangeFileManager, string uploadTempExtension, string localPath, bool skipBackupBeforeRestore,
        string backupNamePrefix, string dateMask, string backupFileExtension, string backupNameMiddlePart,
        bool compress, bool verify, EBackupType backupType)
    {
        BackupRestoreParameters = backupRestoreParameters;
        DatabaseRecoveryModel = databaseRecoveryModel;
        NeedDownload = needDownload;
        DownloadTempExtension = downloadTempExtension;
        LocalFileManager = localFileManager;
        LocalSmartSchema = localSmartSchema;
        NeedUploadToExchange = needUploadToExchange;
        ExchangeFileManager = exchangeFileManager;
        UploadTempExtension = uploadTempExtension;
        LocalPath = localPath;
        SkipBackupBeforeRestore = skipBackupBeforeRestore;
        BackupNamePrefix = backupNamePrefix;
        DateMask = dateMask;
        BackupFileExtension = backupFileExtension;
        BackupNameMiddlePart = backupNameMiddlePart;
        Compress = compress;
        Verify = verify;
        BackupType = backupType;
    }

    public BackupRestoreParameters BackupRestoreParameters { get; }
    public EDatabaseRecoveryModel DatabaseRecoveryModel { get; }
    public bool NeedDownload { get; }
    public string DownloadTempExtension { get; }
    public FileManager LocalFileManager { get; }
    public SmartSchema? LocalSmartSchema { get; }
    public bool NeedUploadToExchange { get; }
    public FileManager? ExchangeFileManager { get; }
    public string UploadTempExtension { get; }
    public string LocalPath { get; }
    public bool SkipBackupBeforeRestore { get; }

    public string BackupNamePrefix { get; }
    public string DateMask { get; }
    public string BackupFileExtension { get; }
    public string BackupNameMiddlePart { get; }
    public bool Compress { get; }
    public bool Verify { get; }
    public EBackupType BackupType { get; }
}