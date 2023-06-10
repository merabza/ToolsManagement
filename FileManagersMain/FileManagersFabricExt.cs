using LibFileParameters.Models;
using Microsoft.Extensions.Logging;

namespace FileManagersMain;

public static class FileManagersFabricExt
{
    public static FileManager? CreateFileManager(bool useConsole, ILogger logger, string? localPatch,
        FileStorageData fileStorage, bool allowLocalPathNull = false)
    {
        return FileManagersFabric.CreateFileManager(useConsole, logger, localPatch, fileStorage, allowLocalPathNull);
    }

    public static (FileStorageData?, FileManager?) CreateFileStorageAndFileManager(bool useConsole, ILogger logger,
        string localPatch, string? fileStorageName, FileStorages fileStorages)
    {
        FileStorageData? fileStorageData = null;
        if (string.IsNullOrWhiteSpace(fileStorageName))
        {
            logger.LogError("File storage name not specified");
        }
        else
        {
            fileStorageData = fileStorages.GetFileStorageDataByKey(fileStorageName);
            if (fileStorageData == null) logger.LogError($"File storage with name {fileStorageName} not found");
        }

        if (fileStorageData == null)
            return (null, null);

        var fmg = FileManagersFabric.CreateFileManager(useConsole, logger, localPatch, fileStorageData);

        return fmg == null ? (null, null) : (fileStorageData, fmg);
    }
}