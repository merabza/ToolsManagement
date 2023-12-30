using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SystemToolsShared;
// ReSharper disable ConvertToPrimaryConstructor

namespace LibToolActions;

public /*open*/ class ToolAction
{
    private readonly string _actionName;
    protected readonly IMessagesDataManager? MessagesDataManager;
    protected readonly string? UserName;
    protected readonly ILogger Logger;


    protected ToolAction(ILogger logger, string actionName, IMessagesDataManager? messagesDataManager, string? userName)
    {
        Logger = logger;
        _actionName = actionName;
        MessagesDataManager = messagesDataManager;
        UserName = userName;
    }

    public async Task<bool> Run(CancellationToken cancellationToken)
    {
        try
        {
            if (!CheckValidate())
                return false;

            if (MessagesDataManager is not null)
                await MessagesDataManager.SendMessage(UserName, $"{_actionName} Started...", cancellationToken);
            Logger.LogInformation("{_actionName} Started...", _actionName);

            //დავინიშნოთ დრო პროცესისათვის
            var startDateTime = DateTime.Now;

            var success = await RunAction(cancellationToken);

            var timeTakenMessage = StShared.TimeTakenMessage(startDateTime);

            if (MessagesDataManager is not null)
                await MessagesDataManager.SendMessage(UserName, $"{_actionName} Finished. {timeTakenMessage}", cancellationToken);
            Logger.LogInformation("{_actionName} Finished. {timeTakenMessage}", _actionName, timeTakenMessage);

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