using System.Threading.Tasks;

namespace ToolsManagement.LibToolActions.BackgroundTasks;

public interface IProcesses
{
    bool IsBusy();
    Task WaitForFinishAll();
    void CancelProcesses();
    ProcessManager GetNewProcessManager();
}
