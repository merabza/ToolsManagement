using System;
using System.Text;
using SystemToolsShared;

namespace FileManagersMain;

public sealed class MaskManager
{
    private const string YearMask = "yyyy";
    private const string MonthMask = "MM";
    private const string DayMask = "dd";
    private const string HourMask = "HH";
    private const string MinuteMask = "mm";
    private const string SecondMask = "ss";

    private readonly string _dateMask;
    private readonly string _fileNamePrefix;


    public MaskManager(string fileNamePrefix, string dateMask)
    {
        _dateMask = dateMask;
        _fileNamePrefix = fileNamePrefix;
        YearPosition = _fileNamePrefix.Length + _dateMask.IndexOf(YearMask, StringComparison.Ordinal) + 1;
        MonthPosition = _fileNamePrefix.Length + _dateMask.IndexOf(MonthMask, StringComparison.Ordinal) + 1;
        DayPosition = _fileNamePrefix.Length + _dateMask.IndexOf(DayMask, StringComparison.Ordinal) + 1;
        HourPosition = _fileNamePrefix.Length + _dateMask.IndexOf(HourMask, StringComparison.Ordinal) + 1;
        MinutePosition = _fileNamePrefix.Length + _dateMask.IndexOf(MinuteMask, StringComparison.Ordinal) + 1;
        SecondPosition = _fileNamePrefix.Length + _dateMask.IndexOf(SecondMask, StringComparison.Ordinal) + 1;
    }

    public int YearPosition { get; }
    public int MonthPosition { get; }
    public int DayPosition { get; }
    public int HourPosition { get; }
    public int MinutePosition { get; }
    public int SecondPosition { get; }

    internal DateTime GetDateTimeByMask(string fileName)
    {
        return new DateTime(Convert.ToInt32(fileName.Substring(YearPosition, YearMask.Length)),
            Convert.ToInt32(fileName.Substring(MonthPosition, MonthMask.Length)),
            Convert.ToInt32(fileName.Substring(DayPosition, DayMask.Length)),
            Convert.ToInt32(fileName.Substring(HourPosition, HourMask.Length)),
            Convert.ToInt32(fileName.Substring(MinutePosition, MinuteMask.Length)),
            Convert.ToInt32(fileName.Substring(SecondPosition, SecondMask.Length)));
    }

    public string GetFullMask(string extension)
    {
        return _fileNamePrefix + GetMasked(_dateMask) + extension.AddNeedLeadPart(".");
    }

    public string GetTempFullMask(string extension, string tempExtension)
    {
        return _fileNamePrefix + GetMasked(_dateMask) + extension.AddNeedLeadPart(".") +
               tempExtension.AddNeedLeadPart(".");
    }

    public string GetFileNameForDate(DateTime forDate, string extension)
    {
        return _fileNamePrefix + forDate.ToString(_dateMask) + extension.AddNeedLeadPart(".");
    }

    private static string GetMasked(string str)
    {
        var sb = new StringBuilder();
        foreach (var c in str) sb.Append(c != '_' ? '?' : c);
        return sb.ToString();
    }
}