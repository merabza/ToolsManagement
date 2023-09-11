using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace LibToolActions.BackgroundTasks;

public sealed class ProcessesToolActionsQueue
{
    private readonly SemaphoreSlim _signal = new(0);
    private readonly ConcurrentQueue<ProcessesToolAction> _workItems = new();

    public void QueueBackgroundToolAction(ProcessesToolAction toolAction)
    {
        if (toolAction == null) throw new ArgumentNullException(nameof(toolAction));

        _workItems.Enqueue(toolAction);
        _signal.Release();
    }

    public bool IsBusy()
    {
        return !_workItems.IsEmpty;
    }

    public async Task<ProcessesToolAction?> DequeueAsync(CancellationToken cancellationToken)
    {
        await _signal.WaitAsync(cancellationToken);
        _workItems.TryDequeue(out var workItem);
        return workItem;
    }
}