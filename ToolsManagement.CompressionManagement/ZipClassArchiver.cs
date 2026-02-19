using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
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
        {
            return false;
        }

        //დავადგინოთ გვაქვს თუ არა გამორიცხვები გამოყენებული.
        //დავადგინოთ ყველა ფაილების ჯამური სიგრძე (რომელიც უნდა შევიდეს არქივში)
        if (UseConsole)
        {
            Console.WriteLine($"Creating archive file {archiveFileName}");
        }

        // ReSharper disable once using
        // ReSharper disable once DisposableConstructor
        using var zipToOpen = new FileStream(archiveFileName, FileMode.CreateNew);
        // ReSharper disable once using
        // ReSharper disable once DisposableConstructor
        using var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create);
        foreach (string source in sources)
        {
            if (processManager is not null && processManager.CheckCancellation())
            {
                return false;
            }

            string? startPath = Path.GetDirectoryName(source);
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
                {
                    return false;
                }
            }
            else if (File.Exists(source))
            {
                // This path is a file
                var sourceFile = new FileInfo(source);
                if (!ArchiveFile(startPath, sourceFile, archive, excludes))
                {
                    return false;
                }
            }
            else
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("{Source} is not a valid file or directory.", source);
                }
            }
        }

        return true;
    }

    public async Task<bool> ArchiveToPathAsync(string archiveFileName, string path)
    {
        // ReSharper disable once using
        await using ZipArchive archive = await ZipFile.OpenReadAsync(archiveFileName);
        IEnumerable<ZipArchiveEntry> result =
            archive.Entries.Where(currentEntry => !string.IsNullOrWhiteSpace(currentEntry.FullName));

        foreach (ZipArchiveEntry entry in result)
        {
            string[] fileNameParts = entry.FullName.Split('/');
            string curPath = path;
            var curDirectory = new DirectoryInfo(curPath);
            for (int i = 0; i < fileNameParts.Length - 1; i++)
            {
                curPath = Path.Combine(curDirectory.FullName, fileNameParts[i]);
                curDirectory = Directory.CreateDirectory(curPath);
            }

            string fileName = fileNameParts[^1];
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                await entry.ExtractToFileAsync(Path.Combine(curDirectory.FullName, fileName));
            }
        }

        return true;
    }

    private bool ArchiveDirectory(string startPath, DirectoryInfo sourceDir, ZipArchive archive, string[] excludes,
        ProcessManager? processManager = null)
    {
        if (processManager is not null && processManager.CheckCancellation())
        {
            return false;
        }

        if (NeedExclude(sourceDir.FullName, excludes))
        {
            return true;
        }

        return sourceDir.GetDirectories().All(dir => ArchiveDirectory(startPath, dir, archive, excludes)) &&
               sourceDir.GetFiles().All(file => ArchiveFile(startPath, file, archive, excludes));
    }

    private static bool NeedExclude(string name, string[] excludes)
    {
        bool haveExclude = excludes is { Length: > 0 };

        return haveExclude && excludes.Any(name.FitsMask);
    }

    private bool ArchiveFile(string startPath, FileInfo file, ZipArchive archive, string[] excludes,
        ProcessManager? processManager = null)
    {
        if (processManager is not null && processManager.CheckCancellation())
        {
            return false;
        }

        if (NeedExclude(file.FullName, excludes))
        {
            return true;
        }

        string entryName = Path.GetRelativePath(startPath, file.FullName);

        ZipArchiveEntry fileEntry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        // ReSharper disable once using
        using FileStream inFile = file.OpenRead();
        // ReSharper disable once using
        using Stream entryStream = fileEntry.Open();

        //ნაწილ-ნაწილ ვარიანტი
        byte[] buffer = new byte[2048]; // read in chunks of 2KB
        try
        {
            int bytesRead;
            while ((bytesRead = inFile.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (processManager is not null && processManager.CheckCancellation())
                {
                    return false;
                }

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
