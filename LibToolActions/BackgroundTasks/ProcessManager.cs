using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LibToolActions.BackgroundTasks;

public sealed class ProcessManager : IDisposable
{
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<int, ProcessLine> _processLines;
    private readonly SemaphoreSlim _signal = new(0);
    private readonly CancellationTokenSource _source;

    public ProcessManager(ILogger logger)
    {
        _logger = logger;
        _processLines = new ConcurrentDictionary<int, ProcessLine>();
        // ReSharper disable once DisposableConstructor
        _source = new CancellationTokenSource();
        CancellationToken = _source.Token;
    }

    private CancellationToken CancellationToken { get; }

    public void Dispose()
    {
        _signal.Dispose();
        _source.Dispose();
    }

    public bool IsBusy()
    {
        return _processLines.Any(runner => runner.Value.IsBusy);
    }

    public async Task WaitForFinishAll()
    {
        while (IsBusy())
        {
            _logger.LogInformation("Steel waiting for ending process...");
            await _signal.WaitAsync(CancellationToken);
        }
    }

    public void CancelProcesses()
    {
        _source.Cancel();
        try
        {
            var processLines = _processLines.Values.ToArray();
            foreach (var processLine in processLines) processLine.WaitForFinish();
        }
        catch (OperationCanceledException e)
        {
            _logger.LogError(e, "Error when Cancel Processes");
        }
        finally
        {
            _source.Dispose();
        }
    }

    public void Run(ProcessesToolAction toolAction)
    {
        var procLineId = toolAction.ProcLineId;
        if (!_processLines.ContainsKey(procLineId))
        {
            // ReSharper disable once DisposableConstructor
            if (!_processLines.TryAdd(procLineId, new ProcessLine(_logger, _signal)))
            {
                _logger.LogError("Cannot add New Process Line with Id {procLineId}", procLineId);
                return;
            }

            _processLines[procLineId].StartAsync(CancellationToken);
        }

        _processLines[procLineId].QueueBackgroundToolAction(toolAction);
    }

    public bool CheckCancellation()
    {
        if (!CancellationToken.IsCancellationRequested)
            return false;

        Console.WriteLine("Task was cancelled before it got started.");
        return true;
    }
}