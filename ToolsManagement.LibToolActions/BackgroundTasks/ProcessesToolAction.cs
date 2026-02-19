using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SystemTools.SystemToolsShared;

namespace ToolsManagement.LibToolActions.BackgroundTasks;

public /*open*/ class ProcessesToolAction : ToolAction
{
    //საჭიროა ApAgent-ისთვის
    // ReSharper disable once MemberCanBePrivate.Global
    protected readonly ProcessManager? ProcessManager;

    protected ProcessesToolAction(ILogger logger, IMessagesDataManager? messagesDataManager, string? userName,
        ProcessManager? processManager, string actionName, int procLineId = 0) : base(logger, actionName,
        messagesDataManager, userName)
    {
        ProcessManager = processManager;
        ProcLineId = procLineId;
    }

    public int ProcLineId { get; }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        // ReSharper disable once using
        using Task task = Task.Run(async () =>
        {
            if (ProcessManager is not null && ProcessManager.CheckCancellation())
            {
                return;
            }

            if (!await Run(cancellationToken)) //თუ პროცესი ცუდად დასრულდა, ვჩერდებით
            {
                return;
            }

            //თუ პროცესი კარგად დასრულდა, გაეშვას შემდეგი პროცესი
            ProcessesToolAction? nextAction = GetNextAction();
            await RunNextAction(nextAction, cancellationToken);
        }, cancellationToken);
        await task.WaitAsync(cancellationToken);
    }

    //public საჭიროა ApAgent-ისათვის
    // ReSharper disable once MemberCanBeProtected.Global
    public virtual ProcessesToolAction? GetNextAction()
    {
        return null;
    }

    //protected საჭიროა ApAgent-ისათვის
    // ReSharper disable once MemberCanBePrivate.Global
    protected async ValueTask RunNextAction(ProcessesToolAction? nextToolAction,
        CancellationToken cancellationToken = default)
    {
        while (true)
        {
            if (ProcessManager is not null && ProcessManager.CheckCancellation())
            {
                return;
            }

            if (nextToolAction == null)
            {
                return;
            }

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
