using SystemTools.SystemToolsShared.Errors;

namespace ToolsManagement.DatabasesManagement.Errors;

public static class DatabaseManagerErrors
{
    public static readonly Error LocalPathIsNotSpecifiedInParameters = new()
    {
        Code = nameof(LocalPathIsNotSpecifiedInParameters),
        Name = "localPath is not specified in databasesBackupFilesExchangeParameter"
    };

    public static readonly Error DatabaseNameDoesNotSpecified = new()
    {
        Code = nameof(DatabaseNameDoesNotSpecified), Name = "DatabaseName does not specified"
    };

    public static readonly Error FromDatabaseParametersDbServerFoldersSetNameIsNotSpecified = new()
    {
        Code = nameof(FromDatabaseParametersDbServerFoldersSetNameIsNotSpecified),
        Name = "fromDatabaseParameters.DbServerFoldersSetName is not specified"
    };

    public static readonly Error CanNotCreateDatabaseServerClient = new()
    {
        Code = nameof(CanNotCreateDatabaseServerClient), Name = "Can not create client for source Database server"
    };

    public static readonly Error FileStorageAndFileManagerIsNotCreated = new()
    {
        Code = nameof(FileStorageAndFileManagerIsNotCreated),
        Name = "FileStorage and sourceFileManager is Not Created"
    };

    public static readonly Error LocalFileManagerIsNotCreated = new()
    {
        Code = nameof(LocalFileManagerIsNotCreated), Name = "localFileManager is not created"
    };
}
