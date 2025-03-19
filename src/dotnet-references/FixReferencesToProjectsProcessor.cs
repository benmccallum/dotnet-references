using System.Text.RegularExpressions;
using static BenMcCallum.DotNet.References.Common;

namespace BenMcCallum.DotNet.References
{
    public static class FixReferencesToProjectsProcessor
    {
        public static void Process(string directoryPath)
        {
            var slnFilePaths = Directory.GetFiles(directoryPath, "*.sln", SearchOption.AllDirectories);
            var csProjFilePaths = GetCsProjFilePaths(directoryPath);

            foreach (var slnFilePath in slnFilePaths)
            {
                try
                {
                    FixReferencesInSlnFile(csProjFilePaths, slnFilePath);
                }
                catch (Exception ex)
                {
                    WriteException($"Error fixing references in {slnFilePath}.", ex);
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
                    WriteException($"Error fixing references in {csProjFilePath}.", ex);
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
    }
}
