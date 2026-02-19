using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ToolsManagement.LibToolActions.BackgroundTasks;

public sealed class ProcessesToolActionsQueue : IDisposable
{
    private readonly SemaphoreSlim _signal = new(0);
    private readonly ConcurrentQueue<ProcessesToolAction> _workItems = new();

    public void Dispose()
    {
        _signal.Dispose();
    }

    public void QueueBackgroundToolAction(ProcessesToolAction toolAction)
    {
        ArgumentNullException.ThrowIfNull(toolAction);

        _workItems.Enqueue(toolAction);
        _signal.Release();
    }

    public bool IsBusy()
    {
        return !_workItems.IsEmpty;
    }

    public async Task<ProcessesToolAction?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        await _signal.WaitAsync(cancellationToken);
        _workItems.TryDequeue(out ProcessesToolAction? workItem);
        return workItem;
    }
}
