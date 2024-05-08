using SystemToolsShared;

namespace Installer.ErrorModels;

public static class InstallerErrors
{
    public static Err CannotUpdateProject(string projectName, string environmentName) => new()
    {
        ErrorCode = nameof(CannotUpdateProject),
        ErrorMessage = $"Cannot Update {projectName}/{environmentName}"
    };

    public static Err ProjectArchiveFileWasNotDownloaded => new()
    {
        ErrorCode = nameof(ProjectArchiveFileWasNotDownloaded),
        ErrorMessage = "Project archive file not downloaded"
    };

    public static Err ProjectArchiveFilesNotFoundOnExchangeStorage => new()
    {
        ErrorCode = nameof(ProjectArchiveFilesNotFoundOnExchangeStorage),
        ErrorMessage = "Project archive files not found on exchange storage"
    };

    public static Err CannotUpdateSelf => new()
    {
        ErrorCode = nameof(CannotUpdateSelf),
        ErrorMessage = "Cannot update self"
    };

    public static Err ExchangeFileManagerIsNull => new()
    {
        ErrorCode = nameof(ExchangeFileManagerIsNull),
        ErrorMessage = "exchangeFileManager is null in UpdateProgramWithParameters"
    };

    public static Err CannotRegisterService(string serviceEnvName) => new()
    {
        ErrorCode = nameof(ExchangeFileManagerIsNull),
        ErrorMessage = $"cannot register Service {serviceEnvName}"
    };

    public static Err FileCanNotBeDeleted(string fileName) => new()
    {
        ErrorCode = nameof(FileCanNotBeDeleted),
        ErrorMessage = $"File {fileName} can not Deleted"
    };

    public static Err FileIsNotExists(string fileName) => new()
    {
        ErrorCode = nameof(FileIsNotExists),
        ErrorMessage = $"File {fileName} is not exists"
    };

    public static Err FileNameIsEmpty => new()
    {
        ErrorCode = nameof(FileNameIsEmpty),
        ErrorMessage = "File name is empty"
    };

    public static Err FileOwnerCanNotBeChanged(string fileName) => new()
    {
        ErrorCode = nameof(FileOwnerCanNotBeChanged),
        ErrorMessage = $"File {fileName} owner can not be changed"
    };

    public static Err FolderCanNotBeDeleted(string folderName) => new()
    {
        ErrorCode = nameof(FileCanNotBeDeleted),
        ErrorMessage = $"Folder {folderName} can not be Deleted"
    };

    public static Err FolderIsNotExists(string folderName) => new()
    {
        ErrorCode = nameof(FolderIsNotExists),
        ErrorMessage = $"File {folderName} is not exists"
    };

    public static Err FolderNameIsEmpty => new()
    {
        ErrorCode = nameof(FolderNameIsEmpty),
        ErrorMessage = "Folder name is empty"
    };

    public static Err FolderOwnerCanNotBeChanged(string folderName) => new()
    {
        ErrorCode = nameof(FolderOwnerCanNotBeChanged),
        ErrorMessage = $"Folder {folderName} owner can not be changed"
    };

    public static Err InstallerFolderIsNotExists(string folderName) => new()
    {
        ErrorCode = nameof(InstallerFolderIsNotExists),
        ErrorMessage = $"Installer install folder {folderName} is not exists"
    };

    public static Err InstallerInstallFolderDoesNotCreated(string folderName) => new()
    {
        ErrorCode = nameof(InstallerInstallFolderDoesNotCreated),
        ErrorMessage = $"Installer work install folder {folderName} does not created"
    };

    public static Err InstallerWorkFolderDoesNotCreated(string folderName) => new()
    {
        ErrorCode = nameof(InstallerWorkFolderDoesNotCreated),
        ErrorMessage = $"Installer work folder {folderName} does not created"
    };

    public static readonly Err IsServiceRegisteredProperlyError = new()
    {
        ErrorCode = nameof(IsServiceRegisteredProperlyError),
        ErrorMessage = "Error when check IsServiceRegisteredProperly"
    };

    public static Err ProcessIsRunningAndCannotBeUpdated(string projectName) => new()
    {
        ErrorCode = nameof(ProcessIsRunningAndCannotBeUpdated),
        ErrorMessage = $"Process {projectName} is running and cannot be updated"
    };

    public static Err ProjectFilesIsNotExtracted(string folderName) => new()
    {
        ErrorCode = nameof(ProjectFilesIsNotExtracted),
        ErrorMessage = $"Project files is not extracted to {folderName}"
    };

    public static Err ProjectInstallerFolderIsNotExists(string folderName) => new()
    {
        ErrorCode = nameof(InstallerFolderIsNotExists),
        ErrorMessage = $"Project Installer install folder {folderName} is not exists"
    };

    public static Err ServiceCanNotBeRemoved(string serviceEnvName) => new()
    {
        ErrorCode = nameof(ServiceCanNotBeRemoved),
        ErrorMessage = $"Service with name {serviceEnvName} can not be removed"
    };

    public static Err ServiceCanNotBeStarted(string serviceEnvName) => new()
    {
        ErrorCode = nameof(ServiceCanNotBeStarted),
        ErrorMessage = $"Service with name {serviceEnvName} can not be started"
    };

    public static Err ServiceCanNotBeStopped(string serviceEnvName) => new()
    {
        ErrorCode = nameof(ServiceCanNotBeStopped),
        ErrorMessage = $"Service with name {serviceEnvName} can not be stopped"
    };

    public static Err ServiceIsNotExists(string serviceEnvName) => new()
    {
        ErrorCode = nameof(ServiceIsNotExists),
        ErrorMessage = $"Service {serviceEnvName} does not exists, cannot update settings file"
    };

    public static Err ServiceIsNotStopped(string serviceEnvName) => new()
    {
        ErrorCode = nameof(ServiceIsNotStopped),
        ErrorMessage = $"Service with name {serviceEnvName} is not be stopped"
    };

    public static Err ServiceIsRunningAndCannotBeUpdated(string serviceEnvName) => new()
    {
        ErrorCode = nameof(ServiceIsNotStopped),
        ErrorMessage = $"Service {serviceEnvName} is running and cannot be updated"
    };
}