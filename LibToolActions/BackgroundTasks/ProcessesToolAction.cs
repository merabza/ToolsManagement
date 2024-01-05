using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace LibToolActions.BackgroundTasks;

public /*open*/ class ProcessesToolAction : ToolAction
{
    //საჭიროა ApAgent-ისთვის
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
        var task = Task.Run(async () =>
        {
            if (ProcessManager is not null && ProcessManager.CheckCancellation())
                return;
            if (!await Run(cancellationToken)) //თუ პროცესი ცუდად დასრულდა, ვჩერდებით
                return;
            //თუ პროცესი კარგად დასრულდა, გაეშვას შემდეგი პროცესი
            var nextAction = GetNextAction();
            await RunNextAction(nextAction, cancellationToken);
        }, cancellationToken);
        return task;
    }

    //public საჭიროა ApAgent-ისათვის
    public virtual ProcessesToolAction? GetNextAction()
    {
        return null;
    }

    //protected საჭიროა ApAgent-ისათვის
    protected async Task RunNextAction(ProcessesToolAction? nextToolAction, CancellationToken cancellationToken)
    {
        while (true)
        {
            if (ProcessManager is not null && ProcessManager.CheckCancellation())
                return;
            if (nextToolAction == null)
                return;
            if (ProcLineId == nextToolAction.ProcLineId)
            {
                if (await nextToolAction.Run(cancellationToken))
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