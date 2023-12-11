using System.Collections.Generic;
using System.Linq;

namespace FileManagersMain;

public static class FileStringExtension
{

    public static List<string> PrepareAfterRootPath(this string afterRootPath, char directorySeparatorChar)
    {
        return afterRootPath.Split(directorySeparatorChar).Select(s => s.Trim()).ToList();
    }

}