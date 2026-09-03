using SystemTools.SharedKernel;

namespace ToolsManagement.DatabasesManagement.Errors;

public static class DatabaseManagerErrors
{
    public static readonly Error LocalPathIsNotSpecifiedInParameters = Error.Problem(
        nameof(LocalPathIsNotSpecifiedInParameters),
        "localPath is not specified in databasesBackupFilesExchangeParameter");

    public static readonly Error DatabaseNameDoesNotSpecified =
        Error.Problem(nameof(DatabaseNameDoesNotSpecified), "DatabaseName does not specified");

    public static readonly Error FromDatabaseParametersDbServerFoldersSetNameIsNotSpecified =
        Error.Problem(nameof(FromDatabaseParametersDbServerFoldersSetNameIsNotSpecified),
            "fromDatabaseParameters.DbServerFoldersSetName is not specified");

    public static readonly Error CanNotCreateDatabaseServerClient =
        Error.Problem(nameof(CanNotCreateDatabaseServerClient), "Can not create client for source Database server");

    public static readonly Error FileStorageAndFileManagerIsNotCreated =
        Error.Problem(nameof(FileStorageAndFileManagerIsNotCreated),
            "FileStorage and sourceFileManager is Not Created");

    public static readonly Error LocalFileManagerIsNotCreated =
        Error.Problem(nameof(LocalFileManagerIsNotCreated), "localFileManager is not created");
}
