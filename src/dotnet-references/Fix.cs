using System;
using System.IO;
using static BenMcCallum.DotNet.References.Common;

namespace BenMcCallum.DotNet.References
{
    public static class Fix
    {
        public static int Run(string entryPoint, string workingDirectory, bool shouldRemoveUnreferencedProjectFiles)
        {
            if (string.IsNullOrWhiteSpace(entryPoint))
            {
                return WriteError(ErrorCode.EntryPointArgInvalid);
            }

            var fileAttrs = File.GetAttributes(entryPoint);

            if (entryPoint.EndsWith(".sln"))
            {
                workingDirectory ??= Environment.CurrentDirectory;
                FixLocationsOfProjectsProcessor.Process(entryPoint, workingDirectory, shouldRemoveUnreferencedProjectFiles);
            }
            else if (fileAttrs == FileAttributes.Directory)
            {
                FixReferencesToProjectsProcessor.Process(entryPoint);
            }
            else
            {
                return WriteError(ErrorCode.EntryPointArgInvalid);
            }

            Console.WriteLine("Done.");
            return 0;
        }
    }
}
