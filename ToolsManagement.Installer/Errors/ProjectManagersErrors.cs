using SystemTools.SystemToolsShared.Errors;

namespace ToolsManagement.Installer.Errors;

public static class ProjectManagersErrors
{
    public static readonly ErrorOmd AppParametersFileUpdaterCreateError = new()
    {
        Code = nameof(AppParametersFileUpdaterCreateError), Name = "AppParametersFileUpdater does not created"
    };

    public static ErrorOmd ProjectServiceCanNotRemoved(string projectName, string environmentName)
    {
        return new ErrorOmd
        {
            Code = nameof(ProjectServiceCanNotRemoved),
            Name = $"Project {projectName} => service {projectName}/{environmentName} can not removed"
        };
    }

    public static ErrorOmd ServiceCanNotBeStopped(string projectName, string environmentName)
    {
        return new ErrorOmd
        {
            Code = nameof(ServiceCanNotBeStopped),
            Name = $"service {projectName}/{environmentName} can not be stopped"
        };
    }

    public static ErrorOmd ServiceCanNotBeStarted(string projectName, string environmentName)
    {
        return new ErrorOmd
        {
            Code = nameof(ServiceCanNotBeStarted),
            Name = $"service {projectName}/{environmentName} can not be started"
        };
    }

    public static ErrorOmd ProjectCanNotBeRemoved(string projectName)
    {
        return new ErrorOmd
        {
            Code = nameof(ProjectCanNotBeRemoved), Name = $"Project {projectName} can not be removed"
        };
    }

    public static ErrorOmd ApplicationUpdaterDoesNotCreated(string projectName, string environmentName)
    {
        return new ErrorOmd
        {
            Code = nameof(ApplicationUpdaterDoesNotCreated),
            Name = $"ApplicationUpdater for {projectName}/{environmentName} does not created"
        };
    }
}
