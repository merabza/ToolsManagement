using System;
using System.Collections.Generic;
using System.Linq;
using ConnectionTools.ConnectTools;
using ParametersManagement.LibFileParameters.Models;
using SystemTools.SystemToolsShared;

namespace ToolsManagement.FileManagersMain;

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
        {
            return false;
        }

        Console.WriteLine(_description);
        bool toReturn = ProcessFolder();
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
            bool reloadFolders = true;
            while (reloadFolders)
            {
                List<string> folderNames = FileManager.GetFolderNames(afterRootPath, null)
                    .Where(x => !x.StartsWith('#')).OrderBy(o => o).ToList();
                reloadFolders = false;
                foreach (string folderName in folderNames)
                {
                    (bool success, bool folderNameChanged, bool continueWithThisFolder) =
                        ProcessOneFolder(afterRootPath, folderName);
                    if (folderNameChanged)
                    {
                        reloadFolders = true;
                        break;
                    }

                    if (!success)
                    {
                        return false;
                    }

                    if (!continueWithThisFolder)
                    {
                        continue;
                    }

                    string folderAfterRootFullName = FileManager.PathCombine(afterRootPath, folderName);
                    if (!ProcessFolder(folderAfterRootFullName))
                    {
                        return false;
                    }
                }
            }
        }

        if (_useProcessFiles && !ProcessFiles(afterRootPath))
        {
            return false;
        }

        if (!_deleteEmptyFolders)
        {
            return true;
        }

        //შევამოწმოთ დაცარიელდა თუ არა ფოლდერი და თუ დაცარიელდა, წავშალოთ
        if (afterRootPath != null && FileManager.IsFolderEmpty(afterRootPath))
        {
            FileManager.DeleteDirectory(afterRootPath);
        }

        return true;
    }

    //protected virtual RecursiveParameters? CountNextRecursiveParameters(RecursiveParameters? recursiveParameters,
    //    string folderName)
    //{
    //    return null;
    //}

    //success, folderNameChanged, continueWithThisFolder
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
        return FileManager.GetFilesWithInfo(afterRootPath, _fileSearchPattern)
            .Where(file =>
                ExcludeSet == null || !ExcludeSet.NeedExclude(FileManager.PathCombine(afterRootPath, file.FileName)))
            .OrderBy(o => o.FileName).All(file => ProcessOneFile(afterRootPath, file));
    }

    protected static bool NeedExclude(string name, string[] excludes)
    {
        bool haveExclude = excludes is { Length: > 0 };
        return haveExclude && excludes.Any(name.FitsMask);
    }
}
