using System.IO;

namespace Installer.Domain;

public sealed class FileNameAndTextContent
{
    private readonly string _fileName;
    private readonly string _textContent;

    public FileNameAndTextContent(string fileName, string textContent)
    {
        _fileName = fileName;
        _textContent = textContent;
    }

    private string AppSettingsFileFullPath(string fullPath)
    {
        return Path.Combine(fullPath, _fileName);
    }

    public void WriteAllTextToPath(string fullPath)
    {
        File.WriteAllText(AppSettingsFileFullPath(fullPath), _textContent);
    }
}