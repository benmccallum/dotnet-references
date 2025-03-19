using System.Text.Json;
using System.Text.RegularExpressions;
using static BenMcCallum.DotNet.References.Common;

namespace BenMcCallum.DotNet.References;

public static class FixLocationsOfProjectsProcessor
{
    public static void Process(string entryPoint, string workingDirectory, bool removeExtras)
    {
        Console.WriteLine("Starting process with the following args:");
        Console.WriteLine($"Entry point: {entryPoint}");
        Console.WriteLine($"Current Working Directory: {workingDirectory}");
        Console.WriteLine($"Remove Extras: {removeExtras}");

        var entryPointPath = Path.Combine(workingDirectory, entryPoint);
        var csProjFilePaths = GetCsProjFilePaths(workingDirectory);
        //Console.WriteLine($"Found {csProjFilePaths.Length} .csproj files in {currentWorkingDirectory}.");

        // The files we currently have at hand in the working directory, and which may need to be moved
        var csProjFilesProcessed = new HashSet<string>();
        
        if (entryPoint.EndsWith(".sln"))
        {
            var slnFileDirectoryPath = Path.GetDirectoryName(entryPointPath)!;
            var slnFileContents = File.ReadAllText(entryPointPath);
            var matches = SlnFileCsProjRegex.Matches(slnFileContents);
            foreach (Match match in matches)
            {
                ProcessCsProjFileMatch(match, slnFileDirectoryPath, csProjFilesProcessed, csProjFilePaths);
            }
        }
        else if (entryPoint.EndsWith(".slnf"))
        {
            using var stream = File.OpenRead(entryPointPath);
            var solutionFilterFile = JsonSerializer.Deserialize<SolutionFilterFile>(stream)!;
            var slnFileDirectoryPath = Path.GetDirectoryName(Path.Combine(Path.GetDirectoryName(entryPointPath)!, solutionFilterFile.Solution.Path))!;
            ProcessProjectsInSlnFilterFile(solutionFilterFile.Solution.Projects, slnFileDirectoryPath, csProjFilePaths, csProjFilesProcessed);
        }
        // TODO: Support slnx, bringing in 
        // Microsoft.VisualStudio.SolutionPersistence.Serializer;
        else
        {
            throw new InvalidOperationException("The entry point must be a .sln or .slnf file.");
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

        var csProjReferenceRelativePath = ExtractCsProjReferenceRelativePath(match.Value);
        
        var finalCsProjFilePath = ProcessCsProjFileMove(rootPath, csProjFilesProcessed, csProjFilePaths, csProjFileName, csProjReferenceRelativePath);

        // Process the contents of this file for any of its references
        ProcessCsProjFileContents(csProjFilesProcessed, csProjFilePaths, finalCsProjFilePath);
    }

    private static string ProcessCsProjFileMove(string rootPath, HashSet<string> csProjFilesProcessed, string[] csProjFilePaths, string csProjFileName, string csProjRelativePath)
    {
        Console.WriteLine("Processing project: " + csProjFileName);

        // Find where it currently is
        var csProjFilePath = FindCsProjFilePath(csProjFilePaths, csProjFileName);

        // Determine where it should be moved to
        var finalCsProjFilePath = Path.Combine(rootPath, csProjRelativePath);

        // Move it there instead, creating dirs as necessary
        Directory.CreateDirectory(finalCsProjFilePath.Replace(csProjFileName, "").TrimEnd('/'));
        Console.WriteLine($"Moving '{csProjFileName}' from '{csProjFilePath}' to '{finalCsProjFilePath}'");
        // Far out docker... https://github.com/docker/for-win/issues/1051
        // File.Move(csProjFilePath, newCsProjFilePath);
        File.Copy(csProjFilePath, finalCsProjFilePath);
        File.Delete(csProjFilePath);

        // Mark that we've moved this one
        csProjFilesProcessed.Add(csProjFileName);
        return finalCsProjFilePath;
    }

    private static void ProcessCsProjFileContents(HashSet<string> csProjFilesProcessed, string[] csProjFilePaths, string csProjFilePath)
    {
        var csProjFileDirectoryPath = Path.GetDirectoryName(csProjFilePath)!;
        var csProjFileContents = File.ReadAllText(csProjFilePath);
        var matches = CsProjRegex.Matches(csProjFileContents);
        foreach (Match match in matches)
        {
            ProcessCsProjFileMatch(match, csProjFileDirectoryPath, csProjFilesProcessed, csProjFilePaths);
        }
    }

    private static void ProcessProjectsInSlnFilterFile(string[] projectPaths, string rootPath, string[] csProjFilePaths, HashSet<string> csProjFilesProcessed)
    {
        foreach (var csProjRelativePath in projectPaths)
        {
            var csProjFileName = Path.GetFileName(ExtractCsProjReferenceRelativePath(csProjRelativePath));

            ProcessCsProjFileMove(rootPath, csProjFilesProcessed, csProjFilePaths, csProjFileName, csProjRelativePath);

            // Note: The assumption is that all files to be moved are in the slnf file,
            // and that crawling the tree of this csproj file's references isn't needed
        }
    }

    private static string ExtractCsProjReferenceRelativePath(string input)
    {
        var firstQuoteIndex = input.IndexOf('"');
        if (firstQuoteIndex > 0)
        {
            input = input.Substring(firstQuoteIndex + 1);
        }
        return input
            // Replacing slashes so Linux doesn't freak out
            .Replace("\\", "/")
            .TrimEnd('\"');
    }
}
