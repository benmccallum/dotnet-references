# dotnet references

![Nuget](https://img.shields.io/nuget/dt/dotnet-references) <a href="https://www.buymeacoffee.com/benmccallum" target="_blank"><img src="https://bmc-cdn.nyc3.digitaloceanspaces.com/BMC-button-images/custom_images/orange_img.png" alt="Buy Me A Coffee" style="height: auto !important;width: auto !important;" ></a>

A [dotnet global tool](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools) 
that aids with bulk solution and project file references changes and related directory 
organisation.

(Formerly `dotnet-fix-references`, see prior documentation for it [here](docs/README-dotnet-fix-references.md).)

# Installation

```dotnet tool install --global dotnet-references```

> Note: If using this in a scripted process you want to be consistent (like a build), you should pin to a specific version with `--version x.y.z`.

# Usage

```
Usage: dotnet-references <Mode> [options]

Arguments:
  Mode                                       The mode to run (fix or internalise)

Options:
  -ep|--entry-point                          The entry point to use (a .sln file, .slnf file, or a directory)
  -wd|--working-directory                    The working directory to use.
                                             If not provided, defaults to directory of entry point.
  -rupf|--remove-unreferenced-project-files  Should unreferenced project files be removed?
                                             (Caution! Deletes files! Only supported in fix mode).
  -reig|--remove-empty-item-groups           Should ItemGroup elements in project files that are empty be removed?
                                             (Only supported in internalise mode).
  -?|-h|--help                               Show help information
```

Supports the following modes which have varying use cases.

## Mode 1: Fix
This mode can fix references between solution files and projects in one of two ways.

> :warning: If the dotnet cli complains with "Specify which project file to use because this '...' contains more than one project file.", you've run into a limitation of the dotnet cli. A workaround is to execute from any random directory and utilise the `-ep` and `-wd`  args. (eg. `mkdir temp && cd temp && dotnet references fix -ep ../ -wd .. -rupf` (or `-ep ../Company.Project.sln -wd ..`)).

### Directory as entry point 
By passing a directory as the entry, the tool will assume that the current directory structure is the source of truth and will fix all project references inside all .sln and .csproj files to the correct relative path.

> dotnet references fix -ep ./src

Use cases:
 You have moved your source code into a new folder structure (via a script or otherwise) and don't want to manually updates all your project references in .sln and .csproj files. (Project file names must be the same).

### Solution file as entry point
By passing a .sln file, the tool will assume that it is the source of truth; thus moving the .csproj files into the correct directory structure per their relative path in the sln file.

> dotnet references fix -ep Company.Project.sln

Use cases:
1. Overcoming Dockerfile COPY globbing limitations... See [here](docs/Dockerfile-use-case.md).

### Solution filter file as entry point
By passing a .slnf file, the tool will assume that it is the source of truth; thus moving the .csproj files into the correct directory structure per their relative path in the slnf file with respect to the location of its solution file.

> Note: You will need to have the sln and slnf files in their correct directory structure for this to work.

> dotnet references fix -ep Company.Project.sln

Use cases:
1. Overcoming Dockerfile COPY globbing limitations... See [here](docs/Dockerfile-use-case.md).

## Mode 2: Internalise (PackageReference --> ProjectReference)
This mode "internalises" references, by turning package references to project references.
(The project name must be the same as the package name).

> dotnet references internalise -wd ./src

Use cases:
1. You've consolidated separate projects (and packages) into a mono repo and want to swap all package references to local project references where possible.

Note: It currently doesn't handle transitive dependencies (dependencies of dependencies), which you'll need to add manually.

# Word of warning
This tool updates/deletes files in-place. Backup anything you care about before running this tool. 

# Feature backlog
1. Support fix mode w/ .slnx entry
    1. Support fix mode w/ .slnf entry file, but it pointing at a slnx file
1. Support internalise mode w/ .slnx file/s
1. Support fix mode w/ .csproj entry
