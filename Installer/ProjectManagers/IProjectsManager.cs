using LanguageExt;
using System.Threading;
using System.Threading.Tasks;
using SystemToolsShared.Errors;

namespace Installer.ProjectManagers;

public interface IProjectsManager
{
    //არასერვისი პროგრამებისათვის მოშორებული წაშლა არ მოხდება, რადგან ასეთი პროგრამებისათვის სერვერზე დაინსტალირება გათვალისწინებული არ გვაქვს
    //თუ მომავალში გადავაკეთებთ, ისე, რომ არასერვისული პროგრამებისათვის სერვერის მითითება შესაძლებელი იქნება და მოშორებულ სერვერზე ასეთი პროგრამის დაყენება შესაძლებელი იქნება, მაშინ RemoveProject უნდა აღდგეს
    //Task<bool> RemoveProject(string projectName);
    Task<Option<Err[]>> RemoveProjectAndService(string projectName, string environmentName, bool isService,
        CancellationToken cancellationToken);

    Task<Option<Err[]>> StopService(string projectName, string environmentName, CancellationToken cancellationToken);
    Task<Option<Err[]>> StartService(string projectName, string environmentName, CancellationToken cancellationToken);
}