using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ConnectTools;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace FileManagersMain;

public sealed class DiskFileManager : FileManager
{
    private readonly string _storageFolderName;

    public DiskFileManager(string storageFolderName, bool useConsole, ILogger logger, string? localPatch) : base(
        useConsole, logger, localPatch)
    {
        _storageFolderName = storageFolderName;
    }

    public override List<MyFileInfo> GetFilesWithInfo(string? afterRootPath, string? searchPattern)
    {
        var destDir = GetDirectoryInfo(afterRootPath);
        return GetFiles(searchPattern, destDir)
            .Select(c => new MyFileInfo(c.Name, c.Length)).ToList();
    }

    private static FileInfo[] GetFiles(string? searchPattern, DirectoryInfo destDir)
    {
        return searchPattern == null ? destDir.GetFiles() : destDir.GetFiles(searchPattern);
    }

    private static DirectoryInfo[] GetFolders(string? searchPattern, DirectoryInfo destDir)
    {
        return searchPattern == null ? destDir.GetDirectories() : destDir.GetDirectories(searchPattern);
    }

    //public for UsbCopyRunner
    public override List<string> GetFileNames(string? afterRootPath, string? searchPattern)
    {
        var destDir = GetDirectoryInfo(afterRootPath);
        return GetFiles(searchPattern, destDir).Select(c => c.Name).ToList();
    }

    private DirectoryInfo GetDirectoryInfo(string? afterRootPath)
    {
        return new DirectoryInfo(afterRootPath == null
            ? _storageFolderName
            : Path.Combine(_storageFolderName, afterRootPath));
    }

    //public for UsbCopyRunner
    public override List<string> GetFolderNames(string? afterRootPath, string? searchPattern)
    {
        var destDir = GetDirectoryInfo(afterRootPath);
        return GetFolders(searchPattern, destDir).Select(c => c.Name).ToList();
    }

    public override bool UploadFile(string fileName, string useTempExtension)
    {
        if (LocalPatch is null)
            throw new Exception("UploadFile: LocalPatch is null");

        var fileFullName = Path.Combine(LocalPatch, fileName);
        var tempFileName = fileName + useTempExtension.AddNeedLeadPart(".");

        return CopyFile(fileFullName, _storageFolderName, tempFileName, fileName);
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
        var copyToFolder = PathCombine(_storageFolderName, afterRootPath);

        return CopyFile(fileFullName, copyToFolder, tempFileName, upFileName);
    }

    private bool CopyFile(string fileFullName, string copyToFolder, string tempFileName, string fileName)
    {
        if (LocalPatch is null)
            throw new Exception("CopyFile: LocalPatch is null");

        if (FileStat.NormalizePath(LocalPatch) == FileStat.NormalizePath(copyToFolder))
            return File.Exists(fileFullName);

        Logger.LogInformation("Upload file from {fileFullName} to {copyToFolder}", fileFullName, copyToFolder);
        var tempFileFullName = Path.Combine(copyToFolder, tempFileName);
        var targetFileFullName = Path.Combine(copyToFolder, fileName);
        if (CopyFile(fileFullName, tempFileFullName, UseConsole, Logger) &&
            RenameFile(tempFileFullName, targetFileFullName, UseConsole, Logger))
            return true;

        Logger.LogError("Upload file finished with errors: form {fileFullName} to {targetFileFullName}", fileFullName,
            targetFileFullName);
        return false;
    }

    public override bool UploadContentToTextFile(string content, string serverSideFileName)
    {
        Logger.LogInformation("Uploading Parameters content to {_storageFolderName} in {serverSideFileName}",
            _storageFolderName, serverSideFileName);
        var targetFileFullName = Path.Combine(_storageFolderName, serverSideFileName);
        File.WriteAllText(targetFileFullName, content);
        //Logger.LogError("Upload file content finished with errors: in {serverSideFileName} to {_storageFolderName}",
        //    serverSideFileName, _storageFolderName);
        return true;
    }

    public override bool DownloadFile(string fileName, string useTempExtension, string? afterRootPath = null)
    {
        if (LocalPatch is null)
            throw new Exception("DownloadFile: LocalPatch is null");

        var fileFullName = Path.Combine(LocalPatch, fileName);
        if (FileStat.NormalizePath(LocalPatch) == FileStat.NormalizePath(_storageFolderName))
            return File.Exists(fileFullName);

        var tempFileName = fileName + useTempExtension.AddNeedLeadPart(".");
        var sourceFileFullName = Path.Combine(_storageFolderName, fileName);
        Logger.LogInformation("Downloading File from {sourceFileFullName} to {fileFullName}", sourceFileFullName,
            fileFullName);
        var tempFileFullName = Path.Combine(_storageFolderName, tempFileName);

        if (CopyFile(sourceFileFullName, tempFileFullName, UseConsole, Logger) &&
            RenameFile(tempFileFullName, fileFullName, UseConsole, Logger))
            return true;

        Logger.LogError("Downloading file was finished with errors: from {sourceFileFullName} to {fileFullName}",
            sourceFileFullName, fileFullName);
        return false;
    }

