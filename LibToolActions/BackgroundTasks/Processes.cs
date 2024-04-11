//using System.Threading.Tasks;
//using Microsoft.Extensions.Logging;

//namespace LibToolActions.BackgroundTasks;

//public sealed class Processes : IProcesses
//{
//    private readonly ILogger _logger;

//    private ProcessManager? _processManager;

//    // ReSharper disable once ConvertToPrimaryConstructor
//    public Processes(ILogger<Processes> logger)
//    {
//        _logger = logger;
//    }

//    public bool IsBusy()
//    {
//        return _processManager != null && _processManager.IsBusy();
//    }

//    public async Task WaitForFinishAll()
//    {
//        if (_processManager != null)
//            await _processManager.WaitForFinishAll();
//    }

//    public void CancelProcesses()
//    {
//        _processManager?.CancelProcesses();
//        ClearProcessManager();
//    }

//    public ProcessManager GetNewProcessManager()
//    {
//        _processManager = new ProcessManager(_logger);
//        return _processManager;
//    }

//    public void ClearProcessManager()
//    {
//        _processManager?.Dispose();
//        _processManager = null;
//    }
//}