using System.Globalization;
using ClosedXML.Excel;
using Nexus.Developer.Core.DevelopmentControl;

namespace Nexus.Developer.Infrastructure.DevelopmentControl;

// WI-07-0.2.3: typed cell codecs for the development-control workbook plus the
// enum<->text vocabularies, each verified against the live workbook by read-only probes:
//   - statuses in use:  In Progress | Planned | Completed | Implemented - Verify | Complete | Superseded
//   - node types:       Layer | Feature | Milestone | WorkItem | Task | Subtask
//   - actor types:      only "Agent" in the live Activity Log (22 of 49 rows have a blank
//                       Actor Type cell; see ParseActorType for the documented default)
//   - Effective From:   OADate serial (46259 = 2026-08-25)
//   - timestamps:       "yyyy-MM-dd'T'HH:mm:ss'Z'" (the workbook's own newest manual rows)
//   - list columns:     pipe/separator-delimited ("A | B")
//   - record versions:  version strings ("1.0", "v10.0"); append = integer part + 1,
//                       preserving the node's own "N.0" vs "vN.0" prefix convention
// Unknown vocabulary is surfaced as an exception rather than silently mapped (AF-010:
// the store must not invent semantics).
internal static class DevelopmentControlCellCodec
{
    public static string GetString(IXLCell cell)
    {
        if (cell.IsEmpty()) return "";
        return cell.GetString().Trim();
    }

    public static string? GetNullableString(IXLCell cell)
    {
        var value = GetString(cell);
        return value.Length == 0 ? null : value;
    }

    public static IReadOnlyList<string> GetList(IXLCell cell) => SplitList(GetString(cell));

