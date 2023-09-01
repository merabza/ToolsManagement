using System;
using System.IO;
using System.Runtime.InteropServices;
using FileManagersMain;
using Installer.Domain;
using Installer.ServiceInstaller;
using LibFileParameters.Models;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace Installer;

public sealed class ApplicationUpdater : ApplicationUpdaterBase
{
    private readonly ApplicationUpdaterParameters _applicationUpdaterParameters;

    private readonly InstallerBase _installer;

    private ApplicationUpdater(ILogger logger, ApplicationUpdaterParameters applicationUpdaterParameters,
        InstallerBase serviceInstaller, bool useConsole, IMessagesDataManager? messagesDataManager, string? userName) :
        base(logger, useConsole, messagesDataManager, userName)
    {
        _applicationUpdaterParameters = applicationUpdaterParameters;
        _installer = serviceInstaller;
    }

    public static ApplicationUpdater? Create(ILogger logger, bool useConsole, string programArchiveDateMask,
        string programArchiveExtension, string parametersFileDateMask, string parametersFileExtension,
        FileStorageData fileStorageForUpload, string? installerWorkFolder, string? filesUserName,
        string? filesUsersGroupName, string? serviceUserName, string? downloadTempExtension, string? installFolder,
        string? dotnetRunner, IMessagesDataManager? messagesDataManager, string? userName)
    {
        var serviceInstaller =
            InstallerFabric.CreateInstaller(logger, useConsole, dotnetRunner, messagesDataManager, userName);

        if (serviceInstaller == null)
        {
            messagesDataManager?.SendMessage(userName, "Installer does Not Created").Wait();
            logger.LogError("Installer does Not Created");
            return null;
        }

        if (string.IsNullOrWhiteSpace(installerWorkFolder))
        {
            messagesDataManager?.SendMessage(userName, "installerWorkFolder is empty").Wait();
            logger.LogError("installerWorkFolder is empty");
            return null;
        }

        if (string.IsNullOrWhiteSpace(filesUserName))
        {
            messagesDataManager?.SendMessage(userName, "filesUserName is empty").Wait();
            logger.LogError("filesUserName is empty");
            return null;
        }

        if (string.IsNullOrWhiteSpace(filesUsersGroupName))
        {
            messagesDataManager?.SendMessage(userName, "filesUsersGroupName is empty").Wait();
            logger.LogError("filesUsersGroupName is empty");
            return null;
        }

        if (string.IsNullOrWhiteSpace(serviceUserName))
        {
            messagesDataManager?.SendMessage(userName, "serviceUserName is empty").Wait();
            logger.LogError("serviceUserName is empty");
            return null;
        }

        if (string.IsNullOrWhiteSpace(downloadTempExtension))
        {
            messagesDataManager?.SendMessage(userName, "downloadTempExtension is empty").Wait();
            logger.LogError("downloadTempExtension is empty");
            return null;
        }

        if (string.IsNullOrWhiteSpace(installFolder))
        {
            messagesDataManager?.SendMessage(userName, "downloadTempExtension is empty").Wait();
            logger.LogError("downloadTempExtension is empty");
            return null;
        }

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && string.IsNullOrWhiteSpace(dotnetRunner))
        {
            messagesDataManager?.SendMessage(userName, "dotnetRunner is empty. This parameter required for this OS")
                .Wait();
            logger.LogError("dotnetRunner is empty. This parameter required for this OS");
            return null;
        }

        var applicationUpdaterParameters = new ApplicationUpdaterParameters(
            programArchiveExtension, fileStorageForUpload, parametersFileDateMask, parametersFileExtension,
            filesUserName, filesUsersGroupName, programArchiveDateMask, serviceUserName, downloadTempExtension,
            installerWorkFolder, installFolder);

