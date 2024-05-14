using System.Threading;
using System.Threading.Tasks;
using LibFileParameters.Models;
using Microsoft.Extensions.Logging;
using SignalRContracts;

namespace FileManagersMain;

public static class FileManagersFabricExt
{
    public static FileManager? CreateFileManager(bool useConsole, ILogger logger, string? localPatch,
        FileStorageData fileStorage, bool allowLocalPathNull = false)
    {
        return FileManagersFabric.CreateFileManager(useConsole, logger, localPatch, fileStorage, allowLocalPathNull);
    }

    public static async Task<(FileStorageData?, FileManager?)> CreateFileStorageAndFileManager(bool useConsole,
        ILogger logger, string localPatch, string? fileStorageName, FileStorages fileStorages,
        IMessagesDataManager? messagesDataManager, string? userName, CancellationToken cancellationToken)
    {
        FileStorageData? fileStorageData = null;
        if (string.IsNullOrWhiteSpace(fileStorageName))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName, "File storage name not specified", cancellationToken);
            logger.LogError("File storage name not specified");
        }
        else
        {
            fileStorageData = fileStorages.GetFileStorageDataByKey(fileStorageName);
            if (fileStorageData == null)
            {
                if (messagesDataManager is not null)
                    await messagesDataManager.SendMessage(userName,
                        $"File storage with name {fileStorageName} not found", cancellationToken);
                logger.LogError("File storage with name {fileStorageName} not found", fileStorageName);
            }
        }

        if (fileStorageData == null)
            return (null, null);

        var fmg = FileManagersFabric.CreateFileManager(useConsole, logger, localPatch, fileStorageData);

        return fmg == null ? (null, null) : (fileStorageData, fmg);
    }
}