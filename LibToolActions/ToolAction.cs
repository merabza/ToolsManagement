using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace LibToolActions;

public /*open*/ class ToolAction : MessageLogger
{
    public readonly ILogger? Logger;

    //protected საჭიროა ProcessorWorker პროექტისათვის
    // ReSharper disable once MemberCanBePrivate.Global
    protected readonly string ToolActionName;
    protected readonly bool UseConsole;


    protected ToolAction(ILogger? logger, string actionName, IMessagesDataManager? messagesDataManager,
        string? userName, bool useConsole = false) : base(logger, messagesDataManager, userName, useConsole)
    {
        Logger = logger;
        ToolActionName = actionName;
        UseConsole = useConsole;
    }

    public async ValueTask<bool> Run(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!CheckValidate())
                return false;

            await LogInfoAndSendMessage($"{ToolActionName} Started...", UseConsole, cancellationToken);

            //დავინიშნოთ დრო პროცესისათვის
            var startDateTime = DateTime.Now;

            var success = await RunAction(cancellationToken);

            var timeTakenMessage = StShared.TimeTakenMessage(startDateTime);

            await LogInfoAndSendMessage($"{ToolActionName} Finished. {timeTakenMessage}", UseConsole,
                cancellationToken);

            //StShared.Pause();

            return success;
        }
        catch (OperationCanceledException)
        {
            StShared.WriteErrorLine("Operation Canceled", UseConsole, Logger);
        }
        catch (Exception e)
        {
            StShared.WriteErrorLine($"Error when run Tool Action: {e.Message}", UseConsole, Logger);
        }

        return false;
    }

    //გამოყენებულაი SupportTools-ში
    protected virtual bool CheckValidate()
    {
        return true;
    }

    protected virtual Task<bool> RunAction(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}