﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DbTools.Errors;
using DbTools.Models;
using LanguageExt;
using LibDatabaseParameters;
using Microsoft.Extensions.Logging;
using OneOf;
using SystemToolsShared.Errors;
using WebAgentDatabasesApiContracts;
using WebAgentDatabasesApiContracts.V1.Responses;

namespace DatabasesManagement;

public sealed class RemoteDatabaseManager : IDatabaseManager
{
    private readonly DatabaseApiClient _databaseApiClient;
    private readonly ILogger _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public RemoteDatabaseManager(ILogger logger, DatabaseApiClient databaseApiClient)
    {
        _logger = logger;
        _databaseApiClient = databaseApiClient;
    }

    //დამზადდეს ბაზის სარეზერვო ასლი სერვერის მხარეს.
    //ასევე ამ მეთოდის ამოცანაა უზრუნველყოს ბექაპის ჩამოსაქაჩად ხელმისაწვდომ ადგილას მოხვედრა
    public async Task<OneOf<BackupFileParameters, Err[]>> CreateBackup(
        DatabaseBackupParametersDomain databaseBackupParametersModel, string backupBaseName,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(backupBaseName))
            return await _databaseApiClient.CreateBackup(databaseBackupParametersModel.BackupNamePrefix,
                databaseBackupParametersModel.DateMask, databaseBackupParametersModel.BackupFileExtension,
                databaseBackupParametersModel.BackupNameMiddlePart, databaseBackupParametersModel.Compress,
                databaseBackupParametersModel.Verify, databaseBackupParametersModel.BackupType,
                databaseBackupParametersModel.DbServerSideBackupPath, backupBaseName, cancellationToken);

        _logger.LogError(DbClientErrors.DatabaseNameIsNotSpecifiedForBackup.ErrorMessage);
        return new[] { DbClientErrors.DatabaseNameIsNotSpecifiedForBackup };
    }

    //მონაცემთა ბაზების სიის მიღება სერვერიდან
    public async Task<OneOf<List<DatabaseInfoModel>, Err[]>> GetDatabaseNames(CancellationToken cancellationToken)
    {
        return await _databaseApiClient.GetDatabaseNames(cancellationToken);
    }

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, იმის დასადგენად,
    //მიზნის ბაზა უკვე არსებობს თუ არა, რომ არ მოხდეს ამ ბაზის ისე წაშლა ახლით,
    //რომ არსებულის გადანახვა არ მოხდეს.
    public async Task<OneOf<bool, Err[]>> IsDatabaseExists(string databaseName, CancellationToken cancellationToken)
    {
        return await _databaseApiClient.IsDatabaseExists(databaseName, cancellationToken);
    }

    //გამოიყენება ბაზის დამაკოპირებელ ინსტრუმენტში, დაკოპირებული ბაზის აღსადგენად,
    public async Task<Option<Err[]>> RestoreDatabaseFromBackup(BackupFileParameters backupFileParameters,
        string? destinationDbServerSideDataFolderPath, string? destinationDbServerSideLogFolderPath,
        string databaseName, CancellationToken cancellationToken, string? restoreFromFolderPath = null)
    {
        return await _databaseApiClient.RestoreDatabaseFromBackup(backupFileParameters.Prefix,
            backupFileParameters.Suffix, backupFileParameters.Name, backupFileParameters.DateMask,
            destinationDbServerSideDataFolderPath, destinationDbServerSideLogFolderPath, databaseName,
            cancellationToken);
    }

    //შემოწმდეს არსებული ბაზის მდგომარეობა და საჭიროების შემთხვევაში გამოასწოროს ბაზა
    public async Task<Option<Err[]>> CheckRepairDatabase(string databaseName, CancellationToken cancellationToken)
    {
        return await _databaseApiClient.CheckRepairDatabase(databaseName, cancellationToken);
    }

    //სერვერის მხარეს მონაცემთა ბაზაში ბრძანების გაშვება
    public async Task<Option<Err[]>> ExecuteCommand(string executeQueryCommand, CancellationToken cancellationToken,
        string? databaseName = null)
    {
        return await _databaseApiClient.ExecuteCommand(executeQueryCommand, cancellationToken, databaseName);
    }

    public Task<OneOf<DbServerInfo, Err[]>> GetDatabaseServerInfo(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<OneOf<bool, Err[]>> IsServerLocal(CancellationToken cancellationToken)
    {
        return await Task.FromResult(false);
    }

    //მონაცემთა ბაზაში არსებული პროცედურების რეკომპილირება
    public async Task<Option<Err[]>> RecompileProcedures(string databaseName, CancellationToken cancellationToken)
    {
        return await _databaseApiClient.RecompileProcedures(databaseName, cancellationToken);
    }

    public async Task<Option<Err[]>> TestConnection(string? databaseName, CancellationToken cancellationToken)
    {
        return await _databaseApiClient.TestConnection(databaseName, cancellationToken);
    }

    //მონაცემთა ბაზაში არსებული სტატისტიკების დაანგარიშება
    public async Task<Option<Err[]>> UpdateStatistics(string databaseName, CancellationToken cancellationToken)
    {
        return await _databaseApiClient.UpdateStatistics(databaseName, cancellationToken);
    }

    public Task<Option<Err[]>> SetDefaultFolders(string defBackupFolder, string defDataFolder, string defLogFolder,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}