using System;
using System.IO;
using System.Linq;
using static BenMcCallum.DotNet.FixReferences.Common;

namespace BenMcCallum.DotNet.FixReferences
{
    class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                if (args == null || !args.Any())
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine("An entry point is required. See docs.");
                    Console.ResetColor();
                    return 1;
                }

                var arg0 = args[0];
                var fileAttrs = File.GetAttributes(arg0);

                if (arg0.EndsWith(".sln"))
                {
                    var cwd = args.Length > 1 ? args[1] : Environment.CurrentDirectory;
                    var removeExtras = args.Length > 2 ? bool.Parse(args[2]) : false;
                    FixLocationsOfProjectsProcessor.Process(arg0, cwd, removeExtras);
                }
                else if (fileAttrs == FileAttributes.Directory)
                {
                    FixReferencesToProjectsProcessor.Process(arg0);
                }
                else
                {
                    throw new ArgumentException("Provided argument wasn't valid.");
                }

                Console.WriteLine("Done.");
                return 0;
            }
            catch (Exception ex)
            {
                WriteError("Unexpected error!", ex);
                return 999;
            }
        }
    }
}
