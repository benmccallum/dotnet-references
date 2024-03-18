#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
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
        var slnFileDirectoryPath = Path.GetDirectoryName(slnFilePath)!;

        var csProjFilePaths = GetCsProjFilePaths(workingDirectory);

        // TODO: Also look for .props files that might be adding packages

        // For all .csproj files that are referenced by the .sln file,
        // pull out nuget package references (name, versions)
        // { 
        //   "SomePackage": {
        //     "1.2.3": [ element1, element2 ],
        //     "1.2.4": []
        //   },
        //   /...
        // 
        var packageReferences = new Dictionary<string, Dictionary<string, List<PackageReference>>>();

        var matches = SlnFileCsProjRegex.Matches(slnFileContents);
        foreach (Match match in matches)
        {
            var csProjFileName = ExtractCsProjName(match.Value);
            var csProjFilePath = FindCsProjFilePath(csProjFilePaths, csProjFileName);

            // TODO: Explore Microsoft.Build.Evaluation instead
            // which might make modifying the project and saving it as before easier

            XDocument xmlDoc = XDocument.Load(csProjFilePath);
            foreach (var xPackageRef in xmlDoc.Descendants("PackageReference"))
            {
                var packageName = xPackageRef.Attribute("Include")?.Value
                    ?? xPackageRef.Descendants("Include").SingleOrDefault()?.Value
                    ?? throw new Exception($"Couldn't find package name in {csProjFilePath}");
                if (!packageReferences.TryGetValue(packageName, out var packageVersions))
                {
                    packageVersions = packageReferences[packageName] 
                        = new Dictionary<string, List<PackageReference>>();
                }

                var packageVersion = xPackageRef.Attribute("Version")?.Value
                    ?? xPackageRef.Descendants("Version").SingleOrDefault()?.Value
                    ?? throw new Exception(
                        $"Couldn't find package version for {packageName} in {csProjFilePath}");
                if (!packageVersions.TryGetValue(packageVersion, out var xElements))
                {
                    xElements = packageVersions[packageVersion]
                        = new List<PackageReference>();
                }

                xElements.Add(new(xPackageRef, csProjFilePath));
            }
        }

        var existingVersionVariables = new Dictionary<string, string>();
        if (slnFilePath.Contains("AutoGuru"))
        {
            var directoryBuildPropsFilePath = Path.Combine(slnFileDirectoryPath, "Directory.Build.props");
            var xDirectoryBuildProps = XDocument.Load(directoryBuildPropsFilePath);

            existingVersionVariables = xDirectoryBuildProps.Descendants()
                .Where(e =>
                    e.Parent is not null &&
                    e.Parent.Name.LocalName == "PropertyGroup" &&
                    e.Name.LocalName.EndsWith("Version"))
                .ToDictionary(
                    e => e.Name.LocalName,
                    e => e.Value);

            Console.WriteLine("Init'd existing version variables dictionary");
        }

        // Build up a Directory.Packages.props file
        // (or replace whatever one is there)
        // https://learn.microsoft.com/en-us/nuget/consume-packages/Central-Package-Management
        var xProject = new XElement("Project");
        var xDirectoryPackageProps = new XDocument(xProject);
        var xPropertyGroup = new XElement("PropertyGroup");
        xProject.Add(xPropertyGroup);
        xPropertyGroup.Add(new XElement("ManagePackageVersionsCentrally", "true"));
        var xItemGroup = new XElement("ItemGroup");
        xProject.Add(xItemGroup);

        var modifiedPackageReferences = new HashSet<PackageReference>();

        // Output helpful XML for those that can be immediately centralised
        var singleVersionedPackages = packageReferences
            .Where(pr => pr.Value.Count == 1)
            .OrderBy(pr => pr.Key)
            .ToArray();

        foreach (var singleVersionedPackage in singleVersionedPackages)
        {
            var versionWithElementsKvp = singleVersionedPackage.Value.Single();
            var packageName = singleVersionedPackage.Key;
            var packageVersion = versionWithElementsKvp.Key;

            //if (packageVersion.StartsWith("$"))
            //{
            //    var versionVariableName = packageVersion.Substring(2, packageVersion.Length - 3);
            //    if (existingVersionVariables.TryGetValue(versionVariableName, out var actualVersion))
            //    {
            //        packageVersion = actualVersion;
            //    }
            //}

            var xPackageReference = new XElement("PackageVersion");
            xPackageReference.SetAttributeValue("Include", packageName);
            xPackageReference.SetAttributeValue("Version",  packageVersion);
            xItemGroup.Add(xPackageReference);

            foreach (var packageRef in versionWithElementsKvp.Value)
            {
                var versionAttr = packageRef.Element.Attribute("Version");
                if (versionAttr is not null)
                {
                    versionAttr.Remove();
                }
                else
                {
                    var versionChildElement = packageRef.Element
                        .Descendants("Version")
                        .SingleOrDefault();
                    if (versionChildElement is not null)
                    {
                        versionChildElement.Remove();
                    }
                }

                modifiedPackageReferences.Add(packageRef);
            }

            Console.WriteLine($"Centralised {packageName} @ {packageVersion}.");
        }

        var directoryPackagePropsFilePath = Path.Combine(
            slnFileDirectoryPath, 
            "Directory.Packages.props");
        Console.WriteLine($"Saving {directoryPackagePropsFilePath}.");
        xProject.Document!.Save(directoryPackagePropsFilePath);

        foreach (var modifiedPackageRef in modifiedPackageReferences)
        {
            Console.WriteLine($"Saving changes to {modifiedPackageRef.CsProjFilePath}");

            modifiedPackageRef.Element.Document!.Save(
                modifiedPackageRef.CsProjFilePath);

            // Dodgy as, but preserves the formatting we like on our csproj files
            var sb = new StringBuilder();
            foreach (var line in File.ReadAllLines(modifiedPackageRef.CsProjFilePath).Skip(1))
            {
                if (line.StartsWith("<Project") ||
                    line.StartsWith("  </"))
                {
                    sb.AppendLine(line);
                    sb.AppendLine();
                }
                else
                {
                    sb.AppendLine(line);
                }
            }
            File.WriteAllText(modifiedPackageRef.CsProjFilePath, sb.ToString());
        }

        // Output helpful JSON for those that are trickier to centralise
        var multiVersionedPackages = packageReferences
            .Where(pr => pr.Value.Count != 1)
            .OrderBy(pr => pr.Key)
            .Select(kvp => new
            {
                Package = kvp.Key,
                Versions = kvp.Value.Keys.ToArray()
            })
            .ToArray();
        
        Console.WriteLine(
            $"{multiVersionedPackages.Length} packages have a non-consistent version. ");
        Console.WriteLine(JsonSerializer.Serialize(multiVersionedPackages, _serializerOptions));
        Console.WriteLine();
    }

    private record PackageReference(XElement Element, string CsProjFilePath);
}
