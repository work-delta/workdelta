using WorkDelta.Core.Models;
using WorkDelta.Core.Services;

namespace WorkDelta.Core.Tests;

public sealed class TrackingEngineTests
{
    [Fact]
    public async Task ReportFileDetails_IncludeNamesAndTextLineCounts()
    {
        var root = NewTemporaryDirectory();
        var projectFolder = Directory.CreateDirectory(Path.Combine(root, "project")).FullName;
        var sourceFile = Path.Combine(projectFolder, "notes.txt");
        await File.WriteAllTextAsync(sourceFile, "first\nold");

        var store = new WorkDeltaStore(Path.Combine(root, "data.db"));
        await store.InitializeAsync();
        var project = await store.AddProjectAsync(projectFolder);
        var identity = new AppIdentity("user123456", "Test User", "device1", "Test Device");
        var pathPolicy = new PathPolicy();
        var snapshots = new GitSnapshotStore(Path.Combine(root, "repositories"), pathPolicy);
        var baseline = await snapshots.CreateBaselineAsync(project, identity);
        await store.AddSnapshotAsync(project, baseline, "baseline");

        await File.WriteAllTextAsync(sourceFile, "first\nnew\nthird");
        var changedAt = DateTimeOffset.UtcNow;
        await store.RecordChangesAsync(
            project,
            [new FileChange(sourceFile, "notes.txt", FileChangeKind.Modified)],
            changedAt);
        var snapshot = await snapshots.CreateSnapshotAsync(
            project,
            [new FileChange(sourceFile, "notes.txt", FileChangeKind.Modified)],
            identity,
            "automatic");
        await store.AddSnapshotAsync(project, snapshot, "automatic");

        await using var engine = new TrackingEngine(store, snapshots, pathPolicy, identity);
        var localDate = DateOnly.FromDateTime(changedAt.ToLocalTime().DateTime);
        var details = await engine.GetReportFileDetailsAsync(
            project,
            ["notes.txt", "evidence.pdf"],
            localDate,
            localDate,
            TimeZoneInfo.Local);

        var textFile = Assert.Single(details, item => item.Path == "notes.txt");
        Assert.True(textFile.HasLineStatistics);
        Assert.Equal(2, textFile.AddedLines);
        Assert.Equal(1, textFile.DeletedLines);

        var binaryFile = Assert.Single(details, item => item.Path == "evidence.pdf");
        Assert.False(binaryFile.HasLineStatistics);
    }

    private static string NewTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "WorkDelta.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
