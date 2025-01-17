using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace LibToolActions.BackgroundTasks;

//ეს კლასი გამოიყენება ApAgent-ში
public sealed class Processes : IProcesses
{
    private readonly ILogger _logger;

    private ProcessManager? _processManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public Processes(ILogger<Processes> logger)
    {
        _logger = logger;
    }

    public bool IsBusy()
    {
        return _processManager != null && _processManager.IsBusy();
    }

    public async ValueTask WaitForFinishAll()
    {
        if (_processManager != null)
            await _processManager.WaitForFinishAll();
    }

    public void CancelProcesses()
    {
        _processManager?.CancelProcesses();
        ClearProcessManager();
    }

    public ProcessManager GetNewProcessManager()
    {
        // ReSharper disable once DisposableConstructor
        _processManager = new ProcessManager(_logger);
        return _processManager;
    }

    public void ClearProcessManager()
    {
        _processManager?.Dispose();
        _processManager = null;
    }
}