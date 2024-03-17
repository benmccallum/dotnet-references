using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using static BenMcCallum.DotNet.References.Common;

namespace BenMcCallum.DotNet.References;

public static class Centralise
{
    private static readonly JsonSerializerOptions _serializerOptions = 
        new JsonSerializerOptions(JsonSerializerDefaults.General)
        {
            WriteIndented = true
        };

    public static int Run(string entryPoint, string workingDirectory)
    {
        if (string.IsNullOrWhiteSpace(entryPoint))
        {
            return WriteError(ErrorCode.EntryPointArgInvalid);
        }

        if (entryPoint.EndsWith(".sln"))
        {
            workingDirectory ??= Environment.CurrentDirectory;
            
            var slnFilePath = Path.IsPathFullyQualified(entryPoint)
                ? entryPoint
                : Path.Combine(workingDirectory, entryPoint);

            ProcessSolutionFile(slnFilePath, workingDirectory);
        }
        else if (entryPoint.EndsWith(".csproj"))
        {
            throw new NotImplementedException(
                ".csproj entry points not supported yet, please request.");
        }
        else
        {
            return WriteError(ErrorCode.EntryPointArgInvalid);
        }

        Console.WriteLine("Done.");
        return 0;
    }

    private static void ProcessSolutionFile(string slnFilePath, string workingDirectory)
    {
        var slnFileContents = File.ReadAllText(slnFilePath);

        var csProjFilePaths = GetCsProjFilePaths(workingDirectory);

        // TODO: Also look for .props files that might be adding packages

        // For all .csproj files that are referenced by the .sln file,
        // pull out nuget package references (name, versions)
        var packageReferences = new Dictionary<string, HashSet<string>>();
        var matches = SlnFileCsProjRegex.Matches(slnFileContents);
        foreach (Match match in matches)
        {
            var csProjFileName = ExtractCsProjName(match.Value);
            var csProjFilePath = FindCsProjFilePath(csProjFilePaths, csProjFileName);

            var csProjFileContents = File.ReadAllText(csProjFilePath);

            var packageMatches = PackageReferenceRegex.Matches(csProjFileContents);
            foreach (Match packageMatch in packageMatches)
            {
                var packageName = packageMatch.Groups["name"].Value;
                var packageVersion = packageMatch.Groups["version"].Value;
                if (!packageReferences.TryGetValue(packageName, out var packageVersions))
                {
                    packageVersions = packageReferences[packageName] = new HashSet<string>();
                }
                packageVersions.Add(packageVersion);
            }
        }

        // Output helpful XML for those that can be immediately centralised
        var singleVersionedPackages = packageReferences
            .Where(pr => pr.Value.Count == 1)
            .OrderBy(pr => pr.Key)
            .ToArray();

        var sb = new StringBuilder(
            $"{singleVersionedPackages.Length} packages have a consistent version for central versioning." +
            $"The below block can be copied into your central Directory.Packages.props file per " +
            $"https://learn.microsoft.com/en-us/nuget/consume-packages/Central-Package-Management");
        foreach (var package in singleVersionedPackages) 
        {
            sb.AppendLine($"<PackageVersion Include=\"{package.Key}\" Version=\"{package.Value.Single()}\" />");
        }
        Console.WriteLine(sb.ToString());
        Console.WriteLine();

        // Output helpful JSON for those that are trickier to centralise
        var multiVersionedPackages = packageReferences
            .Where(pr => pr.Value.Count != 1)
            .OrderBy(pr => pr.Key)
            .ToArray();
        
        Console.WriteLine(
            $"{multiVersionedPackages.Length} packages have a non-consistent version. ");
        Console.WriteLine(JsonSerializer.Serialize(multiVersionedPackages, _serializerOptions));
        Console.WriteLine();
    }
}
