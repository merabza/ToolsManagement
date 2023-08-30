using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace LibToolActions.BackgroundTasks;

public /*open*/ class ProcessesToolAction : ToolAction
{
    protected readonly ProcessManager? ProcessManager;

    protected ProcessesToolAction(ILogger logger, IMessagesDataManager? messagesDataManager, string? userName,
        ProcessManager? processManager, string actionName, int procLineId = 0) : base(logger, actionName,
        messagesDataManager, userName)
    {
        ProcessManager = processManager;
        ProcLineId = procLineId;
    }

    public int ProcLineId { get; }

    public Task RunAsync(CancellationToken cancellationToken)
    {
        var task = Task.Run(() =>
        {
            if (ProcessManager is not null && ProcessManager.CheckCancellation())
                return;
            if (!Run()) //თუ პროცესი ცუდად დასრულდა, ვჩერდებით
                return;
            //თუ პროცესი კარგად დასრულდა, გაეშვას შემდეგი პროცესი
            var nextAction = GetNextAction();
            RunNextAction(nextAction);
        }, cancellationToken);
        return task;
    }

    public virtual ProcessesToolAction? GetNextAction()
    {
        return null;
    }


    protected void RunNextAction(ProcessesToolAction? nextToolAction)
    {
        while (true)
        {
            if (ProcessManager is not null && ProcessManager.CheckCancellation())
                return;
            if (nextToolAction == null)
                return;
            if (ProcLineId == nextToolAction.ProcLineId)
            {
                if (nextToolAction.Run())
                {
                    nextToolAction = nextToolAction.GetNextAction();
                    continue;
                }
            }
            else
            {
                ProcessManager?.Run(nextToolAction);
            }

            break;
        }
    }
}