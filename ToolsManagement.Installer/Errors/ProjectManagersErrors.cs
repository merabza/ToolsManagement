using SystemTools.SystemToolsShared.Errors;

namespace ToolsManagement.Installer.Errors;

public static class ProjectManagersErrors
{
    public static readonly Error AppParametersFileUpdaterCreateError = new()
    {
        Code = nameof(AppParametersFileUpdaterCreateError), Name = "AppParametersFileUpdater does not created"
    };

    public static Error ProjectServiceCanNotRemoved(string projectName, string environmentName)
    {
        return new Error
        {
            Code = nameof(ProjectServiceCanNotRemoved),
            Name = $"Project {projectName} => service {projectName}/{environmentName} can not removed"
        };
    }

    public static Error ServiceCanNotBeStopped(string projectName, string environmentName)
    {
        return new Error
        {
            Code = nameof(ServiceCanNotBeStopped),
            Name = $"service {projectName}/{environmentName} can not be stopped"
        };
    }

    public static Error ServiceCanNotBeStarted(string projectName, string environmentName)
    {
        return new Error
        {
            Code = nameof(ServiceCanNotBeStarted),
            Name = $"service {projectName}/{environmentName} can not be started"
        };
    }

    public static Error ProjectCanNotBeRemoved(string projectName)
    {
        return new Error { Code = nameof(ProjectCanNotBeRemoved), Name = $"Project {projectName} can not be removed" };
    }

    public static Error ApplicationUpdaterDoesNotCreated(string projectName, string environmentName)
    {
        return new Error
        {
            Code = nameof(ApplicationUpdaterDoesNotCreated),
            Name = $"ApplicationUpdater for {projectName}/{environmentName} does not created"
        };
    }
}
