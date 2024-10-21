using System.Buffers;
using System.ComponentModel;

namespace Directory.Deduplicate.Cli;

public static class Deduplicator
{
    public static async Task Run(
        DirectoryInfo directory,
        bool force,
        bool recursive,
        FileSortProperty sortProperty,
        ListSortDirection sortDirection,
        bool verbose
    )
    {
        var files = directory.EnumerateFiles("*", new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = recursive,
        })
        .If(sortProperty == FileSortProperty.ModifiedTime, x => x.OrderBy(f => f.LastWriteTimeUtc))
        .If(sortProperty == FileSortProperty.Name, x => x.OrderBy(f => f.Name))
        .If(sortDirection == ListSortDirection.Descending, x => x.Reverse());

        var filesBySize = new Dictionary<long, List<FileInfo>>();
        var duplicateCount = 0;
        var duplicateTotalSize = 0L;

        foreach (var file in files)
        {
            var sameSizeFiles = filesBySize.GetValueOrDefault(file.Length, []);

            var originalFile = await FindFileByContent(sameSizeFiles, file);

            if (originalFile is not null)
            {
                duplicateCount++;
                duplicateTotalSize += file.Length;

                LogDuplicate(directory, originalFile, file, verbose);

                if (force)
                {
                    file.Delete();
                }
            }
            else
            {
                sameSizeFiles.Add(file);
                filesBySize[file.Length] = sameSizeFiles;
            }
        }

        if (duplicateCount == 0)
        {
            Console.WriteLine("No duplicate files were found.");
        }
        else
        {
            Console.WriteLine($"Found {duplicateCount} duplicate(s) occupying {GetHumanReadableSize(duplicateTotalSize)}.");
        }

        if (duplicateCount > 0 && !force)
        {
            Console.WriteLine("No files were deleted. Use the --force option to remove duplicates.");
        }
    }

    private static async Task<FileInfo?> FindFileByContent(List<FileInfo> sameSizeFiles, FileInfo targetFile)
    {
        foreach (var originalFile in sameSizeFiles)
        {
            if (await AreFilesEqual(originalFile, targetFile))
            {
                return originalFile;
            }
        }

        return null;
    }

    public static async Task<bool> AreFilesEqual(FileInfo fileInfo1, FileInfo fileInfo2)
    {
        const int bufferSize = 1024 * 1024; // 1 MiB

        var sharedArrayPool = ArrayPool<byte>.Shared;
        var buffer1 = sharedArrayPool.Rent(bufferSize);
        var buffer2 = sharedArrayPool.Rent(bufferSize);
        Array.Fill<byte>(buffer1, 0);
        Array.Fill<byte>(buffer2, 0);

        await using var fileStream1 = fileInfo1.OpenRead();
        await using var fileStream2 = fileInfo2.OpenRead();

        try
        {
            int lastBytesRead = 0;
            int offset = 0;

            do
            {
                lastBytesRead = await ReadFileBytes(buffer1, fileStream1, offset);
                await ReadFileBytes(buffer2, fileStream2, offset);

                offset += lastBytesRead;

                for (int i = 0; i < lastBytesRead; ++i)
                {
                    if (buffer1[i] != buffer2[i])
                    {
                        return false;
                    }
                }
            }
            while (offset < fileInfo1.Length);

            return true;
        }
        finally
        {
            sharedArrayPool.Return(buffer1);
            sharedArrayPool.Return(buffer2);
        }
    }

    private static async Task<int> ReadFileBytes(byte[] buffer, FileStream fileStream, int offset)
    {
        int totalBytesRead = 0;
        int newBytesRead;

        do
        {
            newBytesRead = await fileStream.ReadAsync(
                buffer.AsMemory(offset + totalBytesRead, buffer.Length - totalBytesRead));

            totalBytesRead += newBytesRead;
        }
        while (totalBytesRead < buffer.Length && newBytesRead != 0);

        return totalBytesRead;
    }

    private static void LogDuplicate(DirectoryInfo directory, FileInfo originalFile, FileInfo currentFile, bool verbose)
    {
        var originalFileString = GetFileDescription(directory, originalFile, verbose);
        var currentFileString = GetFileDescription(directory, currentFile, verbose);

        Console.WriteLine($"= {currentFileString} is a duplicate of {originalFileString}");
    }

    private static string GetFileDescription(DirectoryInfo directory, FileInfo file, bool verbose)
    {
        var shortFilePath = GetRelativeFilePath(directory, file);

        return verbose
            ? $"{shortFilePath} (created at {file.CreationTimeUtc:O}, last modified at {file.LastWriteTimeUtc:O})"
            : shortFilePath;
    }

    private static string GetRelativeFilePath(DirectoryInfo directory, FileInfo file)
    {
        var directoryPath = directory.FullName;
        var filePath = file.FullName;

        var result = filePath.Replace(directoryPath, string.Empty);

        if (result.StartsWith(Path.DirectorySeparatorChar))
            result = result[1..];

        return result;
    }

    private static string GetHumanReadableSize(long bytes)
    {
        const int unitMultiplier = 1000;
        string[] units =
        [
            "B",
            "KB",
            "MB",
            "GB",
            "TB",
        ];

        var currentUnitIndex = 0;
        var currentUnitValue = (double)bytes;

        while (currentUnitIndex + 1 < units.Length && currentUnitValue >= unitMultiplier)
        {
            currentUnitIndex++;
            currentUnitValue /= unitMultiplier;
        }

        return $"{Math.Round(currentUnitValue, 2)} {units[currentUnitIndex]}";
    }
}
