using WorkDelta.Core.Services;

namespace WorkDelta.Core.Tests;

public sealed class PathPolicyTests
{
    [Theory]
    [InlineData("node_modules/package/index.js")]
    [InlineData("src/bin/output.txt")]
    [InlineData(".git/config")]
    [InlineData("build/result.cs")]
    [InlineData("notes.tmp")]
    public void ShouldIgnore_KnownNoise_ReturnsTrue(string relativePath)
    {
        var root = Path.Combine(Path.GetTempPath(), "workdelta-policy");
        var fullPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(new PathPolicy().ShouldIgnore(root, fullPath));
    }

    [Theory]
    [InlineData("src/App.cs")]
    [InlineData("README.md")]
    [InlineData("docs/设计说明.txt")]
    public void ShouldIgnore_ProjectText_ReturnsFalse(string relativePath)
    {
        var root = Path.Combine(Path.GetTempPath(), "workdelta-policy");
        var fullPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.False(new PathPolicy().ShouldIgnore(root, fullPath));
    }

    [Fact]
    public void ShouldIgnore_CustomGlob_ReturnsTrue()
    {
        var root = Path.Combine(Path.GetTempPath(), "workdelta-policy-custom");
        var policy = new PathPolicy();
        policy.SetCustomRules(root, "generated/*\n*.secret");

        Assert.True(policy.ShouldIgnore(root, Path.Combine(root, "generated", "result.txt")));
        Assert.True(policy.ShouldIgnore(root, Path.Combine(root, "keys.secret")));
        Assert.False(policy.ShouldIgnore(root, Path.Combine(root, "notes.txt")));
    }

    [Fact]
    public void ShouldIgnoreActivity_Archive_ReturnsFalseWhileSnapshotIgnoresIt()
    {
        var root = Path.Combine(Path.GetTempPath(), "workdelta-policy-activity");
        var fullPath = Path.Combine(root, "documents.zip");

        var policy = new PathPolicy();

        Assert.False(policy.ShouldIgnoreActivity(root, fullPath));
        Assert.True(policy.ShouldIgnore(root, fullPath));
    }

    [Theory]
    [InlineData("contract.docx")]
    [InlineData("budget.xlsx")]
    [InlineData("briefing.pptx")]
    [InlineData("evidence.pdf")]
    [InlineData("scan.ofd")]
    [InlineData("draft.wps")]
    [InlineData("receipt.jpg")]
    public async Task IsTrackableFile_BusinessDocumentsAndScans_ReturnsTrue(string fileName)
    {
        var root = Directory.CreateDirectory(Path.Combine(
            Path.GetTempPath(),
            "WorkDelta.Tests",
            Guid.NewGuid().ToString("N"))).FullName;
        var fullPath = Path.Combine(root, fileName);
        await File.WriteAllBytesAsync(fullPath, [0x50, 0x4b, 0x00, 0x01]);

        var policy = new PathPolicy();

        Assert.False(policy.ShouldIgnore(root, fullPath));
        Assert.True(policy.IsBusinessDocument(fullPath));
        Assert.True(policy.IsTrackableFile(root, fullPath));
    }

    [Fact]
    public async Task IsTrackableFile_Archive_ReturnsFalse()
    {
        var root = Directory.CreateDirectory(Path.Combine(
            Path.GetTempPath(),
            "WorkDelta.Tests",
            Guid.NewGuid().ToString("N"))).FullName;
        var fullPath = Path.Combine(root, "documents.zip");
        await File.WriteAllBytesAsync(fullPath, [0x50, 0x4b, 0x00, 0x01]);

        Assert.False(new PathPolicy().IsTrackableFile(root, fullPath));
    }

    [Theory]
    [InlineData("notes.tmp")]
    [InlineData("debug.log")]
    [InlineData("bin/output.txt")]
    public void ShouldIgnoreActivity_KnownNoise_ReturnsTrue(string relativePath)
    {
        var root = Path.Combine(Path.GetTempPath(), "workdelta-policy-activity-noise");
        var fullPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));

        Assert.True(new PathPolicy().ShouldIgnoreActivity(root, fullPath));
    }
}
