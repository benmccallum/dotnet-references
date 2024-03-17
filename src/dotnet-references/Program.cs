using McMaster.Extensions.CommandLineUtils;
using System;
using System.ComponentModel.DataAnnotations;
using static BenMcCallum.DotNet.References.Common;

namespace BenMcCallum.DotNet.References
{
    public class Program
    {
        public static int Main(string[] args) => CommandLineApplication.Execute<Program>(args);

        [Argument(0, Description = "The mode to run (fix or internalise)")]
        [Required]
        public Mode Mode { get; set; }

        [Option("-ep|--entry-point", Description = "The entry point to use (a .sln file or a directory)")]
        public string EntryPoint { get; set; }

        [Option("-wd|--working-directory", Description = "The working directory to use")]
        public string WorkingDirectory { get; set; }

        [Option("-rupf|--remove-unreferenced-project-files", Description = "Should unreferenced project files be removed?")]
        public bool RemoveUnreferencedProjectFiles { get; set; }

        [Option("-reig|--remove-empty-item-groups", Description = "Should ItemGroup elements in project files that are empty be removed?")]
        public bool RemoveEmptyItemGroups { get; set; }

        public int OnExecute()
        {
            try
            { 
                if (Mode == Mode.Fix)
                {
                    return Fix.Run(EntryPoint, WorkingDirectory, RemoveUnreferencedProjectFiles);
                }
                else if (Mode == Mode.Internalise)
                {
                    return Internalise.Run(WorkingDirectory, RemoveEmptyItemGroups);
                }
                else if (Mode == Mode.Centralise)
                {
                    return Centralise.Run(EntryPoint, WorkingDirectory);
                }
                return WriteError(ErrorCode.ModeArgInvalid);
            }            
            catch (Exception ex)
            {
                WriteException("Unexpected error!", ex);
                return WriteError(ErrorCode.Unknown);
            }
        }
    }
}
