using System;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibFileParameters.Models;
using SystemTools.SystemToolsShared;

namespace ToolsManagement.FileManagersMain;

public static class FileManagersFactory
{
    public static FileManager? CreateFileManager(bool useConsole, ILogger logger, string storagePatch,
        string? localPatch = null)
    {
        //თუ ლოკალური ფოლდერი არ არსებობს, შეიქმნას
        if (string.IsNullOrWhiteSpace(storagePatch))
        {
            logger.LogError("local path not Specified");
            return null;
        }

        string? storagePatchChecked = FileStat.CreateFolderIfNotExists(storagePatch, useConsole);
        //თუ ლოკალური ფოლდერი არ არსებობს, შეიქმნას
        if (storagePatchChecked == null)
        {
            logger.LogError("local path {LocalPatch} can not created", storagePatch);
            return null;
        }

        string? localPatchChecked = storagePatchChecked;
        if (!string.IsNullOrWhiteSpace(localPatch) && storagePatch != localPatch)
        {
            localPatchChecked = FileStat.CreateFolderIfNotExists(localPatch, useConsole);
            //თუ ლოკალური ფოლდერი არ არსებობს, შეიქმნას
            if (localPatchChecked == null)
            {
                logger.LogError("local path {LocalPatch} can not created", storagePatch);
                return null;
            }
        }

        var dfm = new DiskFileManager(storagePatchChecked, useConsole, logger, localPatchChecked);

        return dfm;
    }

    public static FileManager? CreateFileManager(bool useConsole, ILogger logger, string? localPatch,
        string? fileStorageName, FileStorages? fileStorages, bool allowLocalPathNull = false)
    {
        FileStorageData? fileStorageData = null;

        if (string.IsNullOrWhiteSpace(fileStorageName))
        {
            logger.LogError("File storage name not specified");
        }
        else
        {
            fileStorageData = fileStorages?.GetFileStorageDataByKey(fileStorageName);
        }

        if (fileStorageData == null)
        {
            logger.LogError("File storage with name {FileStorageName} not found", fileStorageName);
        }

        return fileStorageData == null
            ? null
            : CreateFileManager(useConsole, logger, localPatch, fileStorageData, allowLocalPathNull);
    }

    public static FileManager? CreateFileManager(bool useConsole, ILogger logger, string? localPatch,
        FileStorageData fileStorageData, bool allowLocalPathNull = false)
    {
        string? localPatchChecked = null;
        if (!allowLocalPathNull)
        {
            if (localPatch is null)
            {
                logger.LogInformation("local path not specified");
                return null;
            }

            localPatchChecked = FileStat.CreateFolderIfNotExists(localPatch, useConsole);
            //თუ ლოკალური ფოლდერი არ არსებობს, შეიქმნას
            if (localPatchChecked == null)
            {
                logger.LogError("local path {LocalPatch} can not created", localPatch);
                return null;
            }
        }

        if (fileStorageData.FileStoragePath is null)
        {
            throw new Exception("fileStorageData.FileStoragePath is null");
        }

        if (!FileStat.IsFileSchema(fileStorageData.FileStoragePath))
        {
            var rfm = RemoteFileManager.Create(fileStorageData, useConsole, logger, localPatchChecked);
            if (rfm != null)
            {
                return rfm;
            }
        }

        //if (fileStorageData.FileStoragePath == null)
        //{
        //    logger.LogError("file Storage Path not specified");
        //    return null;
        //}

        //თუ სამუშაო ფოლდერი არ არსებობს, შეიქმნას
        string? fileStoragePathChecked = FileStat.CreateFolderIfNotExists(fileStorageData.FileStoragePath, useConsole);
        if (fileStoragePathChecked is null)
        {
            string? fileStoragePath = fileStorageData.FileStoragePath;
            logger.LogError("local path {FileStoragePath} can not created", fileStoragePath);
            return null;
        }

        var dfm = new DiskFileManager(fileStoragePathChecked, useConsole, logger, localPatchChecked);

        return dfm;
    }
}
