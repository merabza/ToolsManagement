using System;
using System.IO;
using FileManagersMain;
using LibFileParameters.Models;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace CompressionManagement;

public class Compressor
{
    private readonly bool _useConsole;
    private readonly ILogger _logger;
    private readonly SmartSchema _smartSchemaForLocal;
    private readonly string _middlePart;
    private readonly string[] _excludes;

    // ReSharper disable once ConvertToPrimaryConstructor
    public Compressor(bool useConsole, ILogger logger, SmartSchema smartSchemaForLocal, string middlePart,
        string[] excludes)
    {
        _useConsole = useConsole;
        _logger = logger;
        _smartSchemaForLocal = smartSchemaForLocal;
        _middlePart = middlePart;
        _excludes = excludes;
    }

    public bool CompressFolder(string sourceFolderFullPath, string localPath)
    {
        const string backupFileNameSuffix = ".zip";
        var archiver = ArchiverFactory.CreateArchiverByType(_useConsole, _logger, EArchiveType.ZipClass, null, null,
            backupFileNameSuffix);

        if (archiver is null)
        {
            StShared.WriteErrorLine("archiver does not created", true, _logger);
            return false;
        }

        const string dateMask = "yyyy_MM_dd_HHmmss";

        const string tempExtension = ".go!";
        var dir = new DirectoryInfo(sourceFolderFullPath);

        var backupFileNamePrefix = $"{dir.Name}{_middlePart}";

        var backupFileName = $"{backupFileNamePrefix}{DateTime.Now.ToString(dateMask)}{backupFileNameSuffix}";
        var backupFileFullName = Path.Combine(localPath, backupFileName);
        var tempFileName = $"{backupFileFullName}{tempExtension}";

        if (!archiver.SourcesToArchive([sourceFolderFullPath], tempFileName, _excludes))
        {
            File.Delete(tempFileName);
            return false;
        }

        File.Move(tempFileName, backupFileFullName);

        var localFileManager = FileManagersFactory.CreateFileManager(_useConsole, _logger, localPath);
        //წაიშალოს ადრე შექმნილი დაძველებული ფაილები

        if (localFileManager is null)
        {
            StShared.WriteErrorLine("localFileManager does not created", true, _logger);
            return false;
        }

        localFileManager.RemoveRedundantFiles(backupFileNamePrefix, dateMask, backupFileNameSuffix,
            _smartSchemaForLocal);

        return true;
    }
}