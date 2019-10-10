using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace BenMcCallum.DotNet.FixReferences
{
    public static class Common
    {
        public static Regex SlnFileCsProjRegex = new Regex(", \"(.*).csproj\"", RegexOptions.Compiled);
        public static Regex CsProjRegex = new Regex("Include=\"(.*).csproj\"", RegexOptions.Compiled);

        public static string ExtractCsProjName(string input)
        {
            var lastSlashIndex = input.LastIndexOf('\\');
            if (lastSlashIndex > 0)
            {
                input = input.Substring(lastSlashIndex + 1);
            }
            return input.TrimEnd('\"');
        }

        public static string FindCsProjFilePath(string[] csProjFilePaths, string csProjFileName, string sourcedFromPath = null)
        {
            try
            {
                return csProjFilePaths.Single(fp => Path.GetFileName(fp) == csProjFileName);
            }
            catch (Exception ex)
            {
                WriteError($"Could not find csproj file path for: '{ csProjFileName}'.", ex);
                throw;
            }
        }

        public static void WriteError(string msg, Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(msg);
            Console.Error.WriteLine($"Exception: {ex}");
            Console.ResetColor();
        }

    }
}
