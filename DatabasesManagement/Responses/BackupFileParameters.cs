namespace DatabasesManagement.Responses;

public sealed class BackupFileParameters
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public BackupFileParameters(string name, string prefix, string suffix, string dateMask)
    {
        Name = name;
        Prefix = prefix;
        Suffix = suffix;
        DateMask = dateMask;
    }

    public string Prefix { get; set; }
    public string Suffix { get; set; }
    public string Name { get; set; }
    public string DateMask { get; set; }
}