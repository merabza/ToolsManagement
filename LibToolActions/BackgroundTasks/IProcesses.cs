using System.Threading.Tasks;

namespace LibToolActions.BackgroundTasks;

public interface IProcesses
{
    bool IsBusy();
    Task WaitForFinishAll();
    void CancelProcesses();
    ProcessManager GetNewProcessManager();
}