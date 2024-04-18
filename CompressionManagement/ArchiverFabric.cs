using System.IO;
using LibFileParameters.Models;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace CompressionManagement;

//არქივატორების ფაბრიკა. გამოიყენება ReServer პროექტში
public static class ArchiverFabric
{
    public static Archiver? CreateArchiver(ILogger logger, bool useConsole, Archivers archivers, string? archiverName)
    {
        if (string.IsNullOrWhiteSpace(archiverName))
        {
            StShared.WriteErrorLine("archiverName does not specified", useConsole, logger);
            return null;
        }

        var archiverData = archivers.GetArchiverDataByKey(archiverName);

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
            return new ZipClassArchiver(logger, useConsole, fileExtension);

        //დადგინდეს გვაქვს თუ არა ინფორმაცია არქივატორის გამშვები ფაილის შესახებ
        if (string.IsNullOrWhiteSpace(compressProgramPatch))
        {
            logger.LogError("Archiver program path is not specified for {archiveType}", archiveType);
            return null;
        }

        //დავადგინოთ არქივატორი ადგილზეა თუ არა
        FileInfo compressProgram = new(compressProgramPatch);
        if (!compressProgram.Exists)
        {
            logger.LogError("Archiver program path is invalid for {archiveType}", archiveType);
            return null;
        }


        if (archiveType != EArchiveType.Zip)
            return archiveType switch
            {
                EArchiveType.Rar => new RarArchiver(logger, compressProgramPatch, useConsole, fileExtension),
                _ => null
            };

        if (string.IsNullOrWhiteSpace(decompressProgramPatch))
        {
            logger.LogError("Archiver decompress program path is invalid for {archiveType}", archiveType);
            return null;
        }

        FileInfo decompressProgram = new(decompressProgramPatch);

        if (decompressProgram.Exists)
            return new ZipArchiver(useConsole, fileExtension);
        logger.LogError("Archiver decompress program path is invalid for {archiveType}", archiveType);
        return null;
    }
}