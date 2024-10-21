using System.CommandLine;
using System.ComponentModel;

namespace Directory.Deduplicate.Cli;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Find and remove duplicate files inside a directory")
            { Name = "dedupe" };

        var directory = new Argument<DirectoryInfo>(
            name: "directory",
            description: "Directory to scan for duplicates");

        directory.AddValidator(result =>
        {
            if (!result.GetValueForArgument(directory).Exists)
            {
                result.ErrorMessage = "Specified directory does not exist";
            }
        });

        var force = new Option<bool>(
            name: "--force",
            getDefaultValue: () => false,
            description: "Delete duplicate files. A dry run is performed without this option specified");

        var recursive = new Option<bool>(
            name: "--recursive",
            getDefaultValue: () => false,
            description: "Scan sub-directories recursively");

        recursive.AddAlias("-r");

        var sortProperty = new Option<FileSortProperty>(
            name: "--sort-by",
            description: "Property to sort by when determining the file copy to keep")
            { IsRequired = true };

        var sortDirection = new Option<ListSortDirection>(
            name: "--sort-direction",
            description: "Sort direction when determining the file copy to keep. The first file in the sorted list will be kept")
            { IsRequired = true };

        var verbose = new Option<bool>(
            name: "--verbose",
            getDefaultValue: () => false,
            description: "Print file attributes on match");

        verbose.AddAlias("-v");

        rootCommand.AddArgument(directory);

        rootCommand.AddOption(sortProperty);
        rootCommand.AddOption(sortDirection);
        rootCommand.AddOption(force);
        rootCommand.AddOption(recursive);
        rootCommand.AddOption(verbose);

        rootCommand.SetHandler(
            Deduplicator.Run,
            directory,
            force,
            recursive,
            sortProperty,
            sortDirection,
            verbose);

        return await rootCommand.InvokeAsync(args);
    }
}
