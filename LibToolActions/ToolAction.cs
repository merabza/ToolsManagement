using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace LibToolActions;

public /*open*/ class ToolAction : MessageLogger
{
    private readonly bool _useConsole;
    public readonly ILogger Logger;

    //protected საჭიროა ProcessorWorker პროექტისათვის
    // ReSharper disable once MemberCanBePrivate.Global
    protected readonly string ToolActionName;


    protected ToolAction(ILogger logger, string actionName, IMessagesDataManager? messagesDataManager, string? userName,
        bool useConsole = false) : base(logger, messagesDataManager, userName, useConsole)
    {
        Logger = logger;
        ToolActionName = actionName;
        _useConsole = useConsole;
    }

    public async Task<bool> Run(CancellationToken cancellationToken)
    {
        try
        {
            if (!CheckValidate())
                return false;

            await LogInfoAndSendMessage($"{ToolActionName} Started...", _useConsole, cancellationToken);

            //დავინიშნოთ დრო პროცესისათვის
            var startDateTime = DateTime.Now;

            var success = await RunAction(cancellationToken);

            var timeTakenMessage = StShared.TimeTakenMessage(startDateTime);

            await LogInfoAndSendMessage($"{ToolActionName} Finished. {timeTakenMessage}", _useConsole,
                cancellationToken);

            //StShared.Pause();

            return success;
        }
        catch (OperationCanceledException e)
        {
            Logger.LogError(e, "Operation Canceled");
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error when run Tool Action");
        }

        return false;
    }

    //გამოყენებულაი SupportTools-ში
    protected virtual bool CheckValidate()
    {
        return true;
    }

    protected virtual Task<bool> RunAction(CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }
}