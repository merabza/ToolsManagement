using System.Threading;
using System.Threading.Tasks;
using FileManagersMain;
using LibFileParameters.Models;
using LibToolActions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SystemToolsShared;

// ReSharper disable ConvertToPrimaryConstructor

namespace Installer.ToolActions;

public sealed class GetLatestParametersFileBodyAction : ToolAction
{
    private readonly string _dateMask;
    private readonly string _environmentName;
    private readonly FileStorageData _fileStorageForDownload;
    private readonly ILogger _logger;
    private readonly string _parametersFileExtension;
    private readonly string _projectName;
    private readonly string _serverName;
    private readonly bool _useConsole;

    public GetLatestParametersFileBodyAction(ILogger logger, bool useConsole, FileStorageData fileStorageForDownload,
        string projectName, string serverName, string environmentName, string dateMask, string parametersFileExtension,
        IMessagesDataManager? messagesDataManager, string? userName) : base(logger, "Get latest Parameters File Body",
        messagesDataManager, userName)
    {
        _logger = logger;
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

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        LatestParametersFileContent = await GetParametersFileBody(cancellationToken);
        if (string.IsNullOrWhiteSpace(LatestParametersFileContent))
            return true;
        var appSetJObject = JObject.Parse(LatestParametersFileContent);
        AppSettingsVersion = appSetJObject["VersionInfo"]?["AppSettingsVersion"]?.Value<string>();
        return true;
    }

    private async Task<string?> GetParametersFileBody(CancellationToken cancellationToken = default)
    {
        var prefix = GetPrefix(_projectName, _serverName, _environmentName, null);

        var exchangeFileManager =
            FileManagersFabric.CreateFileManager(_useConsole, _logger, null, _fileStorageForDownload, true);

        //დავადგინოთ გაცვლით სერვერზე შესაბამისი პარამეტრების ფაილები თუ არსებობს
        //და ავარჩიოთ ყველაზე ახალი
        await LogInfoAndSendMessage("Check files on exchange storage for Prefix {0}, Date Mask {1} and extension {2}",
            prefix, _dateMask, _parametersFileExtension, cancellationToken);

        var lastParametersFileInfo = exchangeFileManager?.GetLastFileInfo(prefix, _dateMask, _parametersFileExtension);
        if (lastParametersFileInfo != null)
            //მოვქაჩოთ არჩეული პარამეტრების ფაილის შიგთავსი
            return exchangeFileManager?.GetTextFileContent(lastParametersFileInfo.FileName);

        await LogInfoAndSendMessage("Project Parameter files not found on exchange storage", cancellationToken);

        return null;
    }

    private static string GetPrefix(string projectName, string serverName, string environmentName, string? runtime)
    {
        var prefix = $"{serverName}-{environmentName}-{projectName}-{(runtime == null ? string.Empty : $"{runtime}-")}";
        return prefix;
    }
}