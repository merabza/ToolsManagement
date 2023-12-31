﻿using System.Threading;
using Microsoft.Extensions.Logging;

namespace LibToolActions.BackgroundTasks;

public sealed class ProcessLine
{
    private readonly ProcessesToolActionsQueue _queue;
    private readonly ToolActionsQueuedRunner _runner;

    public ProcessLine(ILogger logger, SemaphoreSlim mainProcessSignal)
    {
        _queue = new ProcessesToolActionsQueue();
        _runner = new ToolActionsQueuedRunner(_queue, logger, mainProcessSignal);
    }

    public bool IsBusy => _queue.IsBusy() || _runner.IsBusy;

    internal void StartAsync(CancellationToken token)
    {
        _runner.StartAsync(token);
    }

    internal void QueueBackgroundToolAction(ProcessesToolAction toolAction)
    {
        _queue.QueueBackgroundToolAction(toolAction);
    }

    public void WaitForFinish()
    {
        _runner.WaitForFinish();
    }
}