    public static IReadOnlyList<string> SplitList(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return Array.Empty<string>();
        var result = new List<string>();
        foreach (var part in value.Split(new[] { '|', ';', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = part.Trim();
            if (trimmed.Length > 0) result.Add(trimmed);
        }
        return result;
    }

    public static string JoinList(IEnumerable<string> values) =>
        string.Join(" | ", values.Where(v => !string.IsNullOrWhiteSpace(v)));

    public static bool GetYesNo(IXLCell cell, bool defaultValue = false)
    {
        switch (GetString(cell).ToUpperInvariant())
        {
            case "YES":
            case "TRUE":
            case "Y":
                return true;
            case "NO":
            case "FALSE":
            case "N":
                return false;
            default:
                return defaultValue;
        }
    }

    public static int? GetInt(IXLCell cell)
    {
        var value = GetString(cell);
        if (value.Length == 0) return null;
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)) return i;
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)
            && d == Math.Floor(d) && d >= int.MinValue && d <= int.MaxValue) return (int)d;
        return null;
    }

    public static DateTimeOffset? GetDateTimeOffset(IXLCell cell)
    {
        if (cell.IsEmpty()) return null;
        if (cell.DataType == XLDataType.Number)
        {
            try { return new DateTimeOffset(DateTime.SpecifyKind(DateTime.FromOADate(cell.GetDouble()), DateTimeKind.Utc)); }
            catch { return null; }
        }
        if (cell.DataType == XLDataType.DateTime)
        {
            return new DateTimeOffset(cell.GetDateTime().ToUniversalTime());
        }
        var text = GetString(cell);
        if (text.Length == 0) return null;
        if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dto)) return dto;
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var serialText))
        {
            try { return new DateTimeOffset(DateTime.SpecifyKind(DateTime.FromOADate(serialText), DateTimeKind.Utc)); }
            catch { return null; }
        }
        return null;
    }

    public static TimeSpan? GetDuration(IXLCell cell)
    {
        var value = GetString(cell);
        if (value.Length == 0) return null;
        if (TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var span)) return span;
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)) return TimeSpan.FromSeconds(seconds);
        return null;
    }

    // The workbook's own newest manual rows write timestamps as "yyyy-MM-dd'T'HH:mm:ss'Z'".
    public static string FormatTimestamp(DateTimeOffset value) =>
        value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    public static double ToOADate(DateTimeOffset value) => value.ToUniversalTime().DateTime.ToOADate();

    public static void Write(IXLCell cell, string? value) => cell.SetValue(value ?? "");
    public static void Write(IXLCell cell, int? value)
    {
        if (value.HasValue) cell.SetValue(value.Value);
        else cell.SetValue("");
    }
    public static void Write(IXLCell cell, double value) => cell.SetValue(value);
    public static void WriteYesNo(IXLCell cell, bool value) => cell.SetValue(value ? "Yes" : "No");

    // --- record versions -------------------------------------------------------

    public static int ParseRecordVersion(string? version)
    {
        if (string.IsNullOrWhiteSpace(version)) return 0;
        var major = version.Trim();
        var dot = major.IndexOf('.');
        if (dot >= 0) major = major.Substring(0, dot);
        var digits = new string(major.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
    }

    public static string NextRecordVersion(string? current)
    {
        var next = ParseRecordVersion(current) + 1;
        var prefix = current is not null && current.TrimStart().StartsWith("v", StringComparison.OrdinalIgnoreCase) ? "v" : "";
        return prefix + next.ToString(CultureInfo.InvariantCulture) + ".0";
    }

    // --- node type / status / verdict / actor vocabularies --------------------

    public static string NodeTypeToText(NodeType nodeType) => nodeType switch
    {
        NodeType.Layer => "Layer",
        NodeType.Release => "Release",
        NodeType.Feature => "Feature",
        NodeType.Milestone => "Milestone",
        NodeType.WorkItem => "WorkItem",
        NodeType.Task => "Task",
        NodeType.Subtask => "Subtask",
        _ => throw new ArgumentOutOfRangeException(nameof(nodeType), nodeType, "Unrecognized NodeType."),
    };

    public static NodeType ParseNodeType(string value)
    {
        switch (value)
        {
            case "Layer": return NodeType.Layer;
            case "Release": return NodeType.Release;
            case "Feature": return NodeType.Feature;
            case "Milestone": return NodeType.Milestone;
            case "WorkItem": return NodeType.WorkItem;
            case "Task": return NodeType.Task;
            case "Subtask": return NodeType.Subtask;
            default: throw new InvalidOperationException($"Unrecognized node type '{value}' in the development-control workbook.");
        }
    }

    public static string StatusToText(Status status) => status switch
    {
        Status.Proposed => "Proposed",
        Status.Planned => "Planned",
        Status.Ready => "Ready",
        Status.InProgress => "In Progress",
        Status.Blocked => "Blocked",
        // The workbook's canonical text for "implemented, awaiting verification"; writing it
        // back keeps the vocabulary stable across an update of a node in that state.
        Status.InReview => "Implemented - Verify",
        Status.Completed => "Completed",
        Status.Cancelled => "Cancelled",
        Status.Deferred => "Deferred",
        Status.Obsolete => "Obsolete",
        Status.Superseded => "Superseded",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unrecognized Status."),
    };

    public static Status ParseStatus(string value)
    {
        switch (value)
        {
            case "Proposed": return Status.Proposed;
            case "Planned": return Status.Planned;
            case "Ready": return Status.Ready;
            case "In Progress": return Status.InProgress;
            case "Blocked": return Status.Blocked;
            case "Implemented - Verify": return Status.InReview;
            case "In Review": return Status.InReview;
            case "Complete": return Status.Completed;
            case "Completed": return Status.Completed;
            case "Cancelled": return Status.Cancelled;
            case "Deferred": return Status.Deferred;
            case "Obsolete": return Status.Obsolete;
            case "Superseded": return Status.Superseded;
            default: throw new InvalidOperationException($"Unrecognized node status '{value}' in the development-control workbook.");
        }
    }

    public static string PreflightVerdictToText(PreflightVerdict verdict) => verdict switch
    {
        PreflightVerdict.Clear => "CLEAR",
        PreflightVerdict.DependencyFound => "DEPENDENCY FOUND",
        PreflightVerdict.OverlapFound => "OVERLAP FOUND",
        PreflightVerdict.ConflictFound => "CONFLICT FOUND",
        PreflightVerdict.ArchitectureConflict => "ARCHITECTURE CONFLICT",
        _ => throw new ArgumentOutOfRangeException(nameof(verdict), verdict, "Unrecognized PreflightVerdict."),
    };

    public static PreflightVerdict? ParsePreflightVerdict(string value)
    {
        var upper = value.Trim().ToUpperInvariant();
        if (upper.Length == 0) return null;
        if (upper.StartsWith("CLEAR", StringComparison.Ordinal)) return PreflightVerdict.Clear;
        if (upper.StartsWith("DEPENDENCY FOUND", StringComparison.Ordinal)) return PreflightVerdict.DependencyFound;
        if (upper.StartsWith("OVERLAP FOUND", StringComparison.Ordinal)) return PreflightVerdict.OverlapFound;
        if (upper.StartsWith("CONFLICT FOUND", StringComparison.Ordinal)) return PreflightVerdict.ConflictFound;
        if (upper.StartsWith("ARCHITECTURE CONFLICT", StringComparison.Ordinal)) return PreflightVerdict.ArchitectureConflict;
        // Free-form text the enum cannot represent (e.g. "FLAGGED -- ...", "N/A (governance...)")
        // is surfaced as null, never silently mapped to a verdict.
        return null;
    }

    public static string ActorTypeToText(ActorType actorType) => actorType switch
    {
        ActorType.Human => "Human",
        ActorType.Agent => "Agent",
        _ => throw new ArgumentOutOfRangeException(nameof(actorType), actorType, "Unrecognized ActorType."),
    };

    public static ActorType ParseActorType(string value)
    {
        if (string.Equals(value, "Agent", StringComparison.OrdinalIgnoreCase)) return ActorType.Agent;
        if (string.Equals(value, "Human", StringComparison.OrdinalIgnoreCase)) return ActorType.Human;
        // The ActivityLogEntry DTO (WI-07-0.2.1) has no "unknown" member. 22 of the 49 live
        // Activity Log rows have a blank Actor Type cell; every attributed row is "Agent", so
        // blank is read as Agent to keep the append-only log readable. This default is
        // documented, not hidden -- AF-010.
        if (string.IsNullOrWhiteSpace(value)) return ActorType.Agent;
        throw new InvalidOperationException($"Unrecognized actor type '{value}' in the Activity Log.");
    }

    // --- sorting / identifiers ------------------------------------------------

    // Dotted numeric SortKey ordering ("01.001.001.001" < "01.001.002"), segment-by-segment
    // as integers so "01.2" sorts before "01.10" (a lexicographic compare would be wrong).
    public static int CompareSortKeys(string? x, string? y)
    {
        var emptyX = string.IsNullOrEmpty(x);
        var emptyY = string.IsNullOrEmpty(y);
        if (emptyX && emptyY) return 0;
        if (emptyX) return -1;
        if (emptyY) return 1;
        var xs = x!.Split('.');
        var ys = y!.Split('.');
        var n = Math.Min(xs.Length, ys.Length);
        for (var i = 0; i < n; i++)
        {
            var xi = int.TryParse(xs[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out var a) ? a : int.MaxValue;
            var yi = int.TryParse(ys[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out var b) ? b : int.MaxValue;
            var cmp = xi.CompareTo(yi);
            if (cmp != 0) return cmp;
        }
        return xs.Length.CompareTo(ys.Length);
    }

    // Next "ACT-YYYYMMDD-NNN" in the workbook's own scheme, scanning the log's existing ids.
    public static string NextActivityId(IEnumerable<string> existingIds, DateTimeOffset now)
    {
        var prefix = "ACT-" + now.UtcDateTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + "-";
        var max = 0;
        foreach (var id in existingIds)
        {
            if (id.StartsWith(prefix, StringComparison.Ordinal)
                && int.TryParse(id.Substring(prefix.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)
                && n > max)
                max = n;
        }
        return prefix + (max + 1).ToString("000", CultureInfo.InvariantCulture);
    }

    // Compact Before/After narrative for the Activity Log's Before Value / After Value columns.
    public static string SummarizeNode(Node node) =>
        node.NodeId + " | " + (node.ParentId?.Value ?? "(root)") + " | " + StatusToText(node.Status)
        + (node.ManualProgress.HasValue ? " | " + node.ManualProgress.Value.ToString(CultureInfo.InvariantCulture) + "%" : "");
}
