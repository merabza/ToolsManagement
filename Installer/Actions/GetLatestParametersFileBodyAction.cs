using System;
using FileManagersMain;
using LibFileParameters.Models;
using LibToolActions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Installer.Actions;

public sealed class GetLatestParametersFileBodyAction : ToolAction
{
    private readonly string _dateMask;
    private readonly FileStorageData _fileStorageForDownload;

    private readonly string _parametersFileExtension;

    private readonly string _projectName;
    private readonly string _serverName;
    private readonly bool _useConsole;

    public GetLatestParametersFileBodyAction(ILogger logger, bool useConsole, FileStorageData fileStorageForDownload,
        string projectName, string serverName, string dateMask, string parametersFileExtension) : base(logger,
        useConsole, "Get latest Parameters File Body", null)
    {
        _useConsole = useConsole;
        _fileStorageForDownload = fileStorageForDownload;
        _projectName = projectName;
        _serverName = serverName;
        _dateMask = dateMask;
        _parametersFileExtension = parametersFileExtension;
    }

    public string? LatestParametersFileContent { get; private set; }

    public string? AppSettingsVersion { get; private set; }


    protected override bool CheckValidate()
    {
        return true;
    }

    protected override bool RunAction()
    {
        LatestParametersFileContent = GetParametersFileBody();
        if (string.IsNullOrWhiteSpace(LatestParametersFileContent))
            return true;
        var appSetJObject = JObject.Parse(LatestParametersFileContent);
        AppSettingsVersion = appSetJObject["VersionInfo"]?["AppSettingsVersion"]?.Value<string>();
        return true;
    }


    private string? GetParametersFileBody()
    {
        var prefix = GetPrefix(_projectName, _serverName, null);

        var exchangeFileManager =
            FileManagersFabric.CreateFileManager(_useConsole, Logger, null, _fileStorageForDownload, true);

        //დავადგინოთ გაცვლით სერვერზე შესაბამისი პარამეტრების ფაილები თუ არსებობს
        //და ავარჩიოთ ყველაზე ახალი
        Console.WriteLine(
            $"Check files on exchange storage for Prefix {prefix}, Date Mask {_dateMask} and extension {_parametersFileExtension}");
        var lastParametersFileInfo =
            exchangeFileManager?.GetLastFileInfo(prefix, _dateMask, _parametersFileExtension);
        if (lastParametersFileInfo != null)
            //მოვქაჩოთ არჩეული პარამეტრების ფაილის შიგთავსი
            return exchangeFileManager?.GetTextFileContent(lastParametersFileInfo.FileName);
        Logger.LogWarning("Project Parameter files not found on exchange storage");
        return null;
    }


    private string GetPrefix(string projectName, string serverName, string? runtime)
    {
        var hostName = serverName; //.Capitalize();
        var prefix = $"{hostName}-{projectName}-{(runtime == null ? "" : $"{runtime}-")}";
        return prefix;
    }
}