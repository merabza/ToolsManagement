using SystemTools.SystemToolsShared.Errors;

namespace ToolsManagement.Installer.Errors;

public static class InstallerErrors
{
    public static readonly Error IsServiceRegisteredProperlyError = new()
    {
        Code = nameof(IsServiceRegisteredProperlyError), Name = "Error when check IsServiceRegisteredProperly"
    };

    public static readonly Error TheServiceWasNotRemoved = new()
    {
        Code = nameof(TheServiceWasNotRemoved), Name = "The service was not Removed"
    };

    public static readonly Error TheServiceWasNotStopped = new()
    {
        Code = nameof(TheServiceWasNotStopped), Name = "The service was not Stopped"
    };

    public static readonly Error TheServiceWasNotStarted = new()
    {
        Code = nameof(TheServiceWasNotStarted), Name = "The service was not Started"
    };

    public static Error ProjectArchiveFileWasNotDownloaded =>
        new() { Code = nameof(ProjectArchiveFileWasNotDownloaded), Name = "Project archive file not downloaded" };

    public static Error ProjectArchiveFilesNotFoundOnExchangeStorage =>
        new()
        {
            Code = nameof(ProjectArchiveFilesNotFoundOnExchangeStorage),
            Name = "Project archive files not found on exchange storage"
        };

    public static Error CannotUpdateSelf => new() { Code = nameof(CannotUpdateSelf), Name = "Cannot update self" };

    public static Error ExchangeFileManagerIsNull =>
        new()
        {
            Code = nameof(ExchangeFileManagerIsNull),
            Name = "exchangeFileManager is null in UpdateProgramWithParameters"
        };

    public static Error FileNameIsEmpty => new() { Code = nameof(FileNameIsEmpty), Name = "File name is empty" };

    public static Error FolderNameIsEmpty => new() { Code = nameof(FolderNameIsEmpty), Name = "Folder name is empty" };

    public static Error CannotUpdateProject(string projectName, string environmentName)
    {
        return new Error
        {
            Code = nameof(CannotUpdateProject), Name = $"Cannot Update {projectName}/{environmentName}"
        };
    }

    public static Error CannotRegisterService(string serviceEnvName)
    {
        return new Error
        {
            Code = nameof(ExchangeFileManagerIsNull), Name = $"cannot register Service {serviceEnvName}"
        };
    }

    public static Error FileCanNotBeDeleted(string fileName)
    {
        return new Error { Code = nameof(FileCanNotBeDeleted), Name = $"File {fileName} can not Deleted" };
    }

    public static Error FileIsNotExists(string fileName)
    {
        return new Error { Code = nameof(FileIsNotExists), Name = $"File {fileName} is not exists" };
    }

    public static Error FileOwnerCanNotBeChanged(string fileName)
    {
        return new Error
        {
            Code = nameof(FileOwnerCanNotBeChanged), Name = $"File {fileName} owner can not be changed"
        };
    }

    public static Error FolderCanNotBeDeleted(string folderName)
    {
        return new Error { Code = nameof(FileCanNotBeDeleted), Name = $"Folder {folderName} can not be Deleted" };
    }

    public static Error FolderIsNotExists(string folderName)
    {
        return new Error { Code = nameof(FolderIsNotExists), Name = $"File {folderName} is not exists" };
    }

    public static Error FolderOwnerCanNotBeChanged(string folderName)
    {
        return new Error
        {
            Code = nameof(FolderOwnerCanNotBeChanged), Name = $"Folder {folderName} owner can not be changed"
        };
    }

    public static Error InstallerFolderIsNotExists(string folderName)
    {
        return new Error
        {
            Code = nameof(InstallerFolderIsNotExists), Name = $"Installer install folder {folderName} is not exists"
        };
    }

    public static Error InstallerInstallFolderDoesNotCreated(string folderName)
    {
        return new Error
        {
            Code = nameof(InstallerInstallFolderDoesNotCreated),
            Name = $"Installer work install folder {folderName} does not created"
        };
    }

    public static Error InstallerWorkFolderDoesNotCreated(string folderName)
    {
        return new Error
        {
            Code = nameof(InstallerWorkFolderDoesNotCreated),
            Name = $"Installer work folder {folderName} does not created"
        };
    }

    public static Error ProcessIsRunningAndCannotBeUpdated(string projectName)
    {
        return new Error
        {
            Code = nameof(ProcessIsRunningAndCannotBeUpdated),
            Name = $"Process {projectName} is running and cannot be updated"
        };
    }

    public static Error ProjectFilesIsNotExtracted(string folderName)
    {
        return new Error
        {
            Code = nameof(ProjectFilesIsNotExtracted), Name = $"Project files is not extracted to {folderName}"
        };
    }

    public static Error ProjectInstallerFolderIsNotExists(string folderName)
    {
        return new Error
        {
            Code = nameof(InstallerFolderIsNotExists),
            Name = $"Project Installer install folder {folderName} is not exists"
        };
    }

    public static Error ServiceCanNotBeRemoved(string serviceEnvName)
    {
        return new Error
        {
            Code = nameof(ServiceCanNotBeRemoved), Name = $"Service with name {serviceEnvName} can not be removed"
        };
    }

    public static Error ServiceCanNotBeStarted(string serviceEnvName)
    {
        return new Error
        {
            Code = nameof(ServiceCanNotBeStarted), Name = $"Service with name {serviceEnvName} can not be started"
        };
    }

    public static Error ServiceCanNotBeStopped(string serviceEnvName)
    {
        return new Error
        {
            Code = nameof(ServiceCanNotBeStopped), Name = $"Service with name {serviceEnvName} can not be stopped"
        };
    }

    public static Error ServiceIsNotExists(string serviceEnvName)
    {
        return new Error
        {
            Code = nameof(ServiceIsNotExists),
            Name = $"Service {serviceEnvName} does not exists, cannot update settings file"
        };
    }

    public static Error ServiceIsNotStopped(string serviceEnvName)
    {
        return new Error
        {
            Code = nameof(ServiceIsNotStopped), Name = $"Service with name {serviceEnvName} is not be stopped"
        };
    }

    public static Error ServiceIsRunningAndCannotBeUpdated(string serviceEnvName)
    {
        return new Error
        {
            Code = nameof(ServiceIsNotStopped), Name = $"Service {serviceEnvName} is running and cannot be updated"
        };
    }

    public static Error ServiceIsRunningAndCanNotBeRemoved(string serviceEnvName)
    {
        return new Error
        {
            Code = nameof(ServiceIsRunningAndCanNotBeRemoved),
            Name = $"Service {serviceEnvName} is running and can not be removed"
        };
    }
}
