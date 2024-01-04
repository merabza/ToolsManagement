using System;
using System.Linq;
using ConnectTools;
using LibFileParameters.Models;
using SystemToolsShared;

namespace FileManagersMain;

public /*open*/ class FolderProcessor
{
    private readonly bool _deleteEmptyFolders;
    private readonly string _description;

    private readonly string? _fileSearchPattern;

    private readonly string _name;
    private readonly bool _useProcessFiles;
    private readonly bool _useSubFolders;
    protected readonly ExcludeSet? ExcludeSet;
    protected readonly FileManager FileManager;

    protected FolderProcessor(string name, string description, FileManager fileManager, string? fileSearchPattern,
        bool deleteEmptyFolders, ExcludeSet? excludeSet, bool useSubFolders, bool useProcessFiles)
    {
        _name = name;
        _description = description;
        FileManager = fileManager;
        _fileSearchPattern = fileSearchPattern;
        _deleteEmptyFolders = deleteEmptyFolders;
        ExcludeSet = excludeSet;
        _useSubFolders = useSubFolders;
        _useProcessFiles = useProcessFiles;
    }

    protected virtual bool CheckParameters()
    {
        return true;
    }

    public bool Run()
    {
        if (!CheckParameters())
            return false;
        Console.WriteLine(_description);
        var toReturn = ProcessFolder();
        Finish();
        return toReturn;
    }

    protected virtual void Finish()
    {
    }

    private bool ProcessFolder(string? afterRootPath = null)
    {
        Console.WriteLine($"({_name}) Process Folder {afterRootPath}");

        if (_useSubFolders)
        {
            var reloadFolders = true;
            while (reloadFolders)
            {
                var folderNames = FileManager.GetFolderNames(afterRootPath, null)
                    .Where(x => !x.StartsWith("#")).OrderBy(o => o).ToList();
                reloadFolders = false;
                foreach (var folderName in folderNames)
                {
                    var (success, folderNameChanged, continueWithThisFolder) =
                        ProcessOneFolder(afterRootPath, folderName);
                    if (folderNameChanged)
                    {
                        reloadFolders = true;
                        break;
                    }

                    if (!success)
                        return false;

                    if (!continueWithThisFolder)
                        continue;

                    var folderAfterRootFullName = FileManager.PathCombine(afterRootPath, folderName);
                    if (!ProcessFolder(folderAfterRootFullName))
                        return false;
                }
            }
        }

        if (_useProcessFiles && !ProcessFiles(afterRootPath))
            return false;

        if (!_deleteEmptyFolders)
            return true;

        //შევამოწმოთ დაცარიელდა თუ არა ფოლდერი და თუ დაცარიელდა, წავშალოთ
        if (afterRootPath != null && FileManager.IsFolderEmpty(afterRootPath))
            FileManager.DeleteDirectory(afterRootPath);

        return true;
    }

    //protected virtual RecursiveParameters? CountNextRecursiveParameters(RecursiveParameters? recursiveParameters,
    //    string folderName)
    //{
    //    return null;
    //}

    protected virtual (bool, bool, bool) ProcessOneFolder(string? afterRootPath, string folderName)
    {
        return (true, false, true);
    }

    protected virtual bool ProcessOneFile(string? afterRootPath, MyFileInfo file)
    {
        return true;
    }

    private bool ProcessFiles(string? afterRootPath)
    {
        return FileManager.GetFilesWithInfo(afterRootPath, _fileSearchPattern).OrderBy(o => o.FileName)
            .Where(file =>
                ExcludeSet == null ||
                !ExcludeSet.NeedExclude(FileManager.PathCombine(afterRootPath, file.FileName)))
            .All(file => ProcessOneFile(afterRootPath, file));
    }


    protected static bool NeedExclude(string name, string[] excludes)
    {
        var haveExclude = excludes is { Length: > 0 };
        return haveExclude && excludes.Any(name.FitsMask);
    }
}