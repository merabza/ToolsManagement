using System;
using System.Globalization;
using System.IO;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibFileParameters.Models;
using SystemTools.SystemToolsShared;
using ToolsManagement.FileManagersMain;

namespace ToolsManagement.CompressionManagement;

public sealed class Compressor
{
    private readonly string[] _excludes;
    private readonly ILogger _logger;
    private readonly string _middlePart;
    private readonly SmartSchema _smartSchemaForLocal;
    private readonly bool _useConsole;

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
        Archiver? archiver = ArchiverFactory.CreateArchiverByType(_useConsole, _logger, EArchiveType.ZipClass, null,
            null, backupFileNameSuffix);

        if (archiver is null)
        {
            StShared.WriteErrorLine("archiver does not created", true, _logger);
            return false;
        }

        const string dateMask = "yyyy_MM_dd_HHmmss";

        const string tempExtension = ".go!";
        var dir = new DirectoryInfo(sourceFolderFullPath);

        string backupFileNamePrefix = $"{dir.Name}{_middlePart}";

        string backupFileName =
            $"{backupFileNamePrefix}{DateTime.Now.ToString(dateMask, CultureInfo.InvariantCulture)}{backupFileNameSuffix}";
        string backupFileFullName = Path.Combine(localPath, backupFileName);
        string tempFileName = $"{backupFileFullName}{tempExtension}";

        if (!archiver.SourcesToArchive([sourceFolderFullPath], tempFileName, _excludes))
        {
            File.Delete(tempFileName);
            return false;
        }

        File.Move(tempFileName, backupFileFullName);

        FileManager? localFileManager = FileManagersFactory.CreateFileManager(_useConsole, _logger, localPath);
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
