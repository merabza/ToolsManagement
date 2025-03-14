﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LanguageExt;
using SystemToolsShared.Errors;

namespace Installer.ProjectManagers;

public interface IProjectsManager
{
    //არასერვისი პროგრამებისათვის მოშორებული წაშლა არ მოხდება, რადგან ასეთი პროგრამებისათვის სერვერზე დაინსტალირება გათვალისწინებული არ გვაქვს
    //თუ მომავალში გადავაკეთებთ, ისე, რომ არასერვისული პროგრამებისათვის სერვერის მითითება შესაძლებელი იქნება და მოშორებულ სერვერზე ასეთი პროგრამის დაყენება შესაძლებელი იქნება, მაშინ RemoveProject უნდა აღდგეს
    //Task<bool> RemoveProject(string projectName);
    ValueTask<Option<IEnumerable<Err>>> RemoveProjectAndService(string projectName, string environmentName,
        bool isService, CancellationToken cancellationToken = default);

    ValueTask<Option<IEnumerable<Err>>> StopService(string projectName, string environmentName,
        CancellationToken cancellationToken = default);

    ValueTask<Option<IEnumerable<Err>>> StartService(string projectName, string environmentName,
        CancellationToken cancellationToken = default);
}