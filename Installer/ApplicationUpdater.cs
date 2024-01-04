using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using FileManagersMain;
using Installer.Domain;
using Installer.ServiceInstaller;
using LibFileParameters.Models;
using Microsoft.Extensions.Logging;
using OneOf;
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

    public static async Task<OneOf<ApplicationUpdater, Err[]>> Create(ILogger logger, bool useConsole,
        string programArchiveDateMask,
        string programArchiveExtension, string parametersFileDateMask, string parametersFileExtension,
        FileStorageData fileStorageForUpload, string? installerWorkFolder, string? filesUserName,
        string? filesUsersGroupName, string? serviceUserName, string? downloadTempExtension, string? installFolder,
        string? dotnetRunner, IMessagesDataManager? messagesDataManager, string? userName,
        CancellationToken cancellationToken)
    {
        var serviceInstaller = await InstallerFabric.CreateInstaller(logger, useConsole, dotnetRunner,
            messagesDataManager, userName, cancellationToken);

        if (serviceInstaller == null)
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName, "Installer was Not Created", cancellationToken);
            logger.LogError("Installer was Not Created");
            return new Err[]
            {
                new()
                {
                    ErrorCode = "InstallerWasNotCreated",
                    ErrorMessage = "Installer was Not Created"
                }
            };
        }

        if (string.IsNullOrWhiteSpace(installerWorkFolder))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName, "installerWorkFolder is empty", cancellationToken);
            logger.LogError("installerWorkFolder is empty");
            return new Err[]
            {
                new()
                {
                    ErrorCode = "installerWorkFolderIsEmpty",
                    ErrorMessage = "installerWorkFolder is empty"
                }
            };
        }

        if (string.IsNullOrWhiteSpace(filesUserName))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName, "filesUserName is empty", cancellationToken);
            logger.LogError("filesUserName is empty");
            return new Err[]
            {
                new()
                {
                    ErrorCode = "FilesUserNameIsEmpty",
                    ErrorMessage = "filesUserName is empty"
                }
            };
        }

        if (string.IsNullOrWhiteSpace(filesUsersGroupName))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName, "filesUsersGroupName is empty", cancellationToken);
            logger.LogError("filesUsersGroupName is empty");
            return new Err[]
            {
                new()
                {
                    ErrorCode = "filesUsersGroupNameIsEmpty",
                    ErrorMessage = "filesUsersGroupName is empty"
                }
            };
        }

        if (string.IsNullOrWhiteSpace(serviceUserName))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName, "serviceUserName is empty", cancellationToken);
            logger.LogError("serviceUserName is empty");
            return new Err[]
            {
                new()
                {
                    ErrorCode = "serviceUserNameIsEmpty",
                    ErrorMessage = "serviceUserName is empty"
                }
            };
        }

        if (string.IsNullOrWhiteSpace(downloadTempExtension))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName, "downloadTempExtension is empty", cancellationToken);
            logger.LogError("downloadTempExtension is empty");
            return new Err[]
            {
                new()
                {
                    ErrorCode = "downloadTempExtensionIsEmpty",
                    ErrorMessage = "downloadTempExtension is empty"
                }
            };
        }

        if (string.IsNullOrWhiteSpace(installFolder))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName, "installFolder is empty", cancellationToken);
            logger.LogError("installFolder is empty");
            return new Err[]
            {
                new()
                {
                    ErrorCode = "installFolderIsEmpty",
                    ErrorMessage = "installFolder is empty"
                }
            };
        }

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && string.IsNullOrWhiteSpace(dotnetRunner))
        {
            if (messagesDataManager is not null)
                await messagesDataManager.SendMessage(userName,
                    "dotnetRunner is empty. This parameter required for this OS", cancellationToken);
            logger.LogError("dotnetRunner is empty. This parameter required for this OS");
            return new Err[]
            {
                new()
                {
                    ErrorCode = "dotnetRunnerIsEmpty",
                    ErrorMessage = "dotnetRunner is empty"
                }
            };
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
        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName,
                $"starting UpdateProgramWithParameters with parameters: projectName={projectName}, environmentName={environmentName}",
                cancellationToken);
        Logger.LogInformation(
            "starting UpdateProgramWithParameters with parameters: projectName={projectName}, environmentName={environmentName}",
            projectName, environmentName);


        if (projectName == ProgramAttributes.Instance.GetAttribute<string>("AppName"))
        {
            if (MessagesDataManager is not null)
                await MessagesDataManager.SendMessage(UserName, "Cannot update self", cancellationToken);
            Logger.LogError("Cannot update self");
            return new Err[]
            {
                new()
                {
                    ErrorCode = "CannotUpdateSelf",
                    ErrorMessage = "Cannot update self"
                }
            };
        }

        var exchangeFileManager = FileManagersFabric.CreateFileManager(UseConsole, Logger,
            _applicationUpdaterParameters.InstallerWorkFolder,
            _applicationUpdaterParameters.ProgramExchangeFileStorage);

        if (exchangeFileManager is null)
        {
            if (MessagesDataManager is not null)
                await MessagesDataManager.SendMessage(UserName,
                    "exchangeFileManager is null when UpdateProgramWithParameters", cancellationToken);
            Logger.LogError("exchangeFileManager is null when UpdateProgramWithParameters");
            return new Err[]
            {
                new()
                {
                    ErrorCode = "exchangeFileManagerIsNull",
                    ErrorMessage = "exchangeFileManager is null in UpdateProgramWithParameters"
                }
            };
        }

        var runTime = _installer.Runtime;
        var programArchiveExtension = _applicationUpdaterParameters.ProgramArchiveExtension;
        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName,
                $"GetFileParameters with parameters: projectName={projectName}, environmentName={environmentName}, _serviceInstaller.Runtime={runTime}, _applicationUpdaterParameters.ProgramArchiveExtension={programArchiveExtension}",
                cancellationToken);
        Logger.LogInformation(
            "GetFileParameters with parameters: projectName={projectName}, environmentName={environmentName}, _serviceInstaller.Runtime={runTime}, _applicationUpdaterParameters.ProgramArchiveExtension={programArchiveExtension}",
            projectName, environmentName, runTime, programArchiveExtension);

        var (prefix, dateMask, suffix) = GetFileParameters(projectName, environmentName, _installer.Runtime,
            _applicationUpdaterParameters.ProgramArchiveExtension);

        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName,
                $"GetFileParameters results is: prefix={prefix}, dateMask={dateMask}, suffix={suffix}",
                cancellationToken);
        Logger.LogInformation("GetFileParameters results is: prefix={prefix}, dateMask={dateMask}, suffix={suffix}",
            prefix, dateMask, suffix);

        //დავადგინოთ გაცვლით სერვერზე
        //{_projectName} სახელით არსებული საინსტალაციო არქივები თუ არსებობს
        //და ავარჩიოთ ყველაზე ახალი
        var lastFileInfo = exchangeFileManager.GetLastFileInfo(prefix, dateMask, suffix);
        if (lastFileInfo == null)
        {
            if (MessagesDataManager is not null)
                await MessagesDataManager.SendMessage(UserName, "Project archive files not found on exchange storage",
                    cancellationToken);
            Logger.LogError("Project archive files not found on exchange storage");
            return new Err[]
            {
                new()
                {
                    ErrorCode = "ProjectArchiveFilesNotFoundOnExchangeStorage",
                    ErrorMessage = "Project archive files not found on exchange storage"
                }
            };
        }

        var localArchiveFileName =
            Path.Combine(_applicationUpdaterParameters.InstallerWorkFolder, lastFileInfo.FileName);
        //თუ ფაილი უკვე მოქაჩულია, მეორედ მისი მოქაჩვა საჭირო არ არის
        if (!File.Exists(localArchiveFileName))
            //მოვქაჩოთ არჩეული საინსტალაციო არქივი
            if (!exchangeFileManager.DownloadFile(lastFileInfo.FileName,
                    _applicationUpdaterParameters.DownloadTempExtension))
            {
                if (MessagesDataManager is not null)
                    await MessagesDataManager.SendMessage(UserName, "Project archive file not downloaded",
                        cancellationToken);
                Logger.LogError("Project archive file not downloaded");
                return new Err[]
                {
                    new()
                    {
                        ErrorCode = "ProjectArchiveFileWasNotDownloaded",
                        ErrorMessage = "Project archive file not downloaded"
                    }
                };
            }

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

        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName, $"Cannot Update {projectName}/{environmentName}",
                cancellationToken);
        Logger.LogError("Cannot Update {projectName}/{environmentName}", projectName, environmentName);
        return new Err[]
        {
            new()
            {
                ErrorCode = "CannotUpdateProject",
                ErrorMessage = $"Cannot Update {projectName}/{environmentName}"
            }
        };
    }

    public async Task<OneOf<string, Err[]>> UpdateServiceWithParameters(string projectName, string environmentName,
        string serviceUserName, string? serviceName, string? appSettingsFileName, string? serviceDescriptionSignature,
        string? projectDescription, CancellationToken cancellationToken)
    {
        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName,
                $"starting UpdateProgramWithParameters with parameters: projectName={projectName}, environmentName={environmentName}, serviceUserName={serviceUserName}, serviceName={serviceName}",
                cancellationToken);
        Logger.LogInformation(
            "starting UpdateProgramWithParameters with parameters: projectName={projectName}, environmentName={environmentName}, serviceUserName={serviceUserName}, serviceName={serviceName}",
            projectName, environmentName, serviceUserName, serviceName);


        if (projectName == ProgramAttributes.Instance.GetAttribute<string>("AppName"))
        {
            if (MessagesDataManager is not null)
                await MessagesDataManager.SendMessage(UserName, "Cannot update self", cancellationToken);
            Logger.LogError("Cannot update self");
            return new Err[]
            {
                new()
                {
                    ErrorCode = "CannotUpdateSelf",
                    ErrorMessage = "Cannot update self"
                }
            };
        }

        var exchangeFileManager = FileManagersFabric.CreateFileManager(UseConsole, Logger,
            _applicationUpdaterParameters.InstallerWorkFolder,
            _applicationUpdaterParameters.ProgramExchangeFileStorage);

        if (exchangeFileManager is null)
        {
            if (MessagesDataManager is not null)
                await MessagesDataManager.SendMessage(UserName,
                    "exchangeFileManager is null when UpdateProgramWithParameters", cancellationToken);
            Logger.LogError("exchangeFileManager is null when UpdateProgramWithParameters");
            return new Err[]
            {
                new()
                {
                    ErrorCode = "exchangeFileManagerIsNull",
                    ErrorMessage = "exchangeFileManager is null in UpdateProgramWithParameters"
                }
            };
        }

        var runtime = _installer.Runtime;
        var programArchiveExtension = _applicationUpdaterParameters.ProgramArchiveExtension;

        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName,
                $"GetFileParameters with parameters: projectName={projectName}, environmentName={environmentName}, _serviceInstaller.Runtime={runtime}, _applicationUpdaterParameters.ProgramArchiveExtension={programArchiveExtension}",
                cancellationToken);
        Logger.LogInformation(
            "GetFileParameters with parameters: projectName={projectName}, environmentName={environmentName}, _serviceInstaller.Runtime={runtime}, _applicationUpdaterParameters.ProgramArchiveExtension={programArchiveExtension}",
            projectName, environmentName, runtime, programArchiveExtension);

        var (prefix, dateMask, suffix) = GetFileParameters(projectName, environmentName, _installer.Runtime,
            _applicationUpdaterParameters.ProgramArchiveExtension);

        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName,
                $"GetFileParameters results is: prefix={prefix}, dateMask={dateMask}, suffix={suffix}",
                cancellationToken);
        Logger.LogInformation("GetFileParameters results is: prefix={prefix}, dateMask={dateMask}, suffix={suffix}",
            prefix, dateMask, suffix);


        //დავადგინოთ გაცვლით სერვერზე
        //{_projectName} სახელით არსებული საინსტალაციო არქივები თუ არსებობს
        //და ავარჩიოთ ყველაზე ახალი
        var lastFileInfo = exchangeFileManager.GetLastFileInfo(prefix, dateMask, suffix);
        if (lastFileInfo == null)
        {
            if (MessagesDataManager is not null)
                await MessagesDataManager.SendMessage(UserName, "Project archive files not found on exchange storage",
                    cancellationToken);
            Logger.LogError("Project archive files not found on exchange storage");
            return new Err[]
            {
                new()
                {
                    ErrorCode = "ProjectArchiveFilesNotFoundOnExchangeStorage",
                    ErrorMessage = "Project archive files not found on exchange storage"
                }
            };
        }

        var localArchiveFileName =
            Path.Combine(_applicationUpdaterParameters.InstallerWorkFolder, lastFileInfo.FileName);
        //თუ ფაილი უკვე მოქაჩულია, მეორედ მისი მოქაჩვა საჭირო არ არის
        if (!File.Exists(localArchiveFileName))
            //მოვქაჩოთ არჩეული საინსტალაციო არქივი
            if (!exchangeFileManager.DownloadFile(lastFileInfo.FileName,
                    _applicationUpdaterParameters.DownloadTempExtension))
            {
                if (MessagesDataManager is not null)
                    await MessagesDataManager.SendMessage(UserName, "Project archive file was not downloaded",
                        cancellationToken);
                Logger.LogError("Project archive file was not downloaded");
                return new Err[]
                {
                    new()
                    {
                        ErrorCode = "ProjectArchiveFileWasNotDownloaded",
                        ErrorMessage = "Project archive file was not downloaded"
                    }
                };
            }

        FileNameAndTextContent? appSettingsFile = null;
        //string? appSettingsFileBody = null;
        if (!string.IsNullOrWhiteSpace(appSettingsFileName))
        {
            //მოვქაჩოთ ბოლო პარამეტრების ფაილი
            var appSettingsFileBody = await GetParametersFileBody(projectName, environmentName,
                _applicationUpdaterParameters.ProgramExchangeFileStorage,
                _applicationUpdaterParameters.ParametersFileDateMask,
                _applicationUpdaterParameters.ParametersFileExtension, cancellationToken);
            if (appSettingsFileBody is null)
            {
                if (MessagesDataManager is not null)
                    await MessagesDataManager.SendMessage(UserName,
                        $"Cannot Update {projectName}, because cannot get latest parameters file", cancellationToken);
                Logger.LogError("Cannot Update {projectName}, because cannot get latest parameters file", projectName);
                if (string.IsNullOrWhiteSpace(serviceName))
                    return new Err[]
                    {
                        new()
                        {
                            ErrorCode = "CannotUpdateProject",
                            ErrorMessage = $"Cannot Update {projectName}, because cannot get latest parameters file"
                        }
                    };
            }

            if (appSettingsFileBody is not null)
                appSettingsFile =
                    new FileNameAndTextContent(appSettingsFileName, appSettingsFileBody);
        }


        var resolvedServiceUserName = ResolveServiceUserName(serviceUserName);

        var runUpdateServiceResult = await _installer.RunUpdateService(lastFileInfo.FileName, projectName, serviceName,
            environmentName, appSettingsFile, resolvedServiceUserName, _applicationUpdaterParameters.FilesUserName,
            _applicationUpdaterParameters.FilesUsersGroupName, _applicationUpdaterParameters.InstallerWorkFolder,
            _applicationUpdaterParameters.InstallFolder, serviceDescriptionSignature, projectDescription,
            cancellationToken);

        if (runUpdateServiceResult.IsT1)
            return runUpdateServiceResult.AsT1;

        var assemblyVersion = runUpdateServiceResult.AsT0;
        if (assemblyVersion != null)
            return assemblyVersion;

        if (MessagesDataManager is not null)
            await MessagesDataManager.SendMessage(UserName, $"Cannot Update {projectName}/{environmentName}",
                cancellationToken);
        Logger.LogError("Cannot Update {projectName}/{environmentName}", projectName, environmentName);
        return new Err[]
        {
            new()
            {
                ErrorCode = "CannotUpdateProjectEnvironment",
                ErrorMessage = $"Cannot Update {projectName}/{environmentName}"
            }
        };
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