using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Microsoft.Extensions.Logging;
using SystemTools.SystemToolsShared;
using ToolsManagement.LibToolActions.BackgroundTasks;

namespace ToolsManagement.CompressionManagement;

public sealed class ZipClassArchiver : Archiver
{
    private readonly ILogger _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ZipClassArchiver(ILogger logger, bool useConsole, string fileExtension) : base(useConsole, fileExtension)
    {
        _logger = logger;
    }

    public override bool SourcesToArchive(string[] sources, string archiveFileName, string[] excludes,
        ProcessManager? processManager = null)
    {
        if (processManager is not null && processManager.CheckCancellation())
            return false;

        //დავადგინოთ გვაქვს თუ არა გამორიცხვები გამოყენებული.
        //დავადგინოთ ყველა ფაილების ჯამური სიგრძე (რომელიც უნდა შევიდეს არქივში)
        if (UseConsole)
            Console.WriteLine($"Creating archive file {archiveFileName}");

        // ReSharper disable once using
        // ReSharper disable once DisposableConstructor
        using var zipToOpen = new FileStream(archiveFileName, FileMode.CreateNew);
        // ReSharper disable once using
        // ReSharper disable once DisposableConstructor
        using var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create);
        foreach (var source in sources)
        {
            if (processManager is not null && processManager.CheckCancellation())
                return false;
            var startPath = Path.GetDirectoryName(source);
            if (startPath is null)
            {
                _logger.LogError("startPath is null");
                continue;
            }

            if (Directory.Exists(source))
            {
                // This path is a directory
                var sourceDir = new DirectoryInfo(source);
                if (!ArchiveDirectory(startPath, sourceDir, archive, excludes))
                    return false;
            }
            else if (File.Exists(source))
            {
                // This path is a file
                var sourceFile = new FileInfo(source);
                if (!ArchiveFile(startPath, sourceFile, archive, excludes))
                    return false;
            }
            else
            {
                _logger.LogInformation("{source} is not a valid file or directory.", source);
            }
        }

        return true;
    }

    public override bool ArchiveToPath(string archiveFileName, string path)
    {
        // ReSharper disable once using
        using var archive = ZipFile.OpenRead(archiveFileName);
        var result = archive.Entries.Where(currentEntry => !string.IsNullOrWhiteSpace(currentEntry.FullName));

        foreach (var entry in result)
        {
            var fileNameParts = entry.FullName.Split('/');
            var curPath = path;
            var curDirectory = new DirectoryInfo(curPath);
            for (var i = 0; i < fileNameParts.Length - 1; i++)
            {
                curPath = Path.Combine(curDirectory.FullName, fileNameParts[i]);
                curDirectory = Directory.CreateDirectory(curPath);
            }

            var fileName = fileNameParts[^1];
            if (!string.IsNullOrWhiteSpace(fileName))
                entry.ExtractToFile(Path.Combine(curDirectory.FullName, fileName));
        }

        return true;
    }

    private bool ArchiveDirectory(string startPath, DirectoryInfo sourceDir, ZipArchive archive, string[] excludes,
        ProcessManager? processManager = null)
    {
        if (processManager is not null && processManager.CheckCancellation())
            return false;

        if (NeedExclude(sourceDir.FullName, excludes))
            return true;

        return sourceDir.GetDirectories().All(dir => ArchiveDirectory(startPath, dir, archive, excludes)) &&
               sourceDir.GetFiles().All(file => ArchiveFile(startPath, file, archive, excludes));
    }

    private static bool NeedExclude(string name, string[] excludes)
    {
        var haveExclude = excludes is { Length: > 0 };

        return haveExclude && excludes.Any(name.FitsMask);
    }

    private bool ArchiveFile(string startPath, FileInfo file, ZipArchive archive, string[] excludes,
        ProcessManager? processManager = null)
    {
        if (processManager is not null && processManager.CheckCancellation())
            return false;

        if (NeedExclude(file.FullName, excludes))
            return true;

        var entryName = Path.GetRelativePath(startPath, file.FullName);

        var fileEntry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        // ReSharper disable once using
        using var inFile = file.OpenRead();
        // ReSharper disable once using
        using var entryStream = fileEntry.Open();

        //ნაწილ-ნაწილ ვარიანტი
        var buffer = new byte[2048]; // read in chunks of 2KB
        try
        {
            int bytesRead;
            while ((bytesRead = inFile.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (processManager is not null && processManager.CheckCancellation())
                    return false;

                entryStream.Write(buffer, 0, bytesRead);
            }

            return true;
        }
        catch (Exception e)
        {
            StShared.WriteException(e, UseConsole, _logger);
        }
        finally
        {
            inFile.Close();
            entryStream.Close();
        }

        return false;
    }
}