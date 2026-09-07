using SystemTools.SharedKernel;

namespace ToolsManagement.Installer.Errors;

public static class ApplicationUpdaterErrors
{
    public static readonly Error InstallerWasNotCreated =
        Error.Problem(nameof(InstallerWasNotCreated), "Installer was Not Created");

    public static readonly Error InstallerWorkFolderIsEmpty =
        Error.Problem(nameof(InstallerWorkFolderIsEmpty), "InstallerWorkFolder is empty");

    public static readonly Error FilesUserNameIsEmpty =
        Error.Problem(nameof(FilesUserNameIsEmpty), "FilesUserName is empty");

    public static readonly Error FilesUsersGroupNameIsEmpty =
        Error.Problem(nameof(FilesUsersGroupNameIsEmpty), "FilesUsersGroupName is empty");

    public static readonly Error ServiceUserNameIsEmpty =
        Error.Problem(nameof(ServiceUserNameIsEmpty), "serviceUserName is empty");

    public static readonly Error DownloadTempExtensionIsEmpty =
        Error.Problem(nameof(DownloadTempExtensionIsEmpty), "downloadTempExtension is empty");

    public static readonly Error InstallFolderIsEmpty =
        Error.Problem(nameof(InstallFolderIsEmpty), "installFolder is empty");

    public static readonly Error DotnetRunnerIsEmpty =
        Error.Problem(nameof(DotnetRunnerIsEmpty), "dotnetRunner is empty");
}
