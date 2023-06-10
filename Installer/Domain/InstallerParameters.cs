//using System;
//using SystemToolsShared;

//namespace Installer.Domain;

//internal sealed class InstallerParameters
//{

//    public ApiClientSettingsDomain? WebAgentForInstall { get; }

//    public LocalInstallerSettingsDomain? localInstallerSettingsDomain { get; }

//    public static InstallerParameters Create()
//    {
//        try
//        {
//            //CheckVersionParameters? checkVersionParameters =
//            //    CheckVersionParameters.Create(supportToolsParameters, projectName, serverName);
//            //if (checkVersionParameters is null)
//            //    return null;

//            //ProjectModel project = supportToolsParameters.GetProjectRequired(projectName);

//            //if (checkService && !project.IsService)
//            //{
//            //    StShared.WriteErrorLine($"Project {projectName} is not service", true);
//            //    return null;
//            //}

//            //if (string.IsNullOrWhiteSpace(project.ServiceName))
//            //{
//            //    StShared.WriteErrorLine(
//            //        $"Service Name does not specified for Project {projectName} and server {serverName}", true);
//            //    return null;

//            //}

//            //ServerInfoModel serverInfo = project.GetServerInfoRequired(serverName);
//            //ServerDataModel server = supportToolsParameters.GetServerDataRequired(serverName);

//            ApiClientSettingsDomain? webAgentForInstall = null;
//            string? installFolder = null;
//            if (!server.IsLocal)
//            {
//                string? webAgentNameForInstall =
//                    project.UseAlternativeWebAgent ? server.WebAgentInstallerName : server.WebAgentName;
//                if (string.IsNullOrWhiteSpace(webAgentNameForInstall))
//                {
//                    StShared.WriteErrorLine(
//                        $"webAgentNameForCheck does not specified for Project {projectName} and server {serverName}",
//                        true);
//                    return null;

//                }

//                webAgentForInstall = supportToolsParameters.GetWebAgentRequired(webAgentNameForInstall);

//                if (string.IsNullOrWhiteSpace(serverInfo.ApiVersionId))
//                {
//                    StShared.WriteErrorLine(
//                        $"ApiVersionId does not specified for Project {projectName} and server {serverName}", true);
//                    return null;

//                }
//            }
//            else
//            {
//                installFolder = supportToolsParameters.LocalInstallerSettings?.InstallFolder;
//                if ( string.IsNullOrWhiteSpace(installFolder  ) )
//                {
//                    StShared.WriteErrorLine($"Server {serverName} is local, but installFolder does not specified is Parameters", true);
//                    return null;
//                }

//            }

//            return new ServiceStartStopParameters(projectName, project.ServiceName, webAgentForInstall, installFolder,
//                serverInfo.ServerSidePort, serverInfo.ApiVersionId, checkVersionParameters.WebAgentForCheck);

//        }
//        catch (Exception e)
//        {
//            StShared.WriteErrorLine(e.Message, true);
//            return null;
//        }
//    }

//    public InstallerParameters(ApiClientSettingsDomain? webAgentForInstall, LocalInstallerSettingsDomain? localInstallerSettingsDomain)
//    {
//        WebAgentForInstall = webAgentForInstall;
//        this.localInstallerSettingsDomain = localInstallerSettingsDomain;
//    }
//}

