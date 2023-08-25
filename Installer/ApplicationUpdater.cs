using System;
using System.IO;
using System.Runtime.InteropServices;
using FileManagersMain;
using Installer.Domain;
using Installer.ServiceInstaller;
using LibFileParameters.Models;
using LibWebAgentMessages;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace Installer;

public sealed class ApplicationUpdater : ApplicationUpdaterBase
{
    private readonly ApplicationUpdaterParameters _applicationUpdaterParameters;

    private readonly InstallerBase _installer;
    //private readonly IMessagesDataManager _messagesDataManager;
    //private readonly string? _userName;

    private ApplicationUpdater(ILogger logger, ApplicationUpdaterParameters applicationUpdaterParameters,
        InstallerBase serviceInstaller, bool useConsole) : base(logger, useConsole)
    {
        _applicationUpdaterParameters = applicationUpdaterParameters;
        _installer = serviceInstaller;
        //_messagesDataManager = messagesDataManager;
        //_userName = userName;
    }

    public static ApplicationUpdater? Create(ILogger logger, bool useConsole, string programArchiveDateMask,
        string programArchiveExtension, string parametersFileDateMask, string parametersFileExtension,
        FileStorageData fileStorageForUpload, string? installerWorkFolder, string? filesUserName,
        string? filesUsersGroupName, string? serviceUserName, string? downloadTempExtension, string? installFolder,
        string? dotnetRunner, IMessagesDataManager messagesDataManager, string? userName)
    {
        var serviceInstaller =
            InstallerFabric.CreateInstaller(logger, useConsole, dotnetRunner, messagesDataManager, userName);

        if (serviceInstaller == null)
        {
            logger.LogError("Installer does Not Created");
            return null;
        }

        if (string.IsNullOrWhiteSpace(installerWorkFolder))
        {
            logger.LogError("installerWorkFolder is empty");
            return null;
        }

        if (string.IsNullOrWhiteSpace(filesUserName))
        {
            logger.LogError("filesUserName is empty");
            return null;
        }

        if (string.IsNullOrWhiteSpace(filesUsersGroupName))
        {
            logger.LogError("filesUsersGroupName is empty");
            return null;
        }

        if (string.IsNullOrWhiteSpace(serviceUserName))
        {
            logger.LogError("serviceUserName is empty");
            return null;
        }

        if (string.IsNullOrWhiteSpace(downloadTempExtension))
        {
            logger.LogError("downloadTempExtension is empty");
            return null;
        }

        if (string.IsNullOrWhiteSpace(installFolder))
        {
            logger.LogError("downloadTempExtension is empty");
            return null;
        }

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && string.IsNullOrWhiteSpace(dotnetRunner))
        {
            logger.LogError("dotnetRunner is empty. This parameter required for this OS");
            return null;
        }

        var applicationUpdaterParameters = new ApplicationUpdaterParameters(
            programArchiveExtension, fileStorageForUpload, parametersFileDateMask, parametersFileExtension,
            filesUserName, filesUsersGroupName, programArchiveDateMask, serviceUserName, downloadTempExtension,
            installerWorkFolder, installFolder);

