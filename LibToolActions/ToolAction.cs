using System;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace LibToolActions;

public /*open*/ class ToolAction
{
    private readonly string _actionName;
    protected readonly ILogger Logger;
    protected readonly bool UseConsole;


    protected ToolAction(ILogger logger, bool useConsole, string actionName)
    {
        UseConsole = useConsole;
        Logger = logger;
        _actionName = actionName;
    }

    public bool Run()
    {
        try
        {
            if (!CheckValidate())
                return false;

            //if (UseConsole && _askRunAction)
            //{
            //    Console.WriteLine(GetActionDescription());

            //    if (!Inputer.InputBool("Are you sure, you want to run this action", true, false))
            //        return false;
            //}

            Logger.LogInformation("{_actionName} Started...", _actionName);

            //დავინიშნოთ დრო პროცესისათვის
            var startDateTime = DateTime.Now;

            var success = RunAction();

            var timeTakenMessage = StShared.TimeTakenMessage(startDateTime);
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