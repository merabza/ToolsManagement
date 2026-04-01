using SystemTools.SystemToolsShared.Errors;

namespace ToolsManagement.Installer.Errors;

public static class ApplicationUpdaterErrors
{
    public static readonly Error InstallerWasNotCreated = new()
    {
        Code = nameof(InstallerWasNotCreated), Name = "Installer was Not Created"
    };

    public static readonly Error InstallerWorkFolderIsEmpty = new()
    {
        Code = nameof(InstallerWorkFolderIsEmpty), Name = "InstallerWorkFolder is empty"
    };

    public static readonly Error FilesUserNameIsEmpty = new()
    {
        Code = nameof(FilesUserNameIsEmpty), Name = "FilesUserName is empty"
    };

    public static readonly Error FilesUsersGroupNameIsEmpty = new()
    {
        Code = nameof(FilesUsersGroupNameIsEmpty), Name = "FilesUsersGroupName is empty"
    };

    public static readonly Error ServiceUserNameIsEmpty = new()
    {
        Code = nameof(ServiceUserNameIsEmpty), Name = "serviceUserName is empty"
    };

    public static readonly Error DownloadTempExtensionIsEmpty = new()
    {
        Code = nameof(DownloadTempExtensionIsEmpty), Name = "downloadTempExtension is empty"
    };

    public static readonly Error InstallFolderIsEmpty = new()
    {
        Code = nameof(InstallFolderIsEmpty), Name = "installFolder is empty"
    };

    public static readonly Error DotnetRunnerIsEmpty = new()
    {
        Code = nameof(DotnetRunnerIsEmpty), Name = "dotnetRunner is empty"
    };
}
