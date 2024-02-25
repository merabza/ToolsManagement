using LibFileParameters.Models;

namespace Installer.Domain;

public sealed class AppParametersFileUpdaterParameters
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public AppParametersFileUpdaterParameters(FileStorageData programExchangeFileStorage, string parametersFileDateMask,
        string parametersFileExtension, string filesUserName, string? filesUsersGroupName, string installFolder)
    {
        InstallFolder = installFolder;
        ProgramExchangeFileStorage = programExchangeFileStorage;
        ParametersFileDateMask = parametersFileDateMask;
        ParametersFileExtension = parametersFileExtension;
        FilesUserName = filesUserName;
        FilesUsersGroupName = filesUsersGroupName;
    }

    public string InstallFolder { get; }
    public FileStorageData ProgramExchangeFileStorage { get; }
    public string ParametersFileDateMask { get; }
    public string ParametersFileExtension { get; }
    public string FilesUserName { get; }
    public string? FilesUsersGroupName { get; }
}