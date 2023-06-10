namespace Installer.Models;

public sealed class ProjectUpdateDataModel
{
    public string? ApiKey { get; set; }
    public string? ProjectName { get; set; }
    public string? ServiceName { get; set; }
    public string? ServiceUserName { get; set; }
    public string? AppSettingsFileName { get; set; }
}