using SystemTools.SharedKernel;

namespace ToolsManagement.Installer.Errors;

public static class ProjectManagersErrors
{
    public static readonly Error AppParametersFileUpdaterCreateError =
        Error.Problem(nameof(AppParametersFileUpdaterCreateError), "AppParametersFileUpdater does not created");

    public static Error ProjectServiceCanNotRemoved(string projectName, string environmentName)
    {
        return Error.Problem(nameof(ProjectServiceCanNotRemoved),
            $"Project {projectName} => service {projectName}/{environmentName} can not removed");
    }

    public static Error ServiceCanNotBeStopped(string projectName, string environmentName)
    {
        return Error.Problem(nameof(ServiceCanNotBeStopped),
            $"service {projectName}/{environmentName} can not be stopped");
    }

    public static Error ServiceCanNotBeStarted(string projectName, string environmentName)
    {
        return Error.Problem(nameof(ServiceCanNotBeStarted),
            $"service {projectName}/{environmentName} can not be started");
    }

    public static Error ProjectCanNotBeRemoved(string projectName)
    {
        return Error.Problem(nameof(ProjectCanNotBeRemoved), $"Project {projectName} can not be removed");
    }

    public static Error ApplicationUpdaterDoesNotCreated(string projectName, string environmentName)
    {
        return Error.Problem(nameof(ApplicationUpdaterDoesNotCreated),
            $"ApplicationUpdater for {projectName}/{environmentName} does not created");
    }
}
