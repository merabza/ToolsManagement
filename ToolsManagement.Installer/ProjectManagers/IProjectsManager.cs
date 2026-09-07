using System.Threading;
using System.Threading.Tasks;
using SystemTools.SharedKernel;

namespace ToolsManagement.Installer.ProjectManagers;

public interface IProjectsManager
{
    //არასერვისი პროგრამებისათვის მოშორებული წაშლა არ მოხდება, რადგან ასეთი პროგრამებისათვის სერვერზე დაინსტალირება გათვალისწინებული არ გვაქვს
    //თუ მომავალში გადავაკეთებთ, ისე, რომ არასერვისული პროგრამებისათვის სერვერის მითითება შესაძლებელი იქნება და მოშორებულ სერვერზე ასეთი პროგრამის დაყენება შესაძლებელი იქნება, მაშინ RemoveProject უნდა აღდგეს
    //Task<bool> RemoveProject(string projectName);
    ValueTask<Result> RemoveProjectAndService(string projectName, string environmentName, bool isService,
        CancellationToken cancellationToken = default);

    ValueTask<Result> StopService(string projectName, string environmentName,
        CancellationToken cancellationToken = default);

    ValueTask<Result> StartService(string projectName, string environmentName,
        CancellationToken cancellationToken = default);
}