        return new ApplicationUpdater(logger, applicationUpdaterParameters, serviceInstaller, useConsole,
            messagesDataManager, userName);
    }

    public string? UpdateProgram(string projectName, string environmentName)
    {
        MessagesDataManager?.SendMessage(UserName,
                $"starting UpdateProgramWithParameters with parameters: projectName={projectName}, environmentName={environmentName}")
            .Wait();
        Logger.LogInformation(
            "starting UpdateProgramWithParameters with parameters: projectName={projectName}, environmentName={environmentName}",
            projectName, environmentName);


        if (projectName == ProgramAttributes.Instance.GetAttribute<string>("AppName"))
        {
            MessagesDataManager?.SendMessage(UserName, "Cannot update self").Wait();
            Logger.LogError("Cannot update self");
            return null;
        }

        var exchangeFileManager = FileManagersFabric.CreateFileManager(UseConsole, Logger,
            _applicationUpdaterParameters.InstallerWorkFolder,
            _applicationUpdaterParameters.ProgramExchangeFileStorage);

        if (exchangeFileManager is null)
        {
            MessagesDataManager?.SendMessage(UserName, "exchangeFileManager is null when UpdateProgramWithParameters")
                .Wait();
            Logger.LogError("exchangeFileManager is null when UpdateProgramWithParameters");
            return null;
        }

        var runTime = _installer.Runtime;
        var programArchiveExtension = _applicationUpdaterParameters.ProgramArchiveExtension;
        MessagesDataManager?.SendMessage(UserName,
                $"GetFileParameters with parameters: projectName={projectName}, environmentName={environmentName}, _serviceInstaller.Runtime={runTime}, _applicationUpdaterParameters.ProgramArchiveExtension={programArchiveExtension}")
            .Wait();
        Logger.LogInformation(
            "GetFileParameters with parameters: projectName={projectName}, environmentName={environmentName}, _serviceInstaller.Runtime={runTime}, _applicationUpdaterParameters.ProgramArchiveExtension={programArchiveExtension}",
            projectName, environmentName, runTime, programArchiveExtension);

        var (prefix, dateMask, suffix) = GetFileParameters(projectName, environmentName, _installer.Runtime,
            _applicationUpdaterParameters.ProgramArchiveExtension);

        MessagesDataManager?.SendMessage(UserName,
            $"GetFileParameters results is: prefix={prefix}, dateMask={dateMask}, suffix={suffix}").Wait();
        Logger.LogInformation("GetFileParameters results is: prefix={prefix}, dateMask={dateMask}, suffix={suffix}",
            prefix, dateMask, suffix);

        //დავადგინოთ გაცვლით სერვერზე
        //{_projectName} სახელით არსებული საინსტალაციო არქივები თუ არსებობს
        //და ავარჩიოთ ყველაზე ახალი
        var lastFileInfo = exchangeFileManager.GetLastFileInfo(prefix, dateMask, suffix);
        if (lastFileInfo == null)
        {
            MessagesDataManager?.SendMessage(UserName, "Project archive files not found on exchange storage").Wait();
            Logger.LogError("Project archive files not found on exchange storage");
            return null;
        }

        var localArchiveFileName =
            Path.Combine(_applicationUpdaterParameters.InstallerWorkFolder, lastFileInfo.FileName);
        //თუ ფაილი უკვე მოქაჩულია, მეორედ მისი მოქაჩვა საჭირო არ არის
        if (!File.Exists(localArchiveFileName))
            //მოვქაჩოთ არჩეული საინსტალაციო არქივი
            if (!exchangeFileManager.DownloadFile(lastFileInfo.FileName,
                    _applicationUpdaterParameters.DownloadTempExtension))
            {
                MessagesDataManager?.SendMessage(UserName, "Project archive file not downloaded").Wait();
                Logger.LogError("Project archive file not downloaded");
                return null;
            }

        var assemblyVersion = _installer.RunUpdateApplication(lastFileInfo.FileName, projectName, environmentName,
            _applicationUpdaterParameters.FilesUserName, _applicationUpdaterParameters.FilesUsersGroupName,
            _applicationUpdaterParameters.InstallerWorkFolder, _applicationUpdaterParameters.InstallFolder);

        if (assemblyVersion != null)
            return assemblyVersion;

        MessagesDataManager?.SendMessage(UserName, $"Cannot Update {projectName}/{environmentName}").Wait();
        Logger.LogError("Cannot Update {projectName}/{environmentName}", projectName, environmentName);
        return null;
    }

    public string? UpdateServiceWithParameters(string projectName, string environmentName, string serviceUserName,
        string? serviceName, string? appSettingsFileName)
    {
        MessagesDataManager?.SendMessage(UserName,
                $"starting UpdateProgramWithParameters with parameters: projectName={projectName}, environmentName={environmentName}, serviceUserName={serviceUserName}, serviceName={serviceName}")
            .Wait();
        Logger.LogInformation(
            "starting UpdateProgramWithParameters with parameters: projectName={projectName}, environmentName={environmentName}, serviceUserName={serviceUserName}, serviceName={serviceName}",
            projectName, environmentName, serviceUserName, serviceName);


        if (projectName == ProgramAttributes.Instance.GetAttribute<string>("AppName"))
        {
            MessagesDataManager?.SendMessage(UserName, "Cannot update self").Wait();
            Logger.LogError("Cannot update self");
            return null;
        }

        var exchangeFileManager = FileManagersFabric.CreateFileManager(UseConsole, Logger,
            _applicationUpdaterParameters.InstallerWorkFolder,
            _applicationUpdaterParameters.ProgramExchangeFileStorage);

        if (exchangeFileManager is null)
        {
            MessagesDataManager?.SendMessage(UserName, "exchangeFileManager is null when UpdateProgramWithParameters")
                .Wait();
            Logger.LogError("exchangeFileManager is null when UpdateProgramWithParameters");
            return null;
        }

        var runtime = _installer.Runtime;
        var programArchiveExtension = _applicationUpdaterParameters.ProgramArchiveExtension;

        MessagesDataManager?.SendMessage(UserName,
                $"GetFileParameters with parameters: projectName={projectName}, environmentName={environmentName}, _serviceInstaller.Runtime={runtime}, _applicationUpdaterParameters.ProgramArchiveExtension={programArchiveExtension}")
            .Wait();
        Logger.LogInformation(
            "GetFileParameters with parameters: projectName={projectName}, environmentName={environmentName}, _serviceInstaller.Runtime={runtime}, _applicationUpdaterParameters.ProgramArchiveExtension={programArchiveExtension}",
            projectName, environmentName, runtime, programArchiveExtension);

        var (prefix, dateMask, suffix) = GetFileParameters(projectName, environmentName, _installer.Runtime,
            _applicationUpdaterParameters.ProgramArchiveExtension);

        MessagesDataManager?.SendMessage(UserName,
            $"GetFileParameters results is: prefix={prefix}, dateMask={dateMask}, suffix={suffix}").Wait();
        Logger.LogInformation("GetFileParameters results is: prefix={prefix}, dateMask={dateMask}, suffix={suffix}",
            prefix, dateMask, suffix);


        //დავადგინოთ გაცვლით სერვერზე
        //{_projectName} სახელით არსებული საინსტალაციო არქივები თუ არსებობს
        //და ავარჩიოთ ყველაზე ახალი
        var lastFileInfo = exchangeFileManager.GetLastFileInfo(prefix, dateMask, suffix);
        if (lastFileInfo == null)
        {
            MessagesDataManager?.SendMessage(UserName, "Project archive files not found on exchange storage").Wait();
            Logger.LogError("Project archive files not found on exchange storage");
            return null;
        }

        var localArchiveFileName =
            Path.Combine(_applicationUpdaterParameters.InstallerWorkFolder, lastFileInfo.FileName);
        //თუ ფაილი უკვე მოქაჩულია, მეორედ მისი მოქაჩვა საჭირო არ არის
        if (!File.Exists(localArchiveFileName))
            //მოვქაჩოთ არჩეული საინსტალაციო არქივი
            if (!exchangeFileManager.DownloadFile(lastFileInfo.FileName,
                    _applicationUpdaterParameters.DownloadTempExtension))
            {
                MessagesDataManager?.SendMessage(UserName, "Project archive file not downloaded").Wait();
                Logger.LogError("Project archive file not downloaded");
                return null;
            }

        FileNameAndTextContent? appSettingsFile = null;
        //string? appSettingsFileBody = null;
        if (!string.IsNullOrWhiteSpace(appSettingsFileName))
        {
            //მოვქაჩოთ ბოლო პარამეტრების ფაილი
            var appSettingsFileBody = GetParametersFileBody(projectName, environmentName,
                _applicationUpdaterParameters.ProgramExchangeFileStorage,
                _applicationUpdaterParameters.ParametersFileDateMask,
                _applicationUpdaterParameters.ParametersFileExtension);
            if (appSettingsFileBody is null)
            {
                MessagesDataManager?.SendMessage(UserName,
                    $"Cannot Update {projectName}, because cannot get latest parameters file").Wait();
                Logger.LogError("Cannot Update {projectName}, because cannot get latest parameters file", projectName);
                if (string.IsNullOrWhiteSpace(serviceName))
                    return null;
            }

            if (appSettingsFileBody is not null)
                appSettingsFile =
                    new FileNameAndTextContent(appSettingsFileName, appSettingsFileBody);
        }


        var resolvedServiceUserName = ResolveServiceUserName(serviceUserName);

        var assemblyVersion = _installer.RunUpdateService(lastFileInfo.FileName, projectName, serviceName,
            environmentName, appSettingsFile, resolvedServiceUserName, _applicationUpdaterParameters.FilesUserName,
            _applicationUpdaterParameters.FilesUsersGroupName, _applicationUpdaterParameters.InstallerWorkFolder,
            _applicationUpdaterParameters.InstallFolder);

        if (assemblyVersion != null)
            return assemblyVersion;

        MessagesDataManager?.SendMessage(UserName, $"Cannot Update {projectName}/{environmentName}").Wait();
        Logger.LogError("Cannot Update {projectName}/{environmentName}", projectName, environmentName);
        return null;
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