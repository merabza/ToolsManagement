using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibFileParameters.Models;
using SystemTools.SystemToolsShared;

namespace ToolsManagement.FileManagersMain;

public static class FileManagersFactoryExt
{
    public static FileManager? CreateFileManager(bool useConsole, ILogger logger, string? localPatch,
        FileStorageData fileStorage, bool allowLocalPathNull = false)
    {
        return FileManagersFactory.CreateFileManager(useConsole, logger, localPatch, fileStorage, allowLocalPathNull);
    }

    //public static (FileStorageData?, FileManager?) CreateLocalFileStorageAndFileManager(bool useConsole,
    //    ILogger logger, string localPatch, string fileStoragePath)
    //{
    //    var fileStorageData = new FileStorageData {FileStoragePath =fileStoragePath };

    //    var fmg = FileManagersFactory.CreateFileManager(useConsole, logger, localPatch, fileStorageData);

    //    return fmg == null ? (null, null) : (fileStorageData, fmg);
    //}

    public static async ValueTask<(FileStorageData?, FileManager?)> CreateFileStorageAndFileManager(bool useConsole,
        ILogger logger, string localPatch, string? fileStorageName, FileStorages fileStorages,
        IMessagesDataManager? messagesDataManager, string? userName, CancellationToken cancellationToken = default)
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

        var fmg = FileManagersFactory.CreateFileManager(useConsole, logger, localPatch, fileStorageData);

        return fmg == null ? (null, null) : (fileStorageData, fmg);
    }
}