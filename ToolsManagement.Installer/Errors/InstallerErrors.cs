using SystemTools.SharedKernel;

namespace ToolsManagement.Installer.Errors;

public static class InstallerErrors
{
    public static readonly Error IsServiceRegisteredProperlyError =
        Error.Problem(nameof(IsServiceRegisteredProperlyError), "Error when check IsServiceRegisteredProperly");

    public static readonly Error TheServiceWasNotRemoved =
        Error.Problem(nameof(TheServiceWasNotRemoved), "The service was not Removed");

    public static readonly Error TheServiceWasNotStopped =
        Error.Problem(nameof(TheServiceWasNotStopped), "The service was not Stopped");

    public static readonly Error TheServiceWasNotStarted =
        Error.Problem(nameof(TheServiceWasNotStarted), "The service was not Started");

    public static Error ProjectArchiveFileWasNotDownloaded =>
        Error.Problem(nameof(ProjectArchiveFileWasNotDownloaded), "Project archive file not downloaded");

    public static Error ProjectArchiveFilesNotFoundOnExchangeStorage =>
        Error.Problem(nameof(ProjectArchiveFilesNotFoundOnExchangeStorage),
            "Project archive files not found on exchange storage");

    public static Error CannotUpdateSelf => Error.Problem(nameof(CannotUpdateSelf), "Cannot update self");

    public static Error ExchangeFileManagerIsNull =>
        Error.Problem(nameof(ExchangeFileManagerIsNull), "exchangeFileManager is null in UpdateProgramWithParameters");

    public static Error FileNameIsEmpty => Error.Problem(nameof(FileNameIsEmpty), "File name is empty");

    public static Error FolderNameIsEmpty => Error.Problem(nameof(FolderNameIsEmpty), "Folder name is empty");

    public static Error CannotUpdateProject(string projectName, string environmentName)
    {
        return Error.Problem(nameof(CannotUpdateProject), $"Cannot Update {projectName}/{environmentName}");
    }

    public static Error CannotRegisterService(string serviceEnvName)
    {
        return Error.Problem(nameof(ExchangeFileManagerIsNull), $"cannot register Service {serviceEnvName}");
    }

    public static Error FileCanNotBeDeleted(string fileName)
    {
        return Error.Problem(nameof(FileCanNotBeDeleted), $"File {fileName} can not Deleted");
    }

    public static Error FileIsNotExists(string fileName)
    {
        return Error.Problem(nameof(FileIsNotExists), $"File {fileName} is not exists");
    }

    public static Error FileOwnerCanNotBeChanged(string fileName)
    {
        return Error.Problem(nameof(FileOwnerCanNotBeChanged), $"File {fileName} owner can not be changed");
    }

    public static Error FolderCanNotBeDeleted(string folderName)
    {
        return Error.Problem(nameof(FileCanNotBeDeleted), $"Folder {folderName} can not be Deleted");
    }

    public static Error FolderIsNotExists(string folderName)
    {
        return Error.Problem(nameof(FolderIsNotExists), $"File {folderName} is not exists");
    }

    public static Error FolderOwnerCanNotBeChanged(string folderName)
    {
        return Error.Problem(nameof(FolderOwnerCanNotBeChanged), $"Folder {folderName} owner can not be changed");
    }

    public static Error InstallerFolderIsNotExists(string folderName)
    {
        return Error.Problem(nameof(InstallerFolderIsNotExists),
            $"Installer install folder {folderName} is not exists");
    }

    public static Error InstallerInstallFolderDoesNotCreated(string folderName)
    {
        return Error.Problem(nameof(InstallerInstallFolderDoesNotCreated),
            $"Installer work install folder {folderName} does not created");
    }

    public static Error InstallerWorkFolderDoesNotCreated(string folderName)
    {
        return Error.Problem(nameof(InstallerWorkFolderDoesNotCreated),
            $"Installer work folder {folderName} does not created");
    }

    public static Error ProcessIsRunningAndCannotBeUpdated(string projectName)
    {
        return Error.Problem(nameof(ProcessIsRunningAndCannotBeUpdated),
            $"Process {projectName} is running and cannot be updated");
    }

    public static Error ProjectFilesIsNotExtracted(string folderName)
    {
        return Error.Problem(nameof(ProjectFilesIsNotExtracted), $"Project files is not extracted to {folderName}");
    }

    public static Error ProjectInstallerFolderIsNotExists(string folderName)
    {
        return Error.Problem(nameof(InstallerFolderIsNotExists),
            $"Project Installer install folder {folderName} is not exists");
    }

    public static Error ServiceCanNotBeRemoved(string serviceEnvName)
    {
        return Error.Problem(nameof(ServiceCanNotBeRemoved),
            $"Service with name {serviceEnvName} can not be removed");
    }

    public static Error ServiceCanNotBeStarted(string serviceEnvName)
    {
        return Error.Problem(nameof(ServiceCanNotBeStarted),
            $"Service with name {serviceEnvName} can not be started");
    }

    public static Error ServiceCanNotBeStopped(string serviceEnvName)
    {
        return Error.Problem(nameof(ServiceCanNotBeStopped),
            $"Service with name {serviceEnvName} can not be stopped");
    }

    public static Error ServiceIsNotExists(string serviceEnvName)
    {
        return Error.Problem(nameof(ServiceIsNotExists),
            $"Service {serviceEnvName} does not exists, cannot update settings file");
    }

    public static Error ServiceIsNotStopped(string serviceEnvName)
    {
        return Error.Problem(nameof(ServiceIsNotStopped), $"Service with name {serviceEnvName} is not be stopped");
    }

    public static Error ServiceIsRunningAndCannotBeUpdated(string serviceEnvName)
    {
        return Error.Problem(nameof(ServiceIsNotStopped),
            $"Service {serviceEnvName} is running and cannot be updated");
    }

    public static Error ServiceIsRunningAndCanNotBeRemoved(string serviceEnvName)
    {
        return Error.Problem(nameof(ServiceIsRunningAndCanNotBeRemoved),
            $"Service {serviceEnvName} is running and can not be removed");
    }
}
