using static BenMcCallum.DotNet.References.Common;

namespace BenMcCallum.DotNet.References;

public static class Fix
{
    public static int Run(string? entryPoint, string? workingDirectory, bool shouldRemoveUnreferencedProjectFiles)
    {
        if (string.IsNullOrWhiteSpace(entryPoint))
        {
            return WriteError(ErrorCode.EntryPointArgInvalid);
        }

        workingDirectory ??= Environment.CurrentDirectory;
        var entryPointPath = Path.Combine(workingDirectory, entryPoint);
        var fileAttrs = File.GetAttributes(entryPointPath);

        if (entryPoint.EndsWith(".sln") || entryPoint.EndsWith(".slnf"))
        {
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
