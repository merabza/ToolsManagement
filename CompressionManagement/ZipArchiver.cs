using LibToolActions.BackgroundTasks;
using Microsoft.Extensions.Logging;

namespace CompressionManagement;

//Zip არქივატორი. გამოიყენება ReServer პროექტში
public sealed class ZipArchiver : Archiver
{
    private readonly ILogger _logger;

    public ZipArchiver(ILogger logger, string compressProgramPatch, string decompressProgramPatch, bool useConsole,
        string fileExtension) : base(useConsole, fileExtension)
    {
        _logger = logger;
    }

    public override bool SourcesToArchive(string[] sources, string archiveFileName, string[] excludes,
        ProcessManager? processManager = null)
    {
        return false;
    }
}