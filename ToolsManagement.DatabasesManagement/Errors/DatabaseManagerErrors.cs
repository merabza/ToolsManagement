using SystemTools.SystemToolsShared.Errors;

namespace ToolsManagement.DatabasesManagement.Errors;

public static class DatabaseManagerErrors
{
    public static readonly ErrorOmd LocalPathIsNotSpecifiedInParameters = new()
    {
        Code = nameof(LocalPathIsNotSpecifiedInParameters),
        Name = "localPath is not specified in databasesBackupFilesExchangeParameter"
    };

    public static readonly ErrorOmd DatabaseNameDoesNotSpecified = new()
    {
        Code = nameof(DatabaseNameDoesNotSpecified), Name = "DatabaseName does not specified"
    };

    public static readonly ErrorOmd FromDatabaseParametersDbServerFoldersSetNameIsNotSpecified = new()
    {
        Code = nameof(FromDatabaseParametersDbServerFoldersSetNameIsNotSpecified),
        Name = "fromDatabaseParameters.DbServerFoldersSetName is not specified"
    };

    public static readonly ErrorOmd CanNotCreateDatabaseServerClient = new()
    {
        Code = nameof(CanNotCreateDatabaseServerClient), Name = "Can not create client for source Database server"
    };

    public static readonly ErrorOmd FileStorageAndFileManagerIsNotCreated = new()
    {
        Code = nameof(FileStorageAndFileManagerIsNotCreated),
        Name = "FileStorage and sourceFileManager is Not Created"
    };

    public static readonly ErrorOmd LocalFileManagerIsNotCreated = new()
    {
        Code = nameof(LocalFileManagerIsNotCreated), Name = "localFileManager is not created"
    };
}
