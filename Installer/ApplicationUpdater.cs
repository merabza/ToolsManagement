using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using FileManagersMain;
using Installer.Domain;
using Installer.Errors;
using Installer.ServiceInstaller;
using LibFileParameters.Models;
using Microsoft.Extensions.Logging;
using OneOf;
using SystemToolsShared;
using SystemToolsShared.Errors;

namespace Installer;

public sealed class ApplicationUpdater : ApplicationUpdaterBase
{
    private readonly ApplicationUpdaterParameters _applicationUpdaterParameters;
    private readonly InstallerBase _installer;
    private readonly ILogger _logger;

    private ApplicationUpdater(ILogger logger, ApplicationUpdaterParameters applicationUpdaterParameters,
        InstallerBase serviceInstaller, bool useConsole, IMessagesDataManager? messagesDataManager, string? userName) :
        base(logger, useConsole, messagesDataManager, userName)
    {
        _logger = logger;
        _applicationUpdaterParameters = applicationUpdaterParameters;
        _installer = serviceInstaller;
    }

    public static async Task<OneOf<ApplicationUpdater, Err[]>> Create(ILogger logger, bool useConsole,
        string programArchiveDateMask, string programArchiveExtension, string parametersFileDateMask,
        string parametersFileExtension, FileStorageData fileStorageForUpload, string? installerWorkFolder,
        string? filesUserName, string? filesUsersGroupName, string? serviceUserName, string? downloadTempExtension,
        string? installFolder, string? dotnetRunner, IMessagesDataManager? messagesDataManager, string? userName,
        CancellationToken cancellationToken)
    {
        var serviceInstaller = await InstallerFabric.CreateInstaller(logger, useConsole, dotnetRunner,
            messagesDataManager, userName, cancellationToken);

        if (serviceInstaller == null)
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName, "Installer was Not Created", cancellationToken);
            logger.LogError("Installer was Not Created");
            return new[] { ApplicationUpdaterErrors.InstallerWasNotCreated };
        }

        if (string.IsNullOrWhiteSpace(installerWorkFolder))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName, "installerWorkFolder is empty", cancellationToken);
            logger.LogError("installerWorkFolder is empty");
            return new[] { ApplicationUpdaterErrors.InstallerWorkFolderIsEmpty };
        }

        if (string.IsNullOrWhiteSpace(filesUserName))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName, "filesUserName is empty", cancellationToken);
            logger.LogError("filesUserName is empty");
            return new[] { ApplicationUpdaterErrors.FilesUserNameIsEmpty };
        }

        if (string.IsNullOrWhiteSpace(filesUsersGroupName))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName, "filesUsersGroupName is empty", cancellationToken);
            logger.LogError("filesUsersGroupName is empty");
            return new[] { ApplicationUpdaterErrors.FilesUsersGroupNameIsEmpty };
        }

        if (string.IsNullOrWhiteSpace(serviceUserName))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName, "serviceUserName is empty", cancellationToken);
            logger.LogError("serviceUserName is empty");
            return new[] { ApplicationUpdaterErrors.ServiceUserNameIsEmpty };
        }

        if (string.IsNullOrWhiteSpace(downloadTempExtension))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName, "downloadTempExtension is empty", cancellationToken);
            logger.LogError("downloadTempExtension is empty");
            return new[] { ApplicationUpdaterErrors.DownloadTempExtensionIsEmpty };
        }

        if (string.IsNullOrWhiteSpace(installFolder))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName, "installFolder is empty", cancellationToken);
            logger.LogError("installFolder is empty");
            return new[] { ApplicationUpdaterErrors.InstallFolderIsEmpty };
        }

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && string.IsNullOrWhiteSpace(dotnetRunner))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName,
                    "dotnetRunner is empty. This parameter required for this OS", cancellationToken);
            logger.LogError("dotnetRunner is empty. This parameter required for this OS");
            return new[] { ApplicationUpdaterErrors.DotnetRunnerIsEmpty };
        }

        var applicationUpdaterParameters = new ApplicationUpdaterParameters(
            programArchiveExtension, fileStorageForUpload, parametersFileDateMask, parametersFileExtension,
            filesUserName, filesUsersGroupName, programArchiveDateMask, serviceUserName, downloadTempExtension,
            installerWorkFolder, installFolder);

        return new ApplicationUpdater(logger, applicationUpdaterParameters, serviceInstaller, useConsole,
            messagesDataManager, userName);
    }

    public async Task<OneOf<string, Err[]>> UpdateProgram(string projectName, string environmentName,
        CancellationToken cancellationToken)
    {
        await LogInfoAndSendMessage(
            "starting UpdateProgramWithParameters with parameters: projectName={0}, environmentName={1}", projectName,
            environmentName, cancellationToken);

        if (projectName == ProgramAttributes.Instance.AppName)
            return await LogErrorAndSendMessageFromError(InstallerErrors.CannotUpdateSelf, cancellationToken);

        var exchangeFileManager = FileManagersFabric.CreateFileManager(UseConsole, _logger,
            _applicationUpdaterParameters.InstallerWorkFolder,
            _applicationUpdaterParameters.ProgramExchangeFileStorage);

        if (exchangeFileManager is null)
            return await LogErrorAndSendMessageFromError(InstallerErrors.ExchangeFileManagerIsNull, cancellationToken);

        var runTime = _installer.Runtime;
        var programArchiveExtension = _applicationUpdaterParameters.ProgramArchiveExtension;

        await LogInfoAndSendMessage(
            "GetFileParameters with parameters: projectName={0}, environmentName={1}, _serviceInstaller.Runtime={2}, _applicationUpdaterParameters.ProgramArchiveExtension={3}",
            projectName, environmentName, runTime, programArchiveExtension, cancellationToken);

        var (prefix, dateMask, suffix) = GetFileParameters(projectName, environmentName, _installer.Runtime,
            _applicationUpdaterParameters.ProgramArchiveExtension);

        await LogInfoAndSendMessage("GetFileParameters results is: prefix={0}, dateMask={1}, suffix={2}", prefix,
            dateMask, suffix, cancellationToken);

        //დავადგინოთ გაცვლით სერვერზე
        //{_projectName} სახელით არსებული საინსტალაციო არქივები თუ არსებობს
        //და ავარჩიოთ ყველაზე ახალი
        var lastFileInfo = exchangeFileManager.GetLastFileInfo(prefix, dateMask, suffix);
        if (lastFileInfo == null)
            return await LogErrorAndSendMessageFromError(InstallerErrors.ProjectArchiveFilesNotFoundOnExchangeStorage,
                cancellationToken);

        var localArchiveFileName =
            Path.Combine(_applicationUpdaterParameters.InstallerWorkFolder, lastFileInfo.FileName);
        //თუ ფაილი უკვე მოქაჩულია, მეორედ მისი მოქაჩვა საჭირო არ არის
        if (!File.Exists(localArchiveFileName) && !exchangeFileManager.DownloadFile(lastFileInfo.FileName,
                _applicationUpdaterParameters.DownloadTempExtension)) //მოვქაჩოთ არჩეული საინსტალაციო არქივი
            return await LogErrorAndSendMessageFromError(InstallerErrors.ProjectArchiveFileWasNotDownloaded,
                cancellationToken);

        var assemblyVersionResult = await _installer.RunUpdateApplication(lastFileInfo.FileName, projectName,
            environmentName,
            _applicationUpdaterParameters.FilesUserName, _applicationUpdaterParameters.FilesUsersGroupName,
            _applicationUpdaterParameters.InstallerWorkFolder, _applicationUpdaterParameters.InstallFolder,
            cancellationToken);

        if (assemblyVersionResult.IsT1)
            return assemblyVersionResult.AsT1;

        var assemblyVersion = assemblyVersionResult.AsT0;
        if (assemblyVersion != null)
            return assemblyVersion;

        return await LogErrorAndSendMessageFromError(InstallerErrors.CannotUpdateProject(projectName, environmentName),
            cancellationToken);
    }

    public async Task<OneOf<string, Err[]>> UpdateServiceWithParameters(string projectName, string environmentName,
        string serviceUserName, string? appSettingsFileName, string? serviceDescriptionSignature,
        string? projectDescription, CancellationToken cancellationToken)
    {
        await LogInfoAndSendMessage(
            "starting UpdateProgramWithParameters with parameters: projectName={0}, environmentName={1}, serviceUserName={2}",
            projectName, environmentName, serviceUserName, cancellationToken);

        if (projectName == ProgramAttributes.Instance.AppName)
            return await LogErrorAndSendMessageFromError(InstallerErrors.CannotUpdateSelf, cancellationToken);

        var exchangeFileManager = FileManagersFabric.CreateFileManager(UseConsole, _logger,
            _applicationUpdaterParameters.InstallerWorkFolder,
            _applicationUpdaterParameters.ProgramExchangeFileStorage);

        if (exchangeFileManager is null)
            return await LogErrorAndSendMessageFromError(InstallerErrors.ExchangeFileManagerIsNull, cancellationToken);

        var runtime = _installer.Runtime;
        var programArchiveExtension = _applicationUpdaterParameters.ProgramArchiveExtension;

        await LogInfoAndSendMessage(
            "GetFileParameters with parameters: projectName={0}, environmentName={1}, _serviceInstaller.Runtime={2}, _applicationUpdaterParameters.ProgramArchiveExtension={3}",
            projectName, environmentName, runtime, programArchiveExtension, cancellationToken);

        var (prefix, dateMask, suffix) = GetFileParameters(projectName, environmentName, _installer.Runtime,
            _applicationUpdaterParameters.ProgramArchiveExtension);

        await LogInfoAndSendMessage("GetFileParameters results is: prefix={0}, dateMask={1}, suffix={2}", prefix,
            dateMask, suffix, cancellationToken);

        //დავადგინოთ გაცვლით სერვერზე
        //{_projectName} სახელით არსებული საინსტალაციო არქივები თუ არსებობს
        //და ავარჩიოთ ყველაზე ახალი
        var lastFileInfo = exchangeFileManager.GetLastFileInfo(prefix, dateMask, suffix);
        if (lastFileInfo == null)
            return await LogErrorAndSendMessageFromError(InstallerErrors.ProjectArchiveFilesNotFoundOnExchangeStorage,
                cancellationToken);

        var localArchiveFileName =
            Path.Combine(_applicationUpdaterParameters.InstallerWorkFolder, lastFileInfo.FileName);
        //თუ ფაილი უკვე მოქაჩულია, მეორედ მისი მოქაჩვა საჭირო არ არის
        if (!File.Exists(localArchiveFileName) && !exchangeFileManager.DownloadFile(lastFileInfo.FileName,
                _applicationUpdaterParameters.DownloadTempExtension)) //მოვქაჩოთ არჩეული საინსტალაციო არქივი
            return await LogErrorAndSendMessageFromError(InstallerErrors.ProjectArchiveFileWasNotDownloaded,
                cancellationToken);

        FileNameAndTextContent? appSettingsFile = null;

        if (!string.IsNullOrWhiteSpace(appSettingsFileName))
        {
            //მოვქაჩოთ ბოლო პარამეტრების ფაილი
            var appSettingsFileBody = await GetParametersFileBody(projectName, environmentName,
                _applicationUpdaterParameters.ProgramExchangeFileStorage,
                _applicationUpdaterParameters.ParametersFileDateMask,
                _applicationUpdaterParameters.ParametersFileExtension, cancellationToken);
            if (appSettingsFileBody is null)
                return await LogErrorAndSendMessageFromError(
                    InstallerErrors.CannotUpdateProject(projectName, environmentName), cancellationToken);

            appSettingsFile = new FileNameAndTextContent(appSettingsFileName, appSettingsFileBody);
        }


        var resolvedServiceUserName = ResolveServiceUserName(serviceUserName);

        var runUpdateServiceResult = await _installer.RunUpdateService(lastFileInfo.FileName, projectName,
            environmentName, appSettingsFile, resolvedServiceUserName, _applicationUpdaterParameters.FilesUserName,
            _applicationUpdaterParameters.FilesUsersGroupName, _applicationUpdaterParameters.InstallerWorkFolder,
            _applicationUpdaterParameters.InstallFolder, serviceDescriptionSignature, projectDescription,
            cancellationToken);

        if (runUpdateServiceResult.IsT1)
            return runUpdateServiceResult.AsT1;

        var assemblyVersion = runUpdateServiceResult.AsT0;
        if (assemblyVersion != null)
            return assemblyVersion;

        return await LogErrorAndSendMessageFromError(InstallerErrors.CannotUpdateProject(projectName, environmentName),
            cancellationToken);
    }

    private (string, string, string) GetFileParameters(string projectName, string environmentName, string runtime,
        string fleExtension)
    {
        var hostName = Environment.MachineName; //.Capitalize();
        //string prefix = $"{hostName}-{projectName}-{(runtime == null ? "-" : $"{runtime}-")}";
        var prefix = $"{hostName}-{environmentName}-{projectName}-{runtime}-";
        var dateMask = _applicationUpdaterParameters.ProgramArchiveDateMask;
        var suffix = fleExtension;
        return (prefix, dateMask, suffix);
    }

    private string ResolveServiceUserName(string? serviceUserName)
    {
        return string.IsNullOrWhiteSpace(serviceUserName)
            ? _applicationUpdaterParameters.ServiceUserName
            : serviceUserName;
    }
}