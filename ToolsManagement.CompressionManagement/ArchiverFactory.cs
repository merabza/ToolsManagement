using System.IO;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibFileParameters.Models;
using SystemTools.SystemToolsShared;

namespace ToolsManagement.CompressionManagement;

//არქივატორების ფაბრიკა. გამოიყენება ReServer პროექტში
public static class ArchiverFactory
{
    public static Archiver? CreateArchiver(ILogger logger, bool useConsole, Archivers archivers, string? archiverName)
    {
        if (string.IsNullOrWhiteSpace(archiverName))
        {
            StShared.WriteErrorLine("archiverName does not specified", useConsole, logger);
            return null;
        }

        ArchiverData? archiverData = archivers.GetArchiverDataByKey(archiverName);

        if (archiverData is null)
        {
            StShared.WriteErrorLine("Archiver not specified for Files Backup step", useConsole, logger);
            return null;
        }

        if (string.IsNullOrWhiteSpace(archiverData.FileExtension))
        {
            StShared.WriteErrorLine($"FileExtension does not specified for Archiver with name {archiverName}",
                useConsole, logger);
            return null;
        }

        return CreateArchiverByType(useConsole, logger, archiverData.Type, archiverData.CompressProgramPatch,
            archiverData.DecompressProgramPatch, archiverData.FileExtension);
    }

    public static Archiver? CreateArchiverByType(bool useConsole, ILogger logger, EArchiveType archiveType,
        string? compressProgramPatch, string? decompressProgramPatch, string fileExtension)
    {
        if (archiveType == EArchiveType.ZipClass)
        {
            return new ZipClassArchiver(logger, useConsole, fileExtension);
        }

        //დადგინდეს გვაქვს თუ არა ინფორმაცია არქივატორის გამშვები ფაილის შესახებ
        if (string.IsNullOrWhiteSpace(compressProgramPatch))
        {
            logger.LogError("Archiver program path is not specified for {ArchiveType}", archiveType);
            return null;
        }

        //დავადგინოთ არქივატორი ადგილზეა თუ არა
        var compressProgram = new FileInfo(compressProgramPatch);
        if (!compressProgram.Exists)
        {
            logger.LogError("Archiver program path is invalid for {ArchiveType}", archiveType);
            return null;
        }

        if (archiveType != EArchiveType.Zip)
        {
            return archiveType switch
            {
                EArchiveType.Rar => new RarArchiver(logger, compressProgramPatch, useConsole, fileExtension),
                _ => null
            };
        }

        if (string.IsNullOrWhiteSpace(decompressProgramPatch))
        {
            logger.LogError("Archiver decompress program path is invalid for {ArchiveType}", archiveType);
            return null;
        }

        var decompressProgram = new FileInfo(decompressProgramPatch);

        if (decompressProgram.Exists)
        {
            return new ZipArchiver(useConsole, fileExtension);
        }

        logger.LogError("Archiver decompress program path is invalid for {ArchiveType}", archiveType);
        return null;
    }
}
