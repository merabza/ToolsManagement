using System.Threading.Tasks;

namespace LibToolActions.BackgroundTasks;

public interface IProcesses
{
    bool IsBusy();
    ValueTask WaitForFinishAll();
    void CancelProcesses();
    ProcessManager GetNewProcessManager();
}