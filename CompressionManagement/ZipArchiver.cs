using LibToolActions.BackgroundTasks;

namespace CompressionManagement;

//Zip არქივატორი. გამოიყენება ReServer პროექტში
public sealed class ZipArchiver : Archiver
{

    // ReSharper disable once ConvertToPrimaryConstructor
    public ZipArchiver(bool useConsole, string fileExtension) : base(useConsole, fileExtension)
    {
    }

    public override bool SourcesToArchive(string[] sources, string archiveFileName, string[] excludes,
        ProcessManager? processManager = null)
    {
        return false;
    }
}