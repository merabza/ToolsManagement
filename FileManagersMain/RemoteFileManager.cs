using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConnectTools;
using CToolsFabric;
using LibFileParameters.Models;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace FileManagersMain;

public sealed class RemoteFileManager : FileManager
{
    private readonly CTools _cTools;
    private readonly string _serverRootPaths;

    private RemoteFileManager(CTools cTools, string serverRootPaths, bool useConsole, ILogger logger,
        string? localPatch) : base(useConsole, logger, localPatch)
    {
        _cTools = cTools;
        _serverRootPaths = serverRootPaths;
    }

    //public for UsbCopyRunner
    public override char DirectorySeparatorChar => _cTools.DirectorySeparatorChar;

    public static RemoteFileManager? Create(FileStorageData fileStorageData, bool useConsole, ILogger logger,
        string? localPatch)
    {
        var fileStoragePath = fileStorageData.FileStoragePath;
        var connectToolParameters =
            ConnectToolParameters.Create(fileStoragePath, fileStorageData.UserName, fileStorageData.Password);

        if (connectToolParameters == null)
        {
            logger.LogError("Can not create connection parameters to server {fileStoragePath}", fileStoragePath);
            return null;
        }

        var connectionTools =
            ConnectToolsFabric.CreateConnectToolsByAddress(connectToolParameters, logger, useConsole);

        if (connectionTools == null)
        {
            logger.LogError("Can not create connection to server {fileStoragePath}", fileStoragePath);
            return null;
        }

        if (connectionTools.CheckConnection())
            return new RemoteFileManager(connectionTools, connectToolParameters.SiteRootAddress, useConsole, logger,
                localPatch);
        logger.LogError("Can not connect to server {fileStoragePath}", fileStoragePath);
        return null;
    }

    public override IEnumerable<MyFileInfo> GetFilesWithInfo(string? afterRootPath, string? searchPattern)
    {
        return _cTools.GetFilesWithInfo(afterRootPath, searchPattern, false);
    }

    //public for UsbCopyRunner
    public override List<string> GetFileNames(string? afterRootPath, string? searchPattern)
    {
        return _cTools.GetFiles(afterRootPath, searchPattern, false);
    }

    //public for UsbCopyRunner
    public override List<string> GetFolderNames(string? afterRootPath, string? searchPattern)
    {
        return _cTools.GetSubdirectories(afterRootPath, searchPattern, false);
    }

    public override bool UploadFile(string fileName, string useTempExtension)
    {
        if (LocalPatch is null)
            throw new Exception("UploadFile: LocalPatch is null");

        var fileFullName = Path.Combine(LocalPatch, fileName);
        var tempFileName = fileName + useTempExtension.AddNeedLeadPart(".");

        return UploadFileReal(fileFullName, null, tempFileName, fileName);
    }

    public override bool UploadFile(string? localAfterRootPath, string fileName, string? afterRootPath,
        string upFileName, string useTempExtension)
    {
        if (LocalPatch is null)
            throw new Exception("UploadFile: LocalPatch is null");

        var fileFullName = localAfterRootPath != null
            ? Path.Combine(LocalPatch, localAfterRootPath, fileName)
            : Path.Combine(LocalPatch, fileName);
        var tempFileName = upFileName + useTempExtension.AddNeedLeadPart(".");
        return UploadFileReal(fileFullName, afterRootPath, tempFileName, upFileName);
    }


    public override bool DownloadFile(string? remoteAfterRootPath, string fileName, string? localAfterRootPath,
        string downFileName, string useTempExtension)
    {
        if (LocalPatch is null)
            throw new Exception("DownloadFile: LocalPatch is null");

        var path = localAfterRootPath == null ? LocalPatch : Path.Combine(LocalPatch, localAfterRootPath);
        var fileFullName = Path.Combine(path, fileName);
        var tempFileName = downFileName + useTempExtension.AddNeedLeadPart(".");

        if (_cTools.DownloadFile(remoteAfterRootPath, fileName, path, tempFileName) &&
            RenameFile(Path.Combine(path, tempFileName), fileFullName, UseConsole, Logger))
            return true;

        Logger.LogError("DownloadFile file finished with errors: {fileFullName}", fileFullName);
        return false;
    }

    public override bool DownloadFile(string fileName, string useTempExtension, string? afterRootPath = null)
    {
        if (LocalPatch is null)
            throw new Exception("DownloadFile: LocalPatch is null");

        var localAfterRootPath = afterRootPath?.Replace(DirectorySeparatorChar, Path.DirectorySeparatorChar);
        var path = localAfterRootPath == null ? LocalPatch : Path.Combine(LocalPatch, localAfterRootPath);
        var fileFullName = Path.Combine(path, fileName);
        var tempFileName = fileName + useTempExtension.AddNeedLeadPart(".");
        //Logger.LogInformation($"Downloading file from {afterRootPath} to {fileFullName}");
        if (_cTools.DownloadFile(afterRootPath, fileName, path, tempFileName) &&
            RenameFile(Path.Combine(path, tempFileName), fileFullName, UseConsole, Logger))
            return true;

        Logger.LogError("DownloadFile file finished with errors: {fileFullName}", fileFullName);
        return false;
    }


    private bool UploadFileReal(string fileFullName, string? afterRootPath, string tempFileName, string fileName)
    {
        var rootPathName = afterRootPath ?? "Root Path";
        Logger.LogInformation(
            "Uploading file from {fileFullName} to {rootPathName} with temp name {tempFileName} and FinishName {fileName}",
            fileFullName, rootPathName, tempFileName, fileName);
        if (_cTools.UploadFile(fileFullName, afterRootPath, tempFileName) &&
            _cTools.Rename(afterRootPath, tempFileName, fileName))
            return true;

        Logger.LogError("Upload file finished with errors: form {fileFullName}", fileFullName);
        return false;
    }

    public override bool UploadContentToTextFile(string content, string serverSideFileName)
    {
        Logger.LogInformation("Uploading Parameters content to {_serverRootPaths} in {serverSideFileName}",
            _serverRootPaths, serverSideFileName);
        if (_cTools.UploadContentToTextFile(content, null, serverSideFileName))
            return true;

        Logger.LogError("Upload file content finished with errors: in {serverSideFileName} to {_serverRootPaths}",
            serverSideFileName, _serverRootPaths);
        return false;
    }

    public override async Task<bool> UploadContentToTextFileAsync(string content, string serverSideFileName,
        CancellationToken cancellationToken)
    {
        Logger.LogInformation("Uploading Parameters content to {_serverRootPaths} in {serverSideFileName}",
            _serverRootPaths, serverSideFileName);
        if (await _cTools.UploadContentToTextFileAsync(content, null, serverSideFileName, cancellationToken))
            return true;

        Logger.LogError("Upload file content finished with errors: in {serverSideFileName} to {_serverRootPaths}",
            serverSideFileName, _serverRootPaths);
        return false;
    }

    public override string? GetTextFileContent(string fileName)
    {
        Logger.LogInformation("Downloading text file {fileName} content from {_serverRootPaths}", fileName,
            _serverRootPaths);
        return _cTools.GetTextFileContent(null, fileName);
    }

    public override bool RenameFile(string? afterRootPath, string fileName, string newFileName)
    {
        return _cTools.Rename(afterRootPath, fileName, newFileName);
    }


    public override bool RenameFolder(string? afterRootPath, string folderName, string newFolderName)
    {
        return _cTools.Rename(afterRootPath, folderName, newFolderName);
    }

    protected override bool DeleteFile(string fileName)
    {
        return _cTools.Delete(null, fileName);
    }


    //ფაილის წაშლა სახელის მითითებით
    public override bool DeleteFile(string? afterRootPath, string fileName)
    {
        return _cTools.Delete(afterRootPath, fileName);
    }

    public bool CheckConnection()
    {
        return _cTools.CheckConnection();
    }

    public override bool ContainsFile(string fileName)
    {
        return _cTools.FileExists(null, fileName);
    }

    public override bool DirectoryExists(string directoryName)
    {
        return _cTools.DirectoryExists(null, directoryName);
    }


    public override bool DirectoryExists(string? afterRootPath, string directoryName)
    {
        return _cTools.DirectoryExists(afterRootPath, directoryName);
    }

    public override bool FileExists(string? afterRootPath, string fileName)
    {
        return _cTools.FileExists(afterRootPath, fileName);
    }

    public override MyFileInfo? GetOneFileWithInfo(string? afterRootPath, string fileName)
    {
        var files = _cTools.GetFilesWithInfo(afterRootPath, null, true);
        return files.SingleOrDefault(x => x.FileName == fileName);
    }


    //public override string GetPath(string afterRootPath, string fileName)
    //{
    //    return PathCombine(PathCombine(_serverRootPaths, afterRootPath), fileName);
    //}

    public override bool CreateDirectory(string directoryName)
    {
        _cTools.CreateDirectory(null, directoryName);
        return DirectoryExists(directoryName);
    }

    public override bool CreateDirectory(string? afterRootPath, string directoryName)
    {
        _cTools.CreateDirectory(afterRootPath, directoryName);
        return DirectoryExists(afterRootPath, directoryName);
    }

    public override bool CreateFolderIfNotExists(string directoryName)
    {
        if (DirectoryExists(directoryName))
            return true;
        CreateDirectory(directoryName);
        return DirectoryExists(directoryName);
    }

    public override bool DeleteDirectory(string? afterRootPath, string directoryName, bool recursive = false)
    {
        var nextDirName = afterRootPath != null ? PathCombine(afterRootPath, directoryName) : directoryName;
        if (recursive)
        {
            var folders = GetFolderNames(nextDirName, null);
            if (folders.Any(folder => !DeleteDirectory(nextDirName, folder, true)))
                return false;

            var files = GetFileNames(nextDirName, null);
            if (files.Any(delFileName => !DeleteFile(nextDirName, delFileName)))
                return false;
        }

        _cTools.DeleteDirectory(afterRootPath, directoryName);
        return !DirectoryExists(afterRootPath, directoryName);
    }

    //public override bool DeleteDirectory(string? afterRootPath, bool recursive = false)
    //{
    //    if (recursive)
    //    {
    //        List<string> folders = GetFolderNames(afterRootPath, null);
    //        if (folders.Any(folder => !DeleteDirectory(afterRootPath, folder, true)))
    //            return false;

    //        List<string> files = GetFileNames(afterRootPath, null);
    //        if (files.Select(file => GetPath(afterRootPath, file))
    //            .Any(delFileName => !DeleteFile(delFileName)))
    //            return false;

    //    }

    //    //string delDirName = PathCombine(_serverRootPaths, afterRootPath);
    //    _cTools.DeleteDirectory(afterRootPath);
    //    return !DirectoryExists(afterRootPath);
    //}
}