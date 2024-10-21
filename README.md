# Directory.Deduplicate

[![NuGet](https://img.shields.io/nuget/v/Directory.Deduplicate.Cli?logo=nuget&label=Directory.Deduplicate.Cli)](https://nuget.org/packages/Directory.Deduplicate.Cli)

A CLI tool to find and remove duplicate files inside a directory.

# Installation

1. [Download the latest release binary](https://github.com/f1x3d/Directory.Deduplicate/releases/latest)

2. Install as a global .NET tool:

   ```
   dotnet tool install --global Directory.Deduplicate.Cli
   ```

# Usage

```
Usage:
  dedupe <directory> [options]

Arguments:
  <directory>  Directory to scan for duplicates

Options:
  --sort-by <ModifiedTime|Name> (REQUIRED)
    Property to sort by when determining the file copy to keep

  --sort-direction <Ascending|Descending> (REQUIRED)
    Sort direction when determining the file copy to keep. The first file in  the sorted list will be kept

  --force
    Delete duplicate files. A dry run is performed without this option specified [default: False]

  -r, --recursive
    Scan sub-directories recursively [default: False]

  -v, --verbose
    Print file attributes on match [default: False]

  --version
    Show version information

  -?, -h, --help
    Show help and usage information
```

# Examples

* Find and delete all duplicates under the `photos` folder and its sub-folders, keeping the oldest file:

   ```
   dedupe photos --recursive --force --sort-by ModifiedTime --sort-direction Ascending
   ```
