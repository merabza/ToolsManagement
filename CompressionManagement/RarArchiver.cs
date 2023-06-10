using System.Linq;
using LibToolActions.BackgroundTasks;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace CompressionManagement;

//Rar არქივატორი. გამოიყენება ReServer პროექტში
public sealed class RarArchiver : Archiver
{
    private readonly string _compressProgramPatch;
    private readonly ILogger _logger;

    public RarArchiver(ILogger logger, string compressProgramPatch, bool useConsole, string fileExtension) : base(
        useConsole, fileExtension)
    {
        _logger = logger;
        _compressProgramPatch = compressProgramPatch;
    }

    public override bool SourcesToArchive(string[] sources, string archiveFileName, string[]? excludes,
        ProcessManager? processManager = null)
    {
        //გადამრთველების მნიშვნელობა შემდეგია:
        //a არქივში ფაილების ჩამატების ბრძანება
        //-r რეკურსია, ანუ ქვე ფოლდერების გავლა არქივის შექმნისას
        //-m5 მაქსიმალური მე-5 დონის კომპრესია
        //-IBCK პროცესის გაშვება ბექგრაუნდში
        var programArguments = "a -r -m5 -IBCK"; // _archiversRow.archSwitches;

        //დავადგინოთ გვაქვს თუ არა გამორიცხვები გამოყენებული.
        //string[] excludes = GetExcludeList();
        if (excludes != null && excludes.Length > 0)
            //თუ გამორიცხვების რაოდენობა ცოტაა 3-მდე, მაშინ გამოვიყენოთ -X<file> გადამრთველი
            //if (Excludes.Length <= 10)
            //{
            programArguments =
                excludes.Aggregate(programArguments, (current, exclude) => current + " -X" + exclude);
        //}
        //else
        //{
        ////თუ გამორიცხვების რაოდენობა ბევრია, ან საჭიროა .gitignore-ს გაანალიზება. 
        ////მაშინ შევქმნათ მუშა ფოლდერში დროებითი ფაილი, რომელშიც ჩაიწერება ყველა გამორიცხვის შესახებ ინფორმაცია
        ////და ამ შემთხვევაში უნდა გამოვიყენოთ -X@<listfile> გადამრთველი, რომლის საშუალებითაც არქივატორს მივაწვდით გამორიცხვების სიას.
        //}

        programArguments += " \"" + archiveFileName + "\"";

        programArguments =
            sources.Aggregate(programArguments, (current, source) => current + " \"" + source + "\"");

        return StShared.RunProcess(UseConsole, _logger, _compressProgramPatch, programArguments);


        //ProgRunner runProg = new ProgRunner(progPath, programArguments);
        //runProg.Execute();
        //return runProg.ExitCode == 0;
    }
}