        return new ApplicationUpdater(logger, applicationUpdaterParameters, serviceInstaller, useConsole);
    }

    public string? UpdateProgram(string projectName, string environmentName)
    {
        Logger.LogInformation(
            "starting UpdateProgramWithParameters with parameters: projectName={projectName}, environmentName={environmentName}",
            projectName, environmentName);


        if (projectName == ProgramAttributes.Instance.GetAttribute<string>("AppName"))
        {
            Logger.LogError("Cannot update self");
            return null;
        }

        var exchangeFileManager = FileManagersFabric.CreateFileManager(UseConsole, Logger,
            _applicationUpdaterParameters.InstallerWorkFolder,
            _applicationUpdaterParameters.ProgramExchangeFileStorage);

        if (exchangeFileManager is null)
        {
            Logger.LogError("exchangeFileManager is null when UpdateProgramWithParameters");
            return null;
        }

        var runTime = _installer.Runtime;
        var programArchiveExtension = _applicationUpdaterParameters.ProgramArchiveExtension;
        Logger.LogInformation(
            "GetFileParameters with parameters: projectName={projectName}, environmentName={environmentName}, _serviceInstaller.Runtime={runTime}, _applicationUpdaterParameters.ProgramArchiveExtension={programArchiveExtension}",
            projectName, environmentName, runTime, programArchiveExtension);

        var (prefix, dateMask, suffix) = GetFileParameters(projectName, environmentName, _installer.Runtime,
            _applicationUpdaterParameters.ProgramArchiveExtension);

        Logger.LogInformation("GetFileParameters results is: prefix={prefix}, dateMask={dateMask}, suffix={suffix}",
            prefix, dateMask, suffix);

        //დავადგინოთ გაცვლით სერვერზე
        //{_projectName} სახელით არსებული საინსტალაციო არქივები თუ არსებობს
        //და ავარჩიოთ ყველაზე ახალი
        var lastFileInfo = exchangeFileManager.GetLastFileInfo(prefix, dateMask, suffix);
        if (lastFileInfo == null)
        {
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
                Logger.LogError("Project archive file not downloaded");
                return null;
            }

        var assemblyVersion = _installer.RunUpdateApplication(lastFileInfo.FileName, projectName,
            _applicationUpdaterParameters.FilesUserName, _applicationUpdaterParameters.FilesUsersGroupName,
            _applicationUpdaterParameters.InstallerWorkFolder, _applicationUpdaterParameters.InstallFolder);

        if (assemblyVersion != null)
            return assemblyVersion;

        Logger.LogError("Cannot Update {projectName}/{environmentName}", projectName, environmentName);
        return null;
    }

    public string? UpdateServiceWithParameters(string projectName, string environmentName, string serviceUserName,
        string? serviceName, string? appSettingsFileName)
    {
        Logger.LogInformation(
            "starting UpdateProgramWithParameters with parameters: projectName={projectName}, environmentName={environmentName}, serviceUserName={serviceUserName}, serviceName={serviceName}",
            projectName, environmentName, serviceUserName, serviceName);


        if (projectName == ProgramAttributes.Instance.GetAttribute<string>("AppName"))
        {
            Logger.LogError("Cannot update self");
            return null;
        }

        var exchangeFileManager = FileManagersFabric.CreateFileManager(UseConsole, Logger,
            _applicationUpdaterParameters.InstallerWorkFolder,
            _applicationUpdaterParameters.ProgramExchangeFileStorage);

        if (exchangeFileManager is null)
        {
            Logger.LogError("exchangeFileManager is null when UpdateProgramWithParameters");
            return null;
        }

        var runtime = _installer.Runtime;
        var programArchiveExtension = _applicationUpdaterParameters.ProgramArchiveExtension;
        Logger.LogInformation(
            "GetFileParameters with parameters: projectName={projectName}, environmentName={environmentName}, _serviceInstaller.Runtime={runtime}, _applicationUpdaterParameters.ProgramArchiveExtension={programArchiveExtension}",
            projectName, environmentName, runtime, programArchiveExtension);

        var (prefix, dateMask, suffix) = GetFileParameters(projectName, environmentName, _installer.Runtime,
            _applicationUpdaterParameters.ProgramArchiveExtension);

        Logger.LogInformation("GetFileParameters results is: prefix={prefix}, dateMask={dateMask}, suffix={suffix}",
            prefix, dateMask, suffix);


        //დავადგინოთ გაცვლით სერვერზე
        //{_projectName} სახელით არსებული საინსტალაციო არქივები თუ არსებობს
        //და ავარჩიოთ ყველაზე ახალი
        var lastFileInfo = exchangeFileManager.GetLastFileInfo(prefix, dateMask, suffix);
        if (lastFileInfo == null)
        {
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
            appSettingsFile, resolvedServiceUserName, _applicationUpdaterParameters.FilesUserName,
            _applicationUpdaterParameters.FilesUsersGroupName, _applicationUpdaterParameters.InstallerWorkFolder,
            _applicationUpdaterParameters.InstallFolder);

        if (assemblyVersion != null)
            return assemblyVersion;

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