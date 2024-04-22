namespace DatabasesManagement.Requests;

public sealed class RestoreBackupRequest
{
    public string? Prefix { get; set; }
    public string? Suffix { get; set; }
    public string? Name { get; set; }
    public string? DateMask { get; set; }
    public string? DestinationDbServerSideDataFolderPath { get; set; }
    public string? DestinationDbServerSideLogFolderPath { get; set; }
}