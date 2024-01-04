using System;
using System.Threading;
using System.Threading.Tasks;
using FileManagersMain;
using LibFileParameters.Models;
using LibToolActions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SystemToolsShared;

// ReSharper disable ConvertToPrimaryConstructor

namespace Installer.Actions;

public sealed class GetLatestParametersFileBodyAction : ToolAction
{
    private readonly string _dateMask;
    private readonly string _environmentName;
    private readonly FileStorageData _fileStorageForDownload;

    private readonly string _parametersFileExtension;

    private readonly string _projectName;
    private readonly string _serverName;
    private readonly bool _useConsole;

    public GetLatestParametersFileBodyAction(ILogger logger, bool useConsole, FileStorageData fileStorageForDownload,
        string projectName, string serverName, string environmentName, string dateMask, string parametersFileExtension,
        IMessagesDataManager? messagesDataManager, string? userName) : base(logger, "Get latest Parameters File Body",
        messagesDataManager, userName)
    {
        _useConsole = useConsole;
        _fileStorageForDownload = fileStorageForDownload;
        _projectName = projectName;
        _serverName = serverName;
        _environmentName = environmentName;
        _dateMask = dateMask;
        _parametersFileExtension = parametersFileExtension;
    }

    public string? LatestParametersFileContent { get; private set; }

    public string? AppSettingsVersion { get; private set; }


    protected override bool CheckValidate()
    {
        return true;
    }

    protected override async Task<bool> RunAction(CancellationToken cancellationToken)
    {
        LatestParametersFileContent = await GetParametersFileBody(cancellationToken);
        if (string.IsNullOrWhiteSpace(LatestParametersFileContent))
            return true;
        var appSetJObject = JObject.Parse(LatestParametersFileContent);
        AppSettingsVersion = appSetJObject["VersionInfo"]?["AppSettingsVersion"]?.Value<string>();
        return true;
    }


    private async Task<string?> GetParametersFileBody(CancellationToken cancellationToken)
    {
        var prefix = GetPrefix(_projectName, _serverName, _environmentName, null);

        var exchangeFileManager =
            FileManagersFabric.CreateFileManager(_useConsole, Logger, null, _fileStorageForDownload, true);

        //დავადგინოთ გაცვლით სერვერზე შესაბამისი პარამეტრების ფაილები თუ არსებობს
        //და ავარჩიოთ ყველაზე ახალი
        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName,
                $"Check files on exchange storage for Prefix {prefix}, Date Mask {_dateMask} and extension {_parametersFileExtension}",
                cancellationToken);
        Console.WriteLine(
            $"Check files on exchange storage for Prefix {prefix}, Date Mask {_dateMask} and extension {_parametersFileExtension}");
        var lastParametersFileInfo =
            exchangeFileManager?.GetLastFileInfo(prefix, _dateMask, _parametersFileExtension);
        if (lastParametersFileInfo != null)
            //მოვქაჩოთ არჩეული პარამეტრების ფაილის შიგთავსი
            return exchangeFileManager?.GetTextFileContent(lastParametersFileInfo.FileName);
        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName, "Project Parameter files not found on exchange storage",
                cancellationToken);
        Logger.LogWarning("Project Parameter files not found on exchange storage");
        return null;
    }


    private static string GetPrefix(string projectName, string serverName, string environmentName, string? runtime)
    {
        var prefix = $"{serverName}-{environmentName}-{projectName}-{(runtime == null ? "" : $"{runtime}-")}";
        return prefix;
    }
}