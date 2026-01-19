using SystemTools.SystemToolsShared.Errors;

namespace ToolsManagement.Installer.Errors;

public static class ApplicationUpdaterErrors
{
    public static readonly Err InstallerWasNotCreated = new()
    {
        ErrorCode = nameof(InstallerWasNotCreated), ErrorMessage = "Installer was Not Created"
    };

    public static readonly Err InstallerWorkFolderIsEmpty = new()
    {
        ErrorCode = nameof(InstallerWorkFolderIsEmpty), ErrorMessage = "InstallerWorkFolder is empty"
    };

    public static readonly Err FilesUserNameIsEmpty = new()
    {
        ErrorCode = nameof(FilesUserNameIsEmpty), ErrorMessage = "FilesUserName is empty"
    };

    public static readonly Err FilesUsersGroupNameIsEmpty = new()
    {
        ErrorCode = nameof(FilesUsersGroupNameIsEmpty), ErrorMessage = "FilesUsersGroupName is empty"
    };

    public static readonly Err ServiceUserNameIsEmpty = new()
    {
        ErrorCode = nameof(ServiceUserNameIsEmpty), ErrorMessage = "serviceUserName is empty"
    };

    public static readonly Err DownloadTempExtensionIsEmpty = new()
    {
        ErrorCode = nameof(DownloadTempExtensionIsEmpty), ErrorMessage = "downloadTempExtension is empty"
    };

    public static readonly Err InstallFolderIsEmpty = new()
    {
        ErrorCode = nameof(InstallFolderIsEmpty), ErrorMessage = "installFolder is empty"
    };

    public static readonly Err DotnetRunnerIsEmpty = new()
    {
        ErrorCode = nameof(DotnetRunnerIsEmpty), ErrorMessage = "dotnetRunner is empty"
    };
}