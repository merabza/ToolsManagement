using SystemTools.SystemToolsShared.Errors;

namespace ToolsManagement.Installer.Errors;

public static class InstallerErrors
{
    public static readonly ErrorOmd IsServiceRegisteredProperlyError = new()
    {
        Code = nameof(IsServiceRegisteredProperlyError), Name = "ErrorOmd when check IsServiceRegisteredProperly"
    };

    public static readonly ErrorOmd TheServiceWasNotRemoved = new()
    {
        Code = nameof(TheServiceWasNotRemoved), Name = "The service was not Removed"
    };

    public static readonly ErrorOmd TheServiceWasNotStopped = new()
    {
        Code = nameof(TheServiceWasNotStopped), Name = "The service was not Stopped"
    };

    public static readonly ErrorOmd TheServiceWasNotStarted = new()
    {
        Code = nameof(TheServiceWasNotStarted), Name = "The service was not Started"
    };

    public static ErrorOmd ProjectArchiveFileWasNotDownloaded =>
        new() { Code = nameof(ProjectArchiveFileWasNotDownloaded), Name = "Project archive file not downloaded" };

    public static ErrorOmd ProjectArchiveFilesNotFoundOnExchangeStorage =>
        new()
        {
            Code = nameof(ProjectArchiveFilesNotFoundOnExchangeStorage),
            Name = "Project archive files not found on exchange storage"
        };

    public static ErrorOmd CannotUpdateSelf => new() { Code = nameof(CannotUpdateSelf), Name = "Cannot update self" };

    public static ErrorOmd ExchangeFileManagerIsNull =>
        new()
        {
            Code = nameof(ExchangeFileManagerIsNull),
            Name = "exchangeFileManager is null in UpdateProgramWithParameters"
        };

    public static ErrorOmd FileNameIsEmpty => new() { Code = nameof(FileNameIsEmpty), Name = "File name is empty" };

    public static ErrorOmd FolderNameIsEmpty =>
        new() { Code = nameof(FolderNameIsEmpty), Name = "Folder name is empty" };

    public static ErrorOmd CannotUpdateProject(string projectName, string environmentName)
    {
        return new ErrorOmd
        {
            Code = nameof(CannotUpdateProject), Name = $"Cannot Update {projectName}/{environmentName}"
        };
    }

    public static ErrorOmd CannotRegisterService(string serviceEnvName)
    {
        return new ErrorOmd
        {
            Code = nameof(ExchangeFileManagerIsNull), Name = $"cannot register Service {serviceEnvName}"
        };
    }

    public static ErrorOmd FileCanNotBeDeleted(string fileName)
    {
        return new ErrorOmd { Code = nameof(FileCanNotBeDeleted), Name = $"File {fileName} can not Deleted" };
    }

    public static ErrorOmd FileIsNotExists(string fileName)
    {
        return new ErrorOmd { Code = nameof(FileIsNotExists), Name = $"File {fileName} is not exists" };
    }

    public static ErrorOmd FileOwnerCanNotBeChanged(string fileName)
    {
        return new ErrorOmd
        {
            Code = nameof(FileOwnerCanNotBeChanged), Name = $"File {fileName} owner can not be changed"
        };
    }

    public static ErrorOmd FolderCanNotBeDeleted(string folderName)
    {
        return new ErrorOmd { Code = nameof(FileCanNotBeDeleted), Name = $"Folder {folderName} can not be Deleted" };
    }

    public static ErrorOmd FolderIsNotExists(string folderName)
    {
        return new ErrorOmd { Code = nameof(FolderIsNotExists), Name = $"File {folderName} is not exists" };
    }

    public static ErrorOmd FolderOwnerCanNotBeChanged(string folderName)
    {
        return new ErrorOmd
        {
            Code = nameof(FolderOwnerCanNotBeChanged), Name = $"Folder {folderName} owner can not be changed"
        };
    }

    public static ErrorOmd InstallerFolderIsNotExists(string folderName)
    {
        return new ErrorOmd
        {
            Code = nameof(InstallerFolderIsNotExists), Name = $"Installer install folder {folderName} is not exists"
        };
    }

    public static ErrorOmd InstallerInstallFolderDoesNotCreated(string folderName)
    {
        return new ErrorOmd
        {
            Code = nameof(InstallerInstallFolderDoesNotCreated),
            Name = $"Installer work install folder {folderName} does not created"
        };
    }

    public static ErrorOmd InstallerWorkFolderDoesNotCreated(string folderName)
    {
        return new ErrorOmd
        {
            Code = nameof(InstallerWorkFolderDoesNotCreated),
            Name = $"Installer work folder {folderName} does not created"
        };
    }

    public static ErrorOmd ProcessIsRunningAndCannotBeUpdated(string projectName)
    {
        return new ErrorOmd
        {
            Code = nameof(ProcessIsRunningAndCannotBeUpdated),
            Name = $"Process {projectName} is running and cannot be updated"
        };
    }

    public static ErrorOmd ProjectFilesIsNotExtracted(string folderName)
    {
        return new ErrorOmd
        {
            Code = nameof(ProjectFilesIsNotExtracted), Name = $"Project files is not extracted to {folderName}"
        };
    }

    public static ErrorOmd ProjectInstallerFolderIsNotExists(string folderName)
    {
        return new ErrorOmd
        {
            Code = nameof(InstallerFolderIsNotExists),
            Name = $"Project Installer install folder {folderName} is not exists"
        };
    }

    public static ErrorOmd ServiceCanNotBeRemoved(string serviceEnvName)
    {
        return new ErrorOmd
        {
            Code = nameof(ServiceCanNotBeRemoved), Name = $"Service with name {serviceEnvName} can not be removed"
        };
    }

    public static ErrorOmd ServiceCanNotBeStarted(string serviceEnvName)
    {
        return new ErrorOmd
        {
            Code = nameof(ServiceCanNotBeStarted), Name = $"Service with name {serviceEnvName} can not be started"
        };
    }

    public static ErrorOmd ServiceCanNotBeStopped(string serviceEnvName)
    {
        return new ErrorOmd
        {
            Code = nameof(ServiceCanNotBeStopped), Name = $"Service with name {serviceEnvName} can not be stopped"
        };
    }

    public static ErrorOmd ServiceIsNotExists(string serviceEnvName)
    {
        return new ErrorOmd
        {
            Code = nameof(ServiceIsNotExists),
            Name = $"Service {serviceEnvName} does not exists, cannot update settings file"
        };
    }

    public static ErrorOmd ServiceIsNotStopped(string serviceEnvName)
    {
        return new ErrorOmd
        {
            Code = nameof(ServiceIsNotStopped), Name = $"Service with name {serviceEnvName} is not be stopped"
        };
    }

    public static ErrorOmd ServiceIsRunningAndCannotBeUpdated(string serviceEnvName)
    {
        return new ErrorOmd
        {
            Code = nameof(ServiceIsNotStopped), Name = $"Service {serviceEnvName} is running and cannot be updated"
        };
    }

    public static ErrorOmd ServiceIsRunningAndCanNotBeRemoved(string serviceEnvName)
    {
        return new ErrorOmd
        {
            Code = nameof(ServiceIsRunningAndCanNotBeRemoved),
            Name = $"Service {serviceEnvName} is running and can not be removed"
        };
    }
}
