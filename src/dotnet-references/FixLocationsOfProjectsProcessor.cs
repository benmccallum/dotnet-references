using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using static BenMcCallum.DotNet.References.Common;

namespace BenMcCallum.DotNet.References
{
    public static class FixLocationsOfProjectsProcessor
    {
        public static void Process(string slnFilePath, string workingDirectory, bool removeExtras)
        {
            Console.WriteLine("Starting process with the following args:");
            Console.WriteLine($"Solution File Path: {slnFilePath}");
            Console.WriteLine($"Current Working Directory: {workingDirectory}");
            Console.WriteLine($"Remove Extras: {removeExtras}");

            var csProjFilePaths = GetCsProjFilePaths(workingDirectory);
            //Console.WriteLine($"Found {csProjFilePaths.Length} .csproj files in {currentWorkingDirectory}.");

            var csProjFilesProcessed = new HashSet<string>();

            var slnFileDirectoryPath = Path.GetDirectoryName(slnFilePath);
            var slnFileContents = File.ReadAllText(slnFilePath);
            var matches = SlnFileCsProjRegex.Matches(slnFileContents);
            foreach (Match match in matches)
            {
                ProcessCsProjFileMatch(match, slnFileDirectoryPath, csProjFilesProcessed, csProjFilePaths);
            }

            if (removeExtras)
            {
                var toDelete = csProjFilePaths.Where(fp => !csProjFilesProcessed.Contains(Path.GetFileName(fp))).ToList();
                toDelete.ForEach(fp => File.Delete(fp));
            }
        }

        private static void ProcessCsProjFileMatch(Match match, string rootPath, HashSet<string> csProjFilesProcessed, string[] csProjFilePaths)
        {
            var csProjFileName = ExtractCsProjName(match.Value);
            if (csProjFilesProcessed.Contains(csProjFileName))
            {
                return;
            }

            var csProjReferenceRelativePath = ExtractCsProjReferenceRelativePath(match.Value)
                // Replacing slashes so Linux doesn't freak out
                .Replace("\\", "/");

            // Find where it currently is
            var csProjFilePath = FindCsProjFilePath(csProjFilePaths, csProjFileName);

            // Determine where it should be moved to
            var newCsProjFilePath = Path.Combine(rootPath, csProjReferenceRelativePath);

            // Move it there instead, creating dirs as necessary
            Directory.CreateDirectory(newCsProjFilePath.Replace(csProjFileName, "").TrimEnd('/'));
            Console.WriteLine($"Moving '{csProjFileName}' from '{csProjFilePath}' to '{newCsProjFilePath}'");
            // Far out docker... https://github.com/docker/for-win/issues/1051
            // File.Move(csProjFilePath, newCsProjFilePath);
            File.Copy(csProjFilePath, newCsProjFilePath);
            File.Delete(csProjFilePath);            

            // Mark that we've moved this one
            csProjFilesProcessed.Add(csProjFileName);

            // Process the contents of this file for any of its references
            ProcessCsProjFileContents(csProjFilesProcessed, csProjFilePaths, newCsProjFilePath);
        }

        private static void ProcessCsProjFileContents(HashSet<string> csProjFilesProcessed, string[] csProjFilePaths, string csProjFilePath)
        {
            var csProjFileDirectoryPath = Path.GetDirectoryName(csProjFilePath);
            var csProjFileContents = File.ReadAllText(csProjFilePath);
            var matches = CsProjRegex.Matches(csProjFileContents);
            foreach (Match match in matches)
            {
                ProcessCsProjFileMatch(match, csProjFileDirectoryPath, csProjFilesProcessed, csProjFilePaths);
            }
        }

        private static string ExtractCsProjReferenceRelativePath(string input)
        {
            var firstQuoteIndex = input.IndexOf('"');
            if (firstQuoteIndex > 0)
            {
                input = input.Substring(firstQuoteIndex + 1);
            }
            return input.TrimEnd('\"');
        }
    }
}
