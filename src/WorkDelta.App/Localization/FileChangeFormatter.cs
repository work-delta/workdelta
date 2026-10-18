using WorkDelta.Core.Models;

namespace WorkDelta.App.Localization;

internal static class FileChangeFormatter
{
    public static string ForReport(ReportFileDetail file)
    {
        if (file.HasLineStatistics)
        {
            return $"+{file.AddedLines} / -{file.DeletedLines}";
        }

        return file.IsBackedUp
            ? Localizer.Format("BinaryBackedUp", FormatSize(file.OldSize), FormatSize(file.NewSize))
            : Localizer.Get("ActivityOnlyNotBackedUp");
    }

    public static string ForHistory(SnapshotFileChange change)
    {
        if (change.HasLineStatistics)
        {
            return $"{change.Status} · +{change.AddedLines}  -{change.DeletedLines}";
        }

        return $"{change.Status} · {Localizer.Format("BinaryBackedUp", FormatSize(change.OldSize), FormatSize(change.NewSize))}";
    }

    private static string FormatSize(long? size)
    {
        if (size is null)
        {
            return "—";
        }

        string[] units = ["B", "KB", "MB", "GB"];
        var value = (double)size.Value;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return unit == 0 ? $"{value:0} {units[unit]}" : $"{value:0.##} {units[unit]}";
    }
}
