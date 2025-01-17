using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibToolActions.BackgroundTasks;

public sealed class ToolActionsQueuedRunner : BackgroundService
{
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _mainProcessSignal;

    private Task? _currentTask;

    public ToolActionsQueuedRunner(ProcessesToolActionsQueue toolActionsQueue, ILogger logger,
        SemaphoreSlim mainProcessSignal)
    {
        ToolActionsQueue = toolActionsQueue;
        _logger = logger;
        _mainProcessSignal = mainProcessSignal;
    }

    private ProcessesToolActionsQueue ToolActionsQueue { get; }

    public bool IsBusy { get; private set; }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Queued Hosted Service is starting.");

        while (!cancellationToken.IsCancellationRequested)
        {
            var toolAction = await ToolActionsQueue.DequeueAsync(cancellationToken);
            IsBusy = true;
            try
            {
                if (toolAction is not null)
                {
                    _currentTask = toolAction.RunAsync(cancellationToken);
                    await _currentTask;
                }

                _currentTask = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing toolAction.");
            }

            IsBusy = false;
            _mainProcessSignal.Release();
        }

        _mainProcessSignal.Release();

        _logger.LogInformation("Queued Hosted Service is stopping.");
    }

    public void WaitForFinish()
    {
        _currentTask?.Wait();
    }
}