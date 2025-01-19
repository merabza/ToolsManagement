using SystemToolsShared.Errors;

namespace DatabasesManagement.Errors;

public static class DatabaseManagerErrors
{
    public static readonly Err LocalPathIsNotSpecifiedInParameters = new()
    {
        ErrorCode = nameof(LocalPathIsNotSpecifiedInParameters),
        ErrorMessage = "localPath is not specified in databasesBackupFilesExchangeParameter"
    };

    public static readonly Err SourceDatabaseNameDoesNotSpecified = new()
    {
        ErrorCode = nameof(SourceDatabaseNameDoesNotSpecified),
        ErrorMessage = "sourceDatabaseName does not specified"
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

    public static readonly Err SourceFileStorageAndSourceFileManagerIsNotCreated = new()
    {
        ErrorCode = nameof(SourceFileStorageAndSourceFileManagerIsNotCreated),
        ErrorMessage = "sourceFileStorage and sourceFileManager is Not Created"
    };

    public static readonly Err LocalFileManagerIsNotCreated = new()
    {
        ErrorCode = nameof(LocalFileManagerIsNotCreated), ErrorMessage = "localFileManager is not created"
    };


    //localFileManager is not created


    //public static readonly Err RestoreFilesDoesNotDetected = new()
    //{
    //    ErrorCode = nameof(RestoreFilesDoesNotDetected), ErrorMessage = "Restore Files does not detected"
    //};

    //public static Err CannotCreateDbClient(string? databaseName)
    //{
    //    return new Err
    //    {
    //        ErrorCode = nameof(CannotCreateDbClient),
    //        ErrorMessage = $"Cannot create DbClient for database {databaseName}"
    //    };
    //}
}