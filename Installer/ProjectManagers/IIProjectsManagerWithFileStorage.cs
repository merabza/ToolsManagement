﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LanguageExt;
using OneOf;
using SystemToolsShared.Errors;

namespace Installer.ProjectManagers;

public interface IIProjectsManagerWithFileStorage // : IDisposable
{
    ValueTask<Option<IEnumerable<Err>>> UpdateAppParametersFile(string projectName, string environmentName,
        string appSettingsFileName, string parametersFileDateMask, string parametersFileExtension,
        CancellationToken cancellationToken = default);

    ValueTask<OneOf<string, IEnumerable<Err>>> InstallProgram(string projectName, string environmentName,
        string programArchiveDateMask, string programArchiveExtension, string parametersFileDateMask,
        string parametersFileExtension, CancellationToken cancellationToken = default);

    ValueTask<OneOf<string, IEnumerable<Err>>> InstallService(string projectName, string environmentName,
        string serviceUserName, string appSettingsFileName, string programArchiveDateMask,
        string programArchiveExtension, string parametersFileDateMask, string parametersFileExtension,
        string? serviceDescriptionSignature, string? projectDescription, CancellationToken cancellationToken = default);
}