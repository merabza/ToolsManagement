using ParametersManagement.LibFileParameters.Models;
using ToolsManagement.FileManagersMain;

namespace ToolsManagement.DatabasesManagement.Models;

public sealed class BackupRestoreParameters
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public BackupRestoreParameters(IDatabaseManager databaseManager, FileManager fileManager, SmartSchema? smartSchema,
        string databaseName, string dbServerFoldersSetName, FileStorageData fileStorage)
    {
        DatabaseManager = databaseManager;
        FileManager = fileManager;
        SmartSchema = smartSchema;
        DatabaseName = databaseName;
        DbServerFoldersSetName = dbServerFoldersSetName;
        FileStorage = fileStorage;
    }

    public IDatabaseManager DatabaseManager { get; }
    public FileManager FileManager { get; }
    public FileStorageData FileStorage { get; }
    public SmartSchema? SmartSchema { get; }
    public string DatabaseName { get; }
    public string DbServerFoldersSetName { get; set; }
}
