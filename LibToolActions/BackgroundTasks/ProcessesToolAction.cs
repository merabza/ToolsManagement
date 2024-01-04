using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace LibToolActions.BackgroundTasks;

public /*open*/ class ProcessesToolAction : ToolAction
{
    private readonly ProcessManager? _processManager;

    protected ProcessesToolAction(ILogger logger, IMessagesDataManager? messagesDataManager, string? userName,
        ProcessManager? processManager, string actionName, int procLineId = 0) : base(logger, actionName,
        messagesDataManager, userName)
    {
        _processManager = processManager;
        ProcLineId = procLineId;
    }

    public int ProcLineId { get; }

    public Task RunAsync(CancellationToken cancellationToken)
    {
        var task = Task.Run(async () =>
        {
            if (_processManager is not null && _processManager.CheckCancellation())
                return;
            if (!await Run(cancellationToken)) //თუ პროცესი ცუდად დასრულდა, ვჩერდებით
                return;
            //თუ პროცესი კარგად დასრულდა, გაეშვას შემდეგი პროცესი
            var nextAction = GetNextAction();
            await RunNextAction(nextAction, cancellationToken);
        }, cancellationToken);
        return task;
    }

    protected virtual ProcessesToolAction? GetNextAction()
    {
        return null;
    }


    private async Task RunNextAction(ProcessesToolAction? nextToolAction, CancellationToken cancellationToken)
    {
        while (true)
        {
            if (_processManager is not null && _processManager.CheckCancellation())
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
                _processManager?.Run(nextToolAction);
            }

            break;
        }
    }
}