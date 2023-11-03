using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConnectTools;
using LibFileParameters.Models;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using SystemToolsShared;

namespace FileManagersMain;

public /*open*/ class FileManager
{
    protected readonly string? LocalPatch;
    protected readonly ILogger Logger;
    protected readonly bool UseConsole;

    protected FileManager(bool useConsole, ILogger logger, string? localPatch)
    {
        UseConsole = useConsole;
        Logger = logger;
        LocalPatch = localPatch;
    }

    //public for UsbCopyRunner
    public virtual char DirectorySeparatorChar => Path.DirectorySeparatorChar;

    public virtual List<MyFileInfo> GetFilesWithInfo(string? afterRootPath, string? searchPattern)
    {
        return new List<MyFileInfo>();
    }

    //public for UsbCopyRunner
    public virtual List<string> GetFileNames(string? relativePath, string? searchPattern)
    {
        return new List<string>();
    }

    //public for UsbCopyRunner
    public virtual List<string> GetFolderNames(string? afterRootPath, string? searchPattern)
    {
        return new List<string>();
    }

    //ფაილის წაშლა სახელის მითითებით
    protected virtual bool DeleteFile(string fileName)
    {
        return false;
    }

    //ფაილის წაშლა სახელის მითითებით
    public virtual bool DeleteFile(string? afterRootPath, string fileName)
    {
        return false;
    }

    //ფაილის ატვირთვა
    public virtual bool UploadFile(string filename, string useTempExtension)
    {
        return false;
    }

    public virtual bool UploadFile(string? localAfterRootPath, string fileName, string? afterRootPath,
        string upFileName, string useTempExtension)
    {
        return false;
    }

    //ტექსტის ატვირთვა ფაილის სახით
    public virtual bool UploadContentToTextFile(string content, string serverSideFileName)
    {
        return false;
    }

    //სავარაუდოდ საჭირო იქნება ფაილის ჩამოტვირთვა
    public virtual bool DownloadFile(string filename, string useTempExtension, string? afterRootPath = null)
    {
        return false;
    }

    //სავარაუდოდ საჭირო იქნება ფაილის ჩამოტვირთვა
    public virtual bool DownloadFile(string? remoteAfterRootPath, string filename, string? localAfterRootPath,
        string downFileName, string useTempExtension)
    {
        return false;
    }

    public virtual string? GetTextFileContent(string fileName)
    {
        return null;
    }

    public virtual bool RenameFile(string? afterRootPath, string fileName, string newFileName)
    {
        return false;
    }

    public virtual bool RenameFolder(string? afterRootPath, string folderName, string newFolderName)
    {
        return false;
    }

    public BuFileInfo? GetLastFileInfo(string prefix, string dateMask, string suffix)
    {
        return GetFilesByMask(prefix, dateMask, suffix).MaxBy(ob => ob.FileDateTime);
    }

    public void RemoveRedundantFiles(string prefix, string dateMask, string suffix, SmartSchema? smartSchema)
    {
        if (smartSchema is null)
        {
            Logger.LogWarning("Invalid Smart Schema Cannot Delete old file.");
            return;
        }

        //წაშლა ჭკვიანი სქემის მიხედვით
        var files = GetFilesByMask(prefix, dateMask, suffix).OrderBy(ob => ob.FileDateTime).ToList();
        var filesForDelete = smartSchema.GetFilesForDeleteBySchema(files);

        foreach (var buFileInfo in filesForDelete)
        {
            var fileName = buFileInfo.FileName;
            DeleteFile(buFileInfo.FileName);
            Logger.LogInformation("Deleted old file {fileName}.", fileName);
        }
    }

    public virtual bool ContainsFile(string fileName)
    {
        return File.Exists(fileName);
    }

    private static DateTime GetDateTimeByMask(string fileName, string prefix, string mask, string suffix)
    {
        if (!fileName.StartsWith(prefix) || !fileName.EndsWith(suffix))
            return DateTime.MinValue;

        var strDate = fileName.Substring(prefix.Length, fileName.Length - prefix.Length - suffix.Length);
        //Console.WriteLine($"GetDateTimeByMask file Name {fileName}, date string {strDate}");
        return DateTime.ParseExact(strDate, mask, CultureInfo.InvariantCulture);
    }

    public string PathCombine(string? path1, string? path2)
    {
        if (path1 is null && path2 is not null)
            return path2;
        if (path2 is null && path1 is not null)
            return path1;
        if (path1 is not null && path2 is not null)
            return path1.AddNeedLastPart(DirectorySeparatorChar) + path2;
        throw new Exception("Both paths are null in PathCombine");
    }

    private static string GetFullMask(string prefix, string dateMask, string suffix)
    {
        return prefix + GetMasked(dateMask) + suffix;
    }

    private static string GetMasked(string str)
    {
        StringBuilder sb = new();
        foreach (var c in str) sb.Append(c != '_' ? '?' : c);

        return sb.ToString();
    }

    public BuFileInfo[] GetFilesByMask(int fileManagerId, MaskManager maskManager, string extension,
        bool isOriginal)
    {
        return GetFileNames(null, maskManager.GetFullMask(extension))
            .Select(c => new BuFileInfo(c, isOriginal, maskManager.GetDateTimeByMask(c), fileManagerId)).ToArray();
    }

    public List<BuFileInfo> GetFilesByMask(string prefix, string dateMask, string suffix)
    {
        var f = GetFileNames(null, GetFullMask(prefix, dateMask, suffix));

        var files = f
            .Select(fn => new BuFileInfo(fn, GetDateTimeByMask(fn, prefix, dateMask, suffix)))
            .Where(w => w.FileDateTime != DateTime.MinValue).ToList();

        return files;
    }

    protected static bool CopyFile(string fromFileName, string toFullName, bool useConsole, ILogger logger)
    {
        try
        {
            //File.Copy(fromFileName, toFullName);
            var pipeline = new ResiliencePipelineBuilder().AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Linear,
                Delay = TimeSpan.FromSeconds(1),
                ShouldHandle = new PredicateBuilder().Handle<DirectoryNotFoundException>(),
                OnRetry = retryArgs =>
                {
                    StShared.WriteErrorLine($"File copy Failed. currentAttempt: {retryArgs.AttemptNumber}", useConsole, logger, false);
                    StShared.WriteException(retryArgs.Outcome.Exception, useConsole, logger, false);
                    return ValueTask.CompletedTask;
                }
            }).Build();

            pipeline.Execute(() => File.Copy(fromFileName, toFullName));

            return true;
        }
        catch (Exception e)
        {
            StShared.WriteException(e, useConsole, logger, false);
            return false;
        }
    }

    protected static bool DeleteFile(string fileFullName, bool useConsole, ILogger logger)
    {
        try
        {
            //File.Delete(fileFullName);
            var pipeline = new ResiliencePipelineBuilder().AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Linear,
                Delay = TimeSpan.FromSeconds(1),
                ShouldHandle = new PredicateBuilder().Handle<DirectoryNotFoundException>(),
                OnRetry = retryArgs =>
                {
                    StShared.WriteErrorLine($"File delete Failed. currentAttempt: {retryArgs.AttemptNumber}", useConsole, logger, false);
                    StShared.WriteException(retryArgs.Outcome.Exception, useConsole, logger, false);
                    return ValueTask.CompletedTask;
                }
            }).Build();

            pipeline.Execute(() => File.Delete(fileFullName));

            return true;
        }
        catch (Exception e)
        {
            StShared.WriteException(e, useConsole, logger, false);
            return false;
        }
    }

    protected static bool RenameFile(string fromFileName, string toFullName, bool useConsole, ILogger logger)
    {
        try
        {
            //File.Move(fromFileName, toFullName);
            var pipeline = new ResiliencePipelineBuilder().AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Linear,
                Delay = TimeSpan.FromSeconds(1),
                ShouldHandle = new PredicateBuilder().Handle<DirectoryNotFoundException>(),
                OnRetry = retryArgs =>
                {
                    StShared.WriteErrorLine($"File move Failed. currentAttempt: {retryArgs.AttemptNumber}", useConsole, logger, false);
                    StShared.WriteException(retryArgs.Outcome.Exception, useConsole, logger, false);
                    return ValueTask.CompletedTask;
                }
            }).Build();

            pipeline.Execute(() => File.Move(fromFileName, toFullName));
            return true;
        }
        catch (Exception e)
        {
            StShared.WriteException(e, useConsole, logger, false);
            return false;
        }
    }

    protected static bool RenameFolder(string fromFolderName, string toFullName, bool useConsole, ILogger logger)
    {
        try
        {
            Directory.Move(fromFolderName, toFullName);
            return true;
        }
        catch (Exception e)
        {
            StShared.WriteException(e, useConsole, logger, false);
            return false;
        }
    }

    //public bool LocalFileExists(string fileName, string? afterRootPath)
    //{
    //    string? localAfterRootPath = afterRootPath?.Replace(DirectorySeparatorChar, Path.DirectorySeparatorChar);
    //    if (LocalPatch is null)
    //        throw new Exception("LocalPatch is null");
    //    string path = localAfterRootPath is null ? LocalPatch : Path.Combine(LocalPatch, localAfterRootPath);
    //    string fileFullName = Path.Combine(path, fileName);
    //    return File.Exists(fileFullName);
    //}

    public bool CareCreateDirectory(string newFolderName)
    {
        Console.WriteLine($"Care Create Directory {newFolderName}");
        //შევამოწმოთ ასატვირთ ფოლდერში თუ არსებობს შესაბამისი ახალი ფოლდერი.
        if (DirectoryExists(newFolderName))
        {
            //თუ არსებობს, ვჩერდებით
            Logger.LogWarning("Folder with name {newFolderName} already exists. Process Stopped", newFolderName);
            return false;
        }

        Console.WriteLine($"Directory {newFolderName} does not exists");
        //თუ არ არსებობს, ვქმნით
        if (!CreateDirectory(newFolderName))
        {
            //თუ ფოლდერი ვერ შეიქმნა, ვჩერდებით
            Logger.LogWarning("Folder with name {newFolderName} cannot created. Process Stopped", newFolderName);
            return false;
        }

        Console.WriteLine($"Directory {newFolderName} Created");

        return true;
    }

    public bool CareCreateDirectory(string? afterRootPath, string newFolderName, bool allowExist)
    {
        Console.WriteLine($"Care Create Directory {newFolderName} in {afterRootPath}");
        //შევამოწმოთ ასატვირთ ფოლდერში თუ არსებობს შესაბამისი ახალი ფოლდერი.
        if (DirectoryExists(afterRootPath, newFolderName))
        {
            if (allowExist)
                return true;
            //თუ არსებობს, ვჩერდებით
            Logger.LogWarning("Folder with name {newFolderName} already exists. Process Stopped", newFolderName);
            return false;
        }

        //თუ არ არსებობს, ვქმნით
        if (!CreateDirectory(afterRootPath, newFolderName))
        {
            //თუ ფოლდერი ვერ შეიქმნა, ვჩერდებით
            Logger.LogWarning("Folder with name {newFolderName} cannot created. Process Stopped", newFolderName);
            return false;
        }

        Console.WriteLine($"Directory {newFolderName} Created");
        return true;
    }


    public virtual bool CreateDirectory(string directoryName)
    {
        return false;
    }

    public virtual bool CreateDirectory(string? afterRootPath, string directoryName)
    {
        return false;
    }

    public virtual bool CreateFolderIfNotExists(string directoryName)
    {
        return false;
    }

    public virtual bool DeleteDirectory(string? afterRootPath, string folderName, bool recursive = false)
    {
        return false;
    }

    public virtual bool DeleteDirectory(string? afterRootPath, bool recursive = false)
    {
        return false;
    }

    public bool IsFolderEmpty(string? afterRootPath, string folderName)
    {
        var folder = afterRootPath != null ? PathCombine(afterRootPath, folderName) : folderName;
        return GetFileNames(folder, null).Count == 0 && GetFolderNames(folder, null).Count == 0;
    }

    public bool IsFolderEmpty(string? afterRootPath)
    {
        return GetFileNames(afterRootPath, null).Count == 0 && GetFolderNames(afterRootPath, null).Count == 0;
    }

    public virtual bool DirectoryExists(string directoryName)
    {
        return false;
    }

    public virtual bool DirectoryExists(string? afterRootPath, string directoryName)
    {
        return false;
    }

    public virtual bool FileExists(string? afterRootPath, string fileName)
    {
        return false;
    }

    public virtual MyFileInfo? GetOneFileWithInfo(string? afterRootPath, string fileName)
    {
        return null;
    }

    public virtual string GetPath(string? afterRootPath, string fileName)
    {
        throw new NotImplementedException();
    }
}