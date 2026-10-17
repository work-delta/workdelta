namespace WorkDelta.Core.Models;

public sealed record ProjectActivityReport(
    string ProjectId,
    string ProjectName,
    DateOnly StartDate,
    DateOnly EndDate,
    int SessionCount,
    int FileCount,
    int ChangeCount,
    TimeSpan ActiveDuration,
    IReadOnlyList<string> Files);

public sealed record ReportFileDetail(
    string Path,
    int AddedLines,
    int DeletedLines,
    bool HasLineStatistics);

public sealed record ProjectPreferences(string ProjectId, string IgnorePatterns);
