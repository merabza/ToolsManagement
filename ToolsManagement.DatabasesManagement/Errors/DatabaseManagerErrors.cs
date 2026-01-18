using SystemTools.SystemToolsShared.Errors;

namespace ToolsManagement.DatabasesManagement.Errors;

public static class DatabaseManagerErrors
{
    public static readonly Err LocalPathIsNotSpecifiedInParameters = new()
    {
        ErrorCode = nameof(LocalPathIsNotSpecifiedInParameters),
        ErrorMessage = "localPath is not specified in databasesBackupFilesExchangeParameter"
    };

    public static readonly Err DatabaseNameDoesNotSpecified = new()
    {
        ErrorCode = nameof(DatabaseNameDoesNotSpecified), ErrorMessage = "DatabaseName does not specified"
    };

    public static readonly Err FromDatabaseParametersDbServerFoldersSetNameIsNotSpecified = new()
    {
        ErrorCode = nameof(FromDatabaseParametersDbServerFoldersSetNameIsNotSpecified),
        ErrorMessage = "fromDatabaseParameters.DbServerFoldersSetName is not specified"
    };

    public static readonly Err CanNotCreateDatabaseServerClient = new()
    {
        ErrorCode = nameof(FromDatabaseParametersDbServerFoldersSetNameIsNotSpecified),
        ErrorMessage = "Can not create client for source Database server"
    };

    public static readonly Err FileStorageAndFileManagerIsNotCreated = new()
    {
        ErrorCode = nameof(FileStorageAndFileManagerIsNotCreated),
        ErrorMessage = "FileStorage and sourceFileManager is Not Created"
    };

    public static readonly Err LocalFileManagerIsNotCreated = new()
    {
        ErrorCode = nameof(LocalFileManagerIsNotCreated), ErrorMessage = "localFileManager is not created"
    };
}