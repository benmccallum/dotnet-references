using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace BenMcCallum.DotNet.FixReferences
{
    class Program
    {
        private static Regex _slnFileCsProjRegex = new Regex(", \"(.*).csproj\"", RegexOptions.Compiled);
        private static Regex _csProjRegex = new Regex("Include=\"(.*).csproj\"", RegexOptions.Compiled);

        private static int Main(string[] args)
        {
            try
            {
                if (args == null || !args.Any() || !Directory.Exists(args[0]))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine("A valid base directory path is required.");
                    Console.ResetColor();
                    return 1;
                }

                FixReferences(args[0]);
                Console.WriteLine("Done.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("Unexpected error: " + ex.ToString());
                Console.ResetColor();
                return 999;
            }
        }

        private static void FixReferences(string directoryPath)
        {
            var slnFilePaths = Directory.GetFiles(directoryPath, "*.sln", SearchOption.AllDirectories);
            var csProjFilePaths = Directory.GetFiles(directoryPath, "*.csproj", SearchOption.AllDirectories);

            foreach (var slnFilePath in slnFilePaths)
            {
                var slnFileContents = File.ReadAllText(slnFilePath);
                var matches = _slnFileCsProjRegex.Matches(slnFileContents);
                foreach (Match match in matches)
                {
                    var lastSlashIndex = match.Value.LastIndexOf('\\');
                    var csProjFileName = match.Value.Substring(lastSlashIndex + 1).TrimEnd('\"');
                    var csProjFilePath = FindCsProjFilePath(csProjFilePaths, csProjFileName);
                    var csProjRelativeFilePath = GetRelativePathTo(slnFilePath, csProjFilePath);
                    slnFileContents = slnFileContents.Replace(match.Value, $", \"{csProjRelativeFilePath}\"");
                }

                if (slnFileContents != File.ReadAllText(slnFilePath))
                {
                    File.WriteAllText(slnFilePath, slnFileContents);
                    Console.WriteLine($"Fixed references in {slnFilePath}");
                }
                else
                {
                    Console.WriteLine($"No changes required in {slnFilePath}");
                }
            }

            foreach (var csProjFilePath in csProjFilePaths)
            {
                var csProjFileContents = File.ReadAllText(csProjFilePath);
                var matches = _csProjRegex.Matches(csProjFileContents);
                foreach (Match match in matches)
                {
                    var lastSlashIndex = match.Value.LastIndexOf('\\');
                    var csProjDepFileName = match.Value.Substring(lastSlashIndex + 1).TrimEnd('\"');
                    var csProjDepFilePath = FindCsProjFilePath(csProjFilePaths, csProjDepFileName);
                    var csProjDepRelativeFilePath = GetRelativePathTo(csProjFilePath, csProjDepFilePath);
                    csProjFileContents = csProjFileContents.Replace(match.Value, $"Include=\"{csProjDepRelativeFilePath}\"");
                }

                if (csProjFileContents != File.ReadAllText(csProjFilePath))
                {
                    File.WriteAllText(csProjFilePath, csProjFileContents);
                    Console.WriteLine($"Fixed references in {csProjFilePath}");
                }
                else
                {
                    Console.WriteLine($"No changes required in {csProjFilePath}");
                }
            }
        }

        private static string FindCsProjFilePath(string[] csProjFiles, string csProjFileName)
        {
            return csProjFiles.Single(f => f.EndsWith($"\\{csProjFileName}"));
        }

        private static string GetRelativePathTo(string fromPath, string toPath)
        {
            return GetRelativePathTo(new FileInfo(fromPath), new FileInfo(toPath));
        }

        private static string GetRelativePathTo(FileSystemInfo from, FileSystemInfo to)
        {
            Func<FileSystemInfo, string> getPath = fsi =>
            {
                var d = fsi as DirectoryInfo;
                return d == null ? fsi.FullName : d.FullName.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            };

            var fromPath = getPath(from);
            var toPath = getPath(to);

            var fromUri = new Uri(fromPath);
            var toUri = new Uri(toPath);

            var relativeUri = fromUri.MakeRelativeUri(toUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            return relativePath.Replace('/', Path.DirectorySeparatorChar);
        }
    }
}
