using LibToolActions.BackgroundTasks;

namespace CompressionManagement;

public /*open*/ class Archiver
{
    protected readonly bool UseConsole;

    protected Archiver(bool useConsole, string fileExtension)
    {
        UseConsole = useConsole;
        FileExtension = fileExtension;
    }

    public string FileExtension { get; set; }

    public virtual bool SourcesToArchive(string[] sources, string archiveFileName, string[] excludes,
        ProcessManager? processManager = null)
    {
        return false;
    }

    public bool PathToArchive(string path, string archiveFileName)
    {
        return SourcesToArchive([path], archiveFileName, []);
    }

    public virtual bool ArchiveToPath(string archiveFileName, string path)
    {
        return false;
    }
}