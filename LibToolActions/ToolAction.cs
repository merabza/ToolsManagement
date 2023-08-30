using System;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

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

    public bool Run()
    {
        try
        {
            if (!CheckValidate())
                return false;

            MessagesDataManager?.SendMessage(UserName, $"{_actionName} Started...").Wait();
            Logger.LogInformation("{_actionName} Started...", _actionName);

            //დავინიშნოთ დრო პროცესისათვის
            var startDateTime = DateTime.Now;

            var success = RunAction();

            var timeTakenMessage = StShared.TimeTakenMessage(startDateTime);

            MessagesDataManager?.SendMessage(UserName, $"{_actionName} Finished. {timeTakenMessage}").Wait();
            Logger.LogInformation("{_actionName} Finished. {timeTakenMessage}", _actionName, timeTakenMessage);

            return success;
        }
        catch (OperationCanceledException e)
        {
            Logger.LogError(e, null);
        }
        catch (Exception e)
        {
            Logger.LogError(e, null);
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

    protected virtual bool RunAction()
    {
        return false;
    }
}