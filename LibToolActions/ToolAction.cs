using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibToolActions;

public /*open*/ class ToolAction
{
    protected readonly string ToolActionName;
    protected readonly ILogger Logger;
    protected readonly IMessagesDataManager? MessagesDataManager;
    protected readonly string? UserName;
    private readonly bool _useConsole;


    protected ToolAction(ILogger logger, string actionName, IMessagesDataManager? messagesDataManager, string? userName,
        bool useConsole = false)
    {
        Logger = logger;
        ToolActionName = actionName;
        MessagesDataManager = messagesDataManager;
        UserName = userName;
        _useConsole = useConsole;
    }

    public async Task<bool> Run(CancellationToken cancellationToken)
    {
        try
        {
            if (!CheckValidate())
                return false;

            await WriteMessage($"{ToolActionName} Started...", _useConsole, cancellationToken);

            //დავინიშნოთ დრო პროცესისათვის
            var startDateTime = DateTime.Now;

            var success = await RunAction(cancellationToken);

            var timeTakenMessage = StShared.TimeTakenMessage(startDateTime);

            await WriteMessage($"{ToolActionName} Finished. {timeTakenMessage}", _useConsole, cancellationToken);

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

    private async Task WriteMessage(string message, bool useConsole, CancellationToken cancellationToken)
    {
        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName, message, cancellationToken);
        if (useConsole)
            Console.WriteLine(message);
        else
            Logger.LogInformation(message);
    }

    //protected virtual string GetActionDescription()
    //{
    //    if (!string.IsNullOrWhiteSpace(_actionDescription) && _actionName != _actionDescription)
    //        return _actionDescription;
    //    return _actionName;
    //}

    protected virtual bool CheckValidate()
    {
        return true;
    }

    protected virtual Task<bool> RunAction(CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }
}