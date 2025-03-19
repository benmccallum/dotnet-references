using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace BenMcCallum.DotNet.References
{
    public static class Common
    {
        public static Regex SlnFileCsProjRegex = new Regex(", \"(.*).csproj\"", RegexOptions.Compiled);
        public static Regex CsProjRegex = new Regex("Include=\"(.*).csproj\"", RegexOptions.Compiled);

        public static string[] GetCsProjFilePaths(string workingDirectory)
        {
            return Directory.GetFiles(workingDirectory, "*.csproj", SearchOption.AllDirectories)
                // Excluding those in pesky NuGet package folders
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}packages{Path.DirectorySeparatorChar}"))
                .OrderBy(path => path)
                .ToArray();
        }

        public static string ExtractCsProjName(string input)
        {
            // Needs to handle the following inputs:
            //   - ", \"SomeProject\SomeProject.csproj\"" (a reference to a project from a .sln file)
            //   - "SomeProject\SomeProject.csproj" (a reference to a project from a .csproj file)

            var normalizedInput = input.Replace("\\", "/");
            var lastSlashIndex = normalizedInput.LastIndexOf('/');
            if (lastSlashIndex > 0)
            {
                normalizedInput = normalizedInput.Substring(lastSlashIndex + 1);
            }
            return normalizedInput.TrimEnd('\"');
        }

        public static string FindCsProjFilePath(string[] csProjFilePaths, string csProjFileName)
        {
            try
            {
                return csProjFilePaths.Single(fp => Path.GetFileName(fp) == csProjFileName);
            }
            catch (Exception ex)
            {
                WriteException($"Could not find csproj file path for: '{csProjFileName}'.", ex);
                throw;
            }
        }

        public static string GetRelativePathTo(string fromPath, string toPath)
        {
            return GetRelativePathTo(new FileInfo(fromPath), new FileInfo(toPath));
        }

        public static string GetRelativePathTo(FileSystemInfo from, FileSystemInfo to)
        {
            static string getPath(FileSystemInfo fsi)
            {
                return fsi is DirectoryInfo d
                    ? d.FullName.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar
                    : fsi.FullName;
            }

            var fromPath = getPath(from);
            var toPath = getPath(to);

            var fromUri = new Uri(fromPath);
            var toUri = new Uri(toPath);

            var relativeUri = fromUri.MakeRelativeUri(toUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            return relativePath.Replace('/', Path.DirectorySeparatorChar);
        }

        public static void WriteException(string msg, Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(msg);
            Console.Error.WriteLine($"Exception: {ex}");
            Console.ResetColor();
        }

        public static int WriteError(ErrorCode errorCode)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(Errors.ErrorMessages[errorCode]);
            Console.ResetColor();
            return (int)errorCode;
        }
    }
}
