using DbTools;

namespace DatabasesManagement.Requests;

public sealed class CreateBackupRequest
{
    public string? BackupNamePrefix { get; set; }
    public string? DateMask { get; set; }
    public string? BackupFileExtension { get; set; }
    public string? BackupNameMiddlePart { get; set; }

    public bool Compress { get; set; }
    public bool Verify { get; set; }
    public EBackupType BackupType { get; set; }

    public string? DbServerSideBackupPath { get; set; }
}