using SystemTools.SystemToolsShared.Errors;

namespace ToolsManagement.Installer.Errors;

public static class ApplicationUpdaterErrors
{
    public static readonly ErrorOmd InstallerWasNotCreated = new()
    {
        Code = nameof(InstallerWasNotCreated), Name = "Installer was Not Created"
    };

    public static readonly ErrorOmd InstallerWorkFolderIsEmpty = new()
    {
        Code = nameof(InstallerWorkFolderIsEmpty), Name = "InstallerWorkFolder is empty"
    };

    public static readonly ErrorOmd FilesUserNameIsEmpty = new()
    {
        Code = nameof(FilesUserNameIsEmpty), Name = "FilesUserName is empty"
    };

    public static readonly ErrorOmd FilesUsersGroupNameIsEmpty = new()
    {
        Code = nameof(FilesUsersGroupNameIsEmpty), Name = "FilesUsersGroupName is empty"
    };

    public static readonly ErrorOmd ServiceUserNameIsEmpty = new()
    {
        Code = nameof(ServiceUserNameIsEmpty), Name = "serviceUserName is empty"
    };

    public static readonly ErrorOmd DownloadTempExtensionIsEmpty = new()
    {
        Code = nameof(DownloadTempExtensionIsEmpty), Name = "downloadTempExtension is empty"
    };

    public static readonly ErrorOmd InstallFolderIsEmpty = new()
    {
        Code = nameof(InstallFolderIsEmpty), Name = "installFolder is empty"
    };

    public static readonly ErrorOmd DotnetRunnerIsEmpty = new()
    {
        Code = nameof(DotnetRunnerIsEmpty), Name = "dotnetRunner is empty"
    };
}
