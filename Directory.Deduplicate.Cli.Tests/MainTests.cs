using Xunit.Abstractions;

namespace Directory.Deduplicate.Cli.Tests;

public sealed class MainTests : IDisposable
{
    private readonly DirectoryInfo _temporaryDirectory;
    private readonly ITestOutputHelper _output;

    public MainTests(ITestOutputHelper output)
    {
        _temporaryDirectory = System.IO.Directory.CreateTempSubdirectory();
        _output = output;

        _output.WriteLine($"Temporary directory: {_temporaryDirectory.FullName}");
    }

    [Fact]
    public async Task Main_WithNonExistingDirectoryPath_Fails()
    {
        var exitCode = await Program.Main(
        [
            Path.Combine(_temporaryDirectory.FullName, Path.GetRandomFileName())
        ]);

        exitCode.Should().NotBe(0);
    }

    [Fact]
    public async Task Main_WithTwoUniqueSameSizeFiles_RemovesNothing()
    {
        var fileA = await CreateFileWithRandomContents(seed: 1, size: 512);
        var fileB = await CreateFileWithRandomContents(seed: 2, size: 512);

        var exitCode = await Program.Main(
        [
            _temporaryDirectory.FullName,
            "--force",
            "--sort-by", "ModifiedTime",
            "--sort-direction", "Ascending"
        ]);

        exitCode.Should().Be(0);
        fileA.Exists.Should().BeTrue();
        fileB.Exists.Should().BeTrue();
    }

    [Fact]
    public async Task Main_WithTwoDifferentSizeFiles_RemovesNothing()
    {
        var fileA = await CreateFileWithRandomContents(seed: 1, size: 512);
        var fileB = await CreateFileWithRandomContents(seed: 1, size: 2 * 1024 * 1024);

        var exitCode = await Program.Main(
        [
            _temporaryDirectory.FullName,
            "--force",
            "--sort-by", "ModifiedTime",
            "--sort-direction", "Ascending"
        ]);

        exitCode.Should().Be(0);
        fileA.Exists.Should().BeTrue();
        fileB.Exists.Should().BeTrue();
    }

    [Fact]
    public async Task Main_WithTwoDuplicateFiles_RemovesOne()
    {
        var fileA = await CreateFileWithRandomContents(seed: 1, size: 512);

        // Make sure the file creation timestamp differs
        await Task.Delay(TimeSpan.FromSeconds(1));

        var fileB = await CreateFileWithRandomContents(seed: 1, size: 512);

        var exitCode = await Program.Main(
        [
            _temporaryDirectory.FullName,
            "--force",
            "--sort-by", "ModifiedTime",
            "--sort-direction", "Ascending"
        ]);

        exitCode.Should().Be(0);
        fileA.Exists.Should().BeTrue();
        fileB.Exists.Should().BeFalse();
    }

    [Fact]
    public async Task Main_WithTwoLargeDuplicateFilesWithoutForce_RemovesNothing()
    {
        var fileA = await CreateFileWithRandomContents(seed: 1, size: 2 * 1024 * 1024);
        var fileB = await CreateFileWithRandomContents(seed: 1, size: 2 * 1024 * 1024);

        var exitCode = await Program.Main(
        [
            _temporaryDirectory.FullName,
            "--sort-by", "ModifiedTime",
            "--sort-direction", "Ascending"
        ]);

        exitCode.Should().Be(0);
        fileA.Exists.Should().BeTrue();
        fileB.Exists.Should().BeTrue();
    }

    [Fact]
    public async Task Main_WithMultipleDuplicateFilesInSubdirectories_RemovesDuplicates()
    {
        var fileA = await CreateFileWithRandomContents(seed: 1, size: 512);
        var fileB = await CreateFileWithRandomContents(seed: 1, size: 512);
        var fileC = await CreateFileWithRandomContents(seed: 1, size: 512);

        var subDirectory1 = System.IO.Directory.CreateDirectory(Path.Combine(
            _temporaryDirectory.FullName,
            "sub1",
            "sub1-2"));

        var subDirectory2 = System.IO.Directory.CreateDirectory(Path.Combine(
            _temporaryDirectory.FullName,
            "sub2"));

        fileA.MoveTo(Path.Combine(subDirectory1.FullName, "fileA"));
        fileB.MoveTo(Path.Combine(_temporaryDirectory.FullName, "fileB"));
        fileC.MoveTo(Path.Combine(subDirectory2.FullName, "fileC"));

        var exitCode = await Program.Main(
        [
            _temporaryDirectory.FullName,
            "--force",
            "--recursive",
            "--sort-by", "Name",
            "--sort-direction", "Descending"
        ]);

        exitCode.Should().Be(0);
        fileA.Exists.Should().BeFalse();
        fileB.Exists.Should().BeFalse();
        fileC.Exists.Should().BeTrue();
    }

    private async Task<FileInfo> CreateFileWithRandomContents(int seed, int size)
    {
        var data = new byte[size];

        var random = new Random(seed);
        random.NextBytes(data);

        var file = new FileInfo(Path.Combine(_temporaryDirectory.FullName, Path.GetRandomFileName()));
        await using var fileStream = file.Create();
        await fileStream.WriteAsync(data);

        return file;
    }

    public void Dispose()
    {
        _temporaryDirectory.Delete(recursive: true);
    }
}
