using SystemToolsShared.Errors;

namespace Installer.Errors;

public static class ProjectManagersErrors
{
    public static readonly Err AppParametersFileUpdaterCreateError = new()
    {
        ErrorCode = nameof(AppParametersFileUpdaterCreateError),
        ErrorMessage = "AppParametersFileUpdater does not created"
    };

    public static Err ProjectServiceCanNotRemoved(string projectName, string environmentName)
    {
        return new Err
        {
            ErrorCode = nameof(ProjectServiceCanNotRemoved),
            ErrorMessage = $"Project {projectName} => service {projectName}/{environmentName} can not removed"
        };
    }

    public static Err ServiceCanNotBeStopped(string projectName, string environmentName)
    {
        return new Err
        {
            ErrorCode = nameof(ServiceCanNotBeStopped),
            ErrorMessage = $"service {projectName}/{environmentName} can not be stopped"
        };
    }

    public static Err ServiceCanNotBeStarted(string projectName, string environmentName)
    {
        return new Err
        {
            ErrorCode = nameof(ServiceCanNotBeStarted),
            ErrorMessage = $"service {projectName}/{environmentName} can not be started"
        };
    }

    public static Err ProjectCanNotBeRemoved(string projectName)
    {
        return new Err
            { ErrorCode = nameof(ProjectCanNotBeRemoved), ErrorMessage = $"Project {projectName} can not be removed" };
    }

    public static Err ApplicationUpdaterDoesNotCreated(string projectName, string environmentName)
    {
        return new Err
        {
            ErrorCode = nameof(ApplicationUpdaterDoesNotCreated),
            ErrorMessage = $"ApplicationUpdater for {projectName}/{environmentName} does not created"
        };
    }
}