using WorkDelta.Core.Models;
using WorkDelta.Core.Services;

namespace WorkDelta.Core.Tests;

public sealed class TrackingEngineTests
{
    [Fact]
    public async Task Initialize_MigratesExistingProjectToOfficeDocumentSnapshots()
    {
        var root = NewTemporaryDirectory();
        var projectFolder = Directory.CreateDirectory(Path.Combine(root, "project")).FullName;
        var document = Path.Combine(projectFolder, "proposal.docx");
        await File.WriteAllBytesAsync(document, [0x50, 0x4b, 0x00, 0x01]);

        var store = new WorkDeltaStore(Path.Combine(root, "data.db"));
        await store.InitializeAsync();
        var project = await store.AddProjectAsync(projectFolder);
        var pathPolicy = new PathPolicy();
        var snapshots = new GitSnapshotStore(Path.Combine(root, "repositories"), pathPolicy);
        var identity = new AppIdentity("user123456", "Test User", "device1", "Test Device");

        await using var engine = new TrackingEngine(store, snapshots, pathPolicy, identity);
        await engine.InitializeAsync();

        var snapshot = Assert.Single(await store.GetSnapshotsAsync(project.Id));
        var change = Assert.Single(await snapshots.GetSnapshotChangesAsync(project.Id, snapshot.CommitId));
        Assert.Equal("proposal.docx", change.Path);
        Assert.False(change.HasLineStatistics);
        Assert.Equal("office-documents-v1", await store.GetSettingValueAsync("snapshot_policy_version"));
    }

    [Fact]
    public async Task ReportFileDetails_IncludeNamesAndTextLineCounts()
    {
        var root = NewTemporaryDirectory();
        var projectFolder = Directory.CreateDirectory(Path.Combine(root, "project")).FullName;
        var sourceFile = Path.Combine(projectFolder, "notes.txt");
        var binaryFile = Path.Combine(projectFolder, "evidence.pdf");
        await File.WriteAllTextAsync(sourceFile, "first\nold");
        await File.WriteAllBytesAsync(binaryFile, [0x25, 0x50, 0x44, 0x46, 0x00, 0x01]);

        var store = new WorkDeltaStore(Path.Combine(root, "data.db"));
        await store.InitializeAsync();
        var project = await store.AddProjectAsync(projectFolder);
        var identity = new AppIdentity("user123456", "Test User", "device1", "Test Device");
        var pathPolicy = new PathPolicy();
        var snapshots = new GitSnapshotStore(Path.Combine(root, "repositories"), pathPolicy);
        var baseline = await snapshots.CreateBaselineAsync(project, identity);
        await store.AddSnapshotAsync(project, baseline, "baseline");

        await File.WriteAllTextAsync(sourceFile, "first\nnew\nthird");
        await File.WriteAllBytesAsync(binaryFile, [0x25, 0x50, 0x44, 0x46, 0x00, 0x02, 0x03]);
        var changedAt = DateTimeOffset.UtcNow;
        await store.RecordChangesAsync(
            project,
            [
                new FileChange(sourceFile, "notes.txt", FileChangeKind.Modified),
                new FileChange(binaryFile, "evidence.pdf", FileChangeKind.Modified)
            ],
            changedAt);
        var snapshot = await snapshots.CreateSnapshotAsync(
            project,
            [
                new FileChange(sourceFile, "notes.txt", FileChangeKind.Modified),
                new FileChange(binaryFile, "evidence.pdf", FileChangeKind.Modified)
            ],
            identity,
            "automatic");
        await store.AddSnapshotAsync(project, snapshot, "automatic");

        await using var engine = new TrackingEngine(store, snapshots, pathPolicy, identity);
        var localDate = DateOnly.FromDateTime(changedAt.ToLocalTime().DateTime);
        var details = await engine.GetReportFileDetailsAsync(
            project,
            ["notes.txt", "evidence.pdf", "documents.zip"],
            localDate,
            localDate,
            TimeZoneInfo.Local);

        var textFile = Assert.Single(details, item => item.Path == "notes.txt");
        Assert.True(textFile.HasLineStatistics);
        Assert.Equal(2, textFile.AddedLines);
        Assert.Equal(1, textFile.DeletedLines);
        Assert.True(textFile.IsBackedUp);

        var binaryDetail = Assert.Single(details, item => item.Path == "evidence.pdf");
        Assert.False(binaryDetail.HasLineStatistics);
        Assert.True(binaryDetail.IsBackedUp);
        Assert.Equal(6, binaryDetail.OldSize);
        Assert.Equal(7, binaryDetail.NewSize);

        var archiveDetail = Assert.Single(details, item => item.Path == "documents.zip");
        Assert.False(archiveDetail.HasLineStatistics);
        Assert.False(archiveDetail.IsBackedUp);
    }

    private static string NewTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "WorkDelta.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
