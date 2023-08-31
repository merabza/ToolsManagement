using System.Threading.Tasks;

namespace Installer.AgentClients;

public interface IProjectsApiClient
{
    //არასერვისი პროგრამებისათვის მოშორებული წაშლა არ მოხდება, რადგან ასეთი პროგრამებისათვის სერვერზე დაინსტალირება გათვალისწინებული არ გვაქვს
    //თუ მომავალში გადავაკეთებთ, ისე, რომ არასერვისული პროგრამებისათვის სერვერის მითითება შესაძლებელი იქნება და მოშორებულ სერვერზე ასეთი პროგრამის დაყენება შესაძლებელი იქნება, მაშინ RemoveProject უნდა აღდგეს
    //Task<bool> RemoveProject(string projectName);
    Task<bool> RemoveProjectAndService(string projectName, string serviceName, string environmentName);
    Task<bool> StopService(string serviceName, string environmentName);
    Task<bool> StartService(string serviceName, string environmentName);
}