using System;
using LibFileParameters.Models;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace FileManagersMain;

public static class FileManagersFabric
{
    public static FileManager? CreateFileManager(bool useConsole, ILogger logger, string localPatch)
    {
        //თუ ლოკალური ფოლდერი არ არსებობს, შეიქმნას
        if (string.IsNullOrWhiteSpace(localPatch))
        {
            logger.LogError("local path not Specified");
            return null;
        }

        var localPatchChecked = FileStat.CreateFolderIfNotExists(localPatch, useConsole);
        //თუ ლოკალური ფოლდერი არ არსებობს, შეიქმნას
        if (localPatchChecked == null)
        {
            logger.LogError("local path {localPatch} can not created", localPatch);
            return null;
        }

        DiskFileManager dfm = new(localPatchChecked, useConsole, logger, localPatchChecked);

        return dfm;
    }

    public static FileManager? CreateFileManager(bool useConsole, ILogger logger, string? localPatch,
        string? fileStorageName, FileStorages? fileStorages, bool allowLocalPathNull = false)
    {
        FileStorageData? fileStorageData = null;

        if (string.IsNullOrWhiteSpace(fileStorageName))
            logger.LogError("File storage name not specified");
        else
            fileStorageData = fileStorages?.GetFileStorageDataByKey(fileStorageName);

        if (fileStorageData == null)
            logger.LogError("File storage with name {fileStorageName} not found", fileStorageName);

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
                logger.LogError("local path {localPatch} can not created", localPatch);
                return null;
            }
        }

        if (fileStorageData.FileStoragePath is null)
            throw new Exception("fileStorageData.FileStoragePath is null");

        if (!FileStat.IsFileSchema(fileStorageData.FileStoragePath))
        {
            var rfm = RemoteFileManager.Create(fileStorageData, useConsole, logger, localPatchChecked);
            if (rfm != null)
                return rfm;
        }

        //if (fileStorageData.FileStoragePath == null)
        //{
        //    logger.LogError("file Storage Path not specified");
        //    return null;
        //}

        //თუ სამუშაო ფოლდერი არ არსებობს, შეიქმნას
        var fileStoragePathChecked = FileStat.CreateFolderIfNotExists(fileStorageData.FileStoragePath, useConsole);
        if (fileStoragePathChecked is null)
        {
            var fileStoragePath = fileStorageData.FileStoragePath;
            logger.LogError("local path {fileStoragePath} can not created", fileStoragePath);
            return null;
        }

        DiskFileManager dfm = new(fileStoragePathChecked, useConsole, logger, localPatchChecked);

        return dfm;
    }
}