    public override string GetTextFileContent(string fileName)
    {
        Logger.LogInformation("Get content from text file {fileName}", fileName);
        StreamReader reader = new(fileName);
        return reader.ReadToEnd();
    }

    public override bool RenameFile(string? afterRootPath, string fileName, string newFileName)
    {
        var folderPath = afterRootPath == null
            ? _storageFolderName
            : Path.Combine(_storageFolderName, afterRootPath);

        return RenameFile(Path.Combine(folderPath, fileName), Path.Combine(folderPath, newFileName), UseConsole,
            Logger);
    }

    public override bool RenameFolder(string? afterRootPath, string folderName, string newFolderName)
    {
        var folderPath = afterRootPath == null
            ? _storageFolderName
            : Path.Combine(_storageFolderName, afterRootPath);

        return RenameFolder(Path.Combine(folderPath, folderName), Path.Combine(folderPath, newFolderName), UseConsole,
            Logger);
    }

    protected override bool DeleteFile(string fileName)
    {
        return DeleteFile(Path.Combine(_storageFolderName, fileName), UseConsole, Logger);
    }

    //ფაილის წაშლა სახელის მითითებით
    public override bool DeleteFile(string? afterRootPath, string fileName)
    {
        return DeleteFile(GetPath(afterRootPath, fileName), UseConsole, Logger);
    }

    public override bool ContainsFile(string fileName)
    {
        return File.Exists(Path.Combine(_storageFolderName, fileName));
    }

    public override bool DirectoryExists(string directoryName)
    {
        return Directory.Exists(Path.Combine(_storageFolderName, directoryName));
    }

    public override bool DirectoryExists(string? afterRootPath, string directoryName)
    {
        return Directory.Exists(GetPath(afterRootPath, directoryName));
    }

    public override bool FileExists(string? afterRootPath, string fileName)
    {
        return File.Exists(GetPath(afterRootPath, fileName));
    }

    public override string GetPath(string? afterRootPath, string fileName)
    {
        return afterRootPath == null
            ? Path.Combine(_storageFolderName, fileName)
            : Path.Combine(_storageFolderName, afterRootPath, fileName);
    }

    public override bool CreateDirectory(string directoryName)
    {
        Directory.CreateDirectory(Path.Combine(_storageFolderName, directoryName));
        return DirectoryExists(directoryName);
    }

    public override bool CreateDirectory(string? afterRootPath, string directoryName)
    {
        var inDirName = afterRootPath == null
            ? _storageFolderName
            : Path.Combine(_storageFolderName, afterRootPath);
        var dirFullName = Path.Combine(inDirName, directoryName);
        Directory.CreateDirectory(dirFullName);
        return DirectoryExists(dirFullName);
    }

    public override bool CreateFolderIfNotExists(string directoryName)
    {
        var dirFullName = Path.Combine(_storageFolderName, directoryName);
        if (Directory.Exists(dirFullName))
            return true;
        Directory.CreateDirectory(dirFullName);
        return DirectoryExists(dirFullName);
    }

    public override bool DeleteDirectory(string? afterRootPath, string directoryName, bool recursive = false)
    {
        var inDirName = afterRootPath != null
            ? Path.Combine(_storageFolderName, afterRootPath)
            : _storageFolderName;
        var dirFullName = Path.Combine(inDirName, directoryName);
        Directory.Delete(dirFullName, recursive);
        return !DirectoryExists(dirFullName);
    }

    //public override bool DeleteDirectory(string afterRootPath, bool recursive = false)
    //{
    //    string inDirName = afterRootPath != null
    //        ? Path.Combine(_storageFolderName, afterRootPath)
    //        : _storageFolderName;
    //    Directory.Delete(inDirName, recursive);
    //    return !DirectoryExists(inDirName);
    //}

    public override MyFileInfo? GetOneFileWithInfo(string? afterRootPath, string fileName)
    {
        var file = new FileInfo(GetPath(afterRootPath, fileName));
        return !file.Exists ? null : new MyFileInfo(file.Name, file.Length);
    }
}