using LibFileParameters.Models;

namespace Installer.Domain;

public sealed class ApplicationUpdaterParameters
{
    public ApplicationUpdaterParameters(string programArchiveExtension, FileStorageData programExchangeFileStorage,
        string parametersFileDateMask, string parametersFileExtension, string filesUserName, string filesUsersGroupName,
        string programArchiveDateMask, string serviceUserName, string downloadTempExtension, string installerWorkFolder,
        string installFolder)
    {
        InstallerWorkFolder = installerWorkFolder;
        InstallFolder = installFolder;
        //DotnetRunner = dotnetRunner;
        ProgramArchiveExtension = programArchiveExtension;
        ProgramExchangeFileStorage = programExchangeFileStorage;
        ParametersFileDateMask = parametersFileDateMask;
        ParametersFileExtension = parametersFileExtension;
        FilesUserName = filesUserName;
        FilesUsersGroupName = filesUsersGroupName;
        ProgramArchiveDateMask = programArchiveDateMask;
        ServiceUserName = serviceUserName;
        DownloadTempExtension = downloadTempExtension;
    }

    public string InstallerWorkFolder { get; }

    public string InstallFolder { get; }

    //public string? DotnetRunner { get; }
    public string ProgramArchiveExtension { get; }
    public FileStorageData ProgramExchangeFileStorage { get; }

    public string ParametersFileDateMask { get; }
    public string ParametersFileExtension { get; }
    public string FilesUserName { get; }
    public string FilesUsersGroupName { get; }
    public string ProgramArchiveDateMask { get; }
    public string ServiceUserName { get; }
    public string DownloadTempExtension { get; }
}