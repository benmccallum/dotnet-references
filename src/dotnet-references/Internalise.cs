using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using static BenMcCallum.DotNet.References.Common;

namespace BenMcCallum.DotNet.References
{
    public static class Internalise
    {
        public static int Run(string workingDirectory, bool removeEmptyItemGroups)
        {
            if (string.IsNullOrWhiteSpace(workingDirectory))
            {
                return WriteError(ErrorCode.WorkingDirectoryArgInvalid);
            }

            var csProjFilePaths = GetCsProjFilePaths(workingDirectory);

            ProcessProjectFiles(csProjFilePaths, removeEmptyItemGroups);

            ProcessSolutionFiles(workingDirectory, csProjFilePaths);

            Console.WriteLine("Done.");
            return 0;
        }

        private static void ProcessProjectFiles(string[] csProjFilePaths, bool removeEmptyItemGroups)
        {
            var csProjFilePathsByName = csProjFilePaths
                .ToDictionary(path => Path.GetFileNameWithoutExtension(path));

            foreach (var csProjFilePath in csProjFilePathsByName.Values)
            {
                var xml = File.ReadAllText(csProjFilePath);

                var xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(xml);

                var projectElement = xmlDocument.GetElementsByTagName("Project")[0]!;
                var xmlns = projectElement.NamespaceURI;

                var packageReferenceElements = xmlDocument.GetElementsByTagName("PackageReference");
                if (packageReferenceElements.Count < 1)
                {
                    continue;
                }

                // Find or create an ItemGroup element to add project references to
                //  - First, by looking for one used by existing project references,
                //  - Else, by creating one (will be appended to document later)
                XmlNode itemGroupElement = null;
                var projectReferenceElements = xmlDocument.GetElementsByTagName("ProjectReference");
                if (projectReferenceElements.Count > 0)
                {
                    itemGroupElement = projectReferenceElements[0]!.ParentNode;
                }
                itemGroupElement ??= xmlDocument.CreateElement("ItemGroup", xmlns);

                // Convert package references to internal project references if 
                // a package name matches a project name in the working directory
                for (var i = packageReferenceElements.Count - 1; i >= 0; i--)
                {
                    var packageReferenceElement = packageReferenceElements[i]!;

                    var packageName = packageReferenceElement.Attributes?["Include"]?.Value
                        ?? throw new InvalidOperationException("Missing Include attribute");
                    if (!csProjFilePathsByName.ContainsKey(packageName))
                    {
                        continue;
                    }

                    // Find it's relative path to current proj file being modified
                    var newReferenceCsProjFilePath = csProjFilePathsByName[packageName];
                    var newReferenceCsProjFileRelativePath = GetRelativePathTo(csProjFilePath, newReferenceCsProjFilePath);

                    // Create reference
                    var newProjectReferenceElement = xmlDocument.CreateElement("ProjectReference", xmlns);
                    newProjectReferenceElement.SetAttribute("Include", newReferenceCsProjFileRelativePath);
                    itemGroupElement.AppendChild(newProjectReferenceElement);

                    // TODO: Future enhancement... 
                    // Recurse package's dependencies, as transitive ones are needed too

                    // Delete old reference
                    packageReferenceElement.ParentNode!.RemoveChild(packageReferenceElement);
                }

                // If we had to create the project references under a new ItemGroup element, 
                // it can now be added to the document, directly after the group containing the package references.
                if (itemGroupElement.ChildNodes.Count > 0 && itemGroupElement.ParentNode == null)
                {
                    projectElement.InsertAfter(itemGroupElement, packageReferenceElements[0]!.ParentNode);
                }

                // Clean out empty ItemGroup elements
                if (removeEmptyItemGroups)
                {
                    RemoveEmptyItemGroupElements(xmlDocument);
                }

                xmlDocument.Save(csProjFilePath);
            }
        }

        private static void RemoveEmptyItemGroupElements(XmlDocument xmlDocument)
        {
            var itemGroupElements = xmlDocument.GetElementsByTagName("ItemGroup");
            for (var i = itemGroupElements.Count - 1; i >= 0; i--)
            {
                var itemGroupEle = itemGroupElements[i]!;
                if (itemGroupEle.Attributes?.Count == 0 && itemGroupEle.ChildNodes.Count == 0)
                {
                    itemGroupEle.ParentNode!.RemoveChild(itemGroupEle);
                }
            }
        }

        private static void ProcessSolutionFiles(string workingDirectory, string[] csProjFilePaths)
        {
            // Loop over all .sln files
            var slnFilePaths = Directory.GetFiles(workingDirectory, "*.sln", SearchOption.AllDirectories);
            foreach (var slnFilePath in slnFilePaths)
            {
                var slnFileContents = File.ReadAllText(slnFilePath);

                // Recursively loop over all referenced .csproj files to get a 
                // list of all .csproj files that should be referenced by the .sln file
                var csProjFilePathsAlreadyInSlnFile = new HashSet<string>();
                var csProjFilePathDependencyTree = new HashSet<string>();
                var matches = SlnFileCsProjRegex.Matches(slnFileContents);
                foreach (Match match in matches)
                {
                    var csProjFileName = ExtractCsProjName(match.Value);
                    var csProjFilePath = FindCsProjFilePath(csProjFilePaths, csProjFileName);

                    csProjFilePathsAlreadyInSlnFile.Add(csProjFilePath);

                    RecurseProjFiles(csProjFilePath, csProjFilePaths, csProjFilePathDependencyTree);
                }

                var missingCsProjPaths = csProjFilePathDependencyTree
                    .Except(csProjFilePathsAlreadyInSlnFile)
                    .ToArray();

                if (missingCsProjPaths.Any())
                {
                    Console.WriteLine($"Solution {slnFilePath} is missing {missingCsProjPaths.Length} reference/s. Adding them...");
                    var slnLevelGuid = Guid.NewGuid();

                    var sb = new StringBuilder(200 * missingCsProjPaths.Length);
                    foreach (var missingCsProjPath in missingCsProjPaths)
                    {
                        var relativeCsProjPath = GetRelativePathTo(slnFilePath, missingCsProjPath);
                        var projName = Path.GetFileNameWithoutExtension(missingCsProjPath);
                        sb.AppendLine($"Project(\"{slnLevelGuid}\") = \"{projName}\", \"{relativeCsProjPath}\", \"{Guid.NewGuid()}\"");
                        sb.AppendLine("EndProject");
                    }

                    var indexToInsert = slnFileContents.IndexOf("Global\r\n");
                    slnFileContents = slnFileContents.Insert(indexToInsert, sb.ToString());

                    File.WriteAllText(slnFilePath, slnFileContents);
                    Console.WriteLine($"Added references to {slnFilePath}");
                }
                else
                {
                    Console.WriteLine($"No changes required in {slnFilePath}");
                }
            }
            
            
            // If a reference is not in the .sln, add it in
        }

        private static void RecurseProjFiles(string csProjFilePath, string[] csProjFilePaths, HashSet<string> csProjFilePathDependencyTree)
        {
            var csProjFileContents = File.ReadAllText(csProjFilePath);
            var matches = CsProjRegex.Matches(csProjFileContents);
            foreach (Match match in matches)
            {
                var csProjDepFileName = ExtractCsProjName(match.Value);
                var csProjDepFilePath = FindCsProjFilePath(csProjFilePaths, csProjDepFileName);

                if (!csProjFilePathDependencyTree.Contains(csProjDepFilePath))
                {
                    csProjFilePathDependencyTree.Add(csProjDepFilePath);
                    RecurseProjFiles(csProjDepFilePath, csProjFilePaths, csProjFilePathDependencyTree);
                }
            }
        }
    }
}
