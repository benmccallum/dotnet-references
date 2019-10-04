using System;
using System.IO;
using System.Text.RegularExpressions;
using static BenMcCallum.DotNet.FixReferences.Common;

namespace BenMcCallum.DotNet.FixReferences
{
    public static class FixReferencesToProjectsProcessor
    {
        public static void Process(string directoryPath)
        {
            var slnFilePaths = Directory.GetFiles(directoryPath, "*.sln", SearchOption.AllDirectories);
            var csProjFilePaths = Directory.GetFiles(directoryPath, "*.csproj", SearchOption.AllDirectories);

            foreach (var slnFilePath in slnFilePaths)
            {
                try
                {
                    FixReferencesInSlnFile(csProjFilePaths, slnFilePath);
                }
                catch (Exception ex)
                {
                    WriteError($"Error fixing references in {slnFilePath}.", ex);
                }
            }

            foreach (var csProjFilePath in csProjFilePaths)
            {
                try
                {
                    FixReferencesInProjectFile(csProjFilePaths, csProjFilePath);
                }
                catch (Exception ex)
                {
                    WriteError($"Error fixing references in {csProjFilePath}.", ex);
                }
            }
        }

        private static void FixReferencesInSlnFile(string[] csProjFilePaths, string slnFilePath)
        {
            var slnFileContents = File.ReadAllText(slnFilePath);
            var matches = SlnFileCsProjRegex.Matches(slnFileContents);
            foreach (Match match in matches)
            {
                var csProjFileName = ExtractCsProjName(match.Value);
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

        private static void FixReferencesInProjectFile(string[] csProjFilePaths, string csProjFilePath)
        {
            var csProjFileContents = File.ReadAllText(csProjFilePath);
            var matches = CsProjRegex.Matches(csProjFileContents);
            foreach (Match match in matches)
            {
                var csProjDepFileName = ExtractCsProjName(match.Value);
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
