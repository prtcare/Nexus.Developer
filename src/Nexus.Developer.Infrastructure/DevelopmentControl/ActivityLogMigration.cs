using System.Globalization;
using ClosedXML.Excel;
using Nexus.Developer.Core.DevelopmentControl;

namespace Nexus.Developer.Infrastructure.DevelopmentControl;

// Result of one ActivityLogMigration.Migrate run. DryRun reports whether the run was
// read-only; AlreadyMigrated reports an idempotent no-op on a sheet that already carries
// the 34-column schema; WroteFile reports whether the workbook was saved. SourceRowCount
// is the number of data rows under the old header (row numbers header+1 .. last used).
// VerificationPassed is only true when the migration re-read every written cell and
// confirmed the row count is unchanged and every mapped value round-trips exactly;
// VerificationErrors names any mismatch. RowSamples carries the first few rows' actual
// old->new field mapping for reporting.
public sealed record ActivityLogMigrationResult(
    bool DryRun,
    bool AlreadyMigrated,
    bool WroteFile,
    int SourceRowCount,
    int TargetColumnCount,
    bool VerificationPassed,
    IReadOnlyList<string> VerificationErrors,
    IReadOnlyList<ActivityLogRowSample> RowSamples);

// A sample of one migrated row: the physical worksheet row number, the Activity Id, and
// the non-blank target columns' values keyed by their target column header.
public sealed record ActivityLogRowSample(
    int WorksheetRowNumber,
    string ActivityId,
    IReadOnlyList<KeyValuePair<string, string?>> MappedValues);

// WI-07-0.2.2: one-time, in-place widening of the workbook's Activity Log sheet from the
// 11-column source layout to the full 34-column ActivityLogEntry schema. Dry-run by
// default (never writes). Idempotent: an already-widened sheet is detected by its 34
// column header and left untouched. Every run verifies itself before reporting success:
// the data-region row count is unchanged and each written cell reads back exactly.
//
// Field mapping (old column -> new columns):
//   Activity ID    -> Activity ID
//   Timestamp      -> Timestamp UTC (Excel serial / OADate -> ISO 8601 UTC)
//   Actor / Worker -> Actor Name; Actor Type inferred (Codex/Claude/ChatGPT/DeepCode ->
//                     Agent, a human name -> Human, blank or "Unassigned" -> blank);
//                     Actor ID blank (the source sheet carried no actor id)
//   Activity Type  -> Operation
//   Change ID      -> Change ID
//   Node / Area    -> Entity ID
//   Action         -> Reason
//   Result/Evidence-> Result and Evidence (same value); Status is appended to Result via
//                     "; " when both are present
//   Version / ADR  -> folded into Reason as "Version/ADR: <value>" -- the 34-field
//                     ActivityLogEntry contract has no column for it, so it is preserved
//                     in the Reason narrative rather than dropped (zero data loss)
//   Notes          -> folded into Reason as "Notes: <value>" (same reason as above)
//
// All remaining target columns (Actor ID, Source, Chat Platform, ... Created At) are
// left blank: the source sheet never captured them and no value is fabricated.
public static class ActivityLogMigration
{
    public const string SheetName = "Activity Log";
    public const int HeaderRowNumber = 4;
    public const int SourceColumnCount = 11;
    public const int TargetColumnCount = 34;

    private static readonly string[] SourceHeaders =
    {
        "Activity ID", "Timestamp", "Actor / Worker", "Activity Type", "Change ID",
        "Node / Area", "Action", "Result / Evidence", "Version / ADR", "Status", "Notes"
    };

    private static readonly string[] TargetHeaders =
    {
        "Activity ID", "Timestamp UTC", "Actor Type", "Actor ID", "Actor Name", "Source",
        "Chat Platform", "Chat/Session ID", "Prompt ID", "Change ID", "Correlation ID",
        "Operation", "Entity Type", "Entity ID", "Parent ID", "Expected RowVersion",
        "Previous RowVersion", "New RowVersion", "Before Value", "After Value", "Reason",
        "Repository", "Project", "Branch", "Worktree", "Files/Globs", "Preflight Verdict",
        "Result", "Evidence", "Error Code", "Error Message", "Duration",
        "Human Review Status", "Created At"
    };

    private static readonly string[] AgentNameMarkers =
        { "codex", "claude", "chatgpt", "deepcode" };

    public static ActivityLogMigrationResult Migrate(string workbookPath, bool dryRun = true)
    {
        if (workbookPath is null) throw new ArgumentNullException(nameof(workbookPath));
        if (!File.Exists(workbookPath))
            throw new FileNotFoundException($"Workbook '{workbookPath}' was not found.", workbookPath);

        using var workbook = new XLWorkbook(workbookPath);
        if (!workbook.Worksheets.TryGetWorksheet(SheetName, out var worksheet))
            throw new InvalidOperationException(
                $"Workbook '{workbookPath}' has no sheet named '{SheetName}'.");

        var header = ReadHeaderRow(worksheet);

        // Idempotency: an already-migrated sheet is exactly the target schema.
        if (HeadersEqual(header, TargetHeaders))
        {
            return new ActivityLogMigrationResult(
                DryRun: dryRun,
                AlreadyMigrated: true,
                WroteFile: false,
                SourceRowCount: 0,
                TargetColumnCount: TargetColumnCount,
                VerificationPassed: true,
                VerificationErrors: Array.Empty<string>(),
                RowSamples: Array.Empty<ActivityLogRowSample>());
        }

        if (!HeadersEqual(header, SourceHeaders))
        {
            throw new InvalidOperationException(
                $"Sheet '{SheetName}' header row {HeaderRowNumber} is neither the " +
                $"{SourceColumnCount}-column source schema nor the {TargetColumnCount}-column " +
                $"target schema. Found {header.Length} columns: [{string.Join(", ", header)}].");
        }

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? HeaderRowNumber;
        var sourceRowCount = Math.Max(0, lastRow - HeaderRowNumber);

        // Map every physical data row (blank rows included) so the sheet's physical layout
        // and row count are preserved exactly.
        var expected = new List<MappedRow>(sourceRowCount);
        var samples = new List<ActivityLogRowSample>(3);
        for (var row = HeaderRowNumber + 1; row <= lastRow; row++)
        {
            var source = ReadSourceRow(worksheet, row);
            var target = Map(source);
            if (samples.Count < 3)
            {
                samples.Add(new ActivityLogRowSample(row, target[0], Zip(target)));
            }

            expected.Add(new MappedRow(row, target));
        }

        WriteRow(worksheet, HeaderRowNumber, TargetHeaders);
        foreach (var mapped in expected)
        {
            WriteRow(worksheet, mapped.WorksheetRowNumber, mapped.Values);
        }

        var errors = Verify(worksheet, expected, sourceRowCount);
        var verificationPassed = errors.Count == 0;

        var wroteFile = false;
        if (!dryRun && verificationPassed)
        {
            workbook.Save();
            wroteFile = true;
        }

        return new ActivityLogMigrationResult(
            DryRun: dryRun,
            AlreadyMigrated: false,
            WroteFile: wroteFile,
            SourceRowCount: sourceRowCount,
            TargetColumnCount: TargetColumnCount,
            VerificationPassed: verificationPassed,
            VerificationErrors: errors,
            RowSamples: samples);
    }

    private sealed record MappedRow(int WorksheetRowNumber, string[] Values);

    // --- Mapping ------------------------------------------------------------------

    private static string[] Map(IReadOnlyList<string> source)
    {
        string Get(int index) => index < source.Count ? source[index] : string.Empty;

        var action = Get(6);          // Action
        var versionAdr = Get(8);      // Version / ADR
        var notes = Get(10);          // Notes
        var reason = Join(action, versionAdr is "" ? "" : $"Version/ADR: {versionAdr}");
        reason = Join(reason, notes is "" ? "" : $"Notes: {notes}");

        var resultEvidence = Get(7);  // Result / Evidence
        var status = Get(9);          // Status

        return new[]
        {
            Get(0),                     // Activity ID
            ConvertTimestamp(Get(1)),   // Timestamp UTC
            InferActorType(Get(2)),     // Actor Type
            string.Empty,               // Actor ID
            Get(2),                     // Actor Name
            string.Empty,               // Source
            string.Empty,               // Chat Platform
            string.Empty,               // Chat/Session ID
            string.Empty,               // Prompt ID
            Get(4),                     // Change ID
            string.Empty,               // Correlation ID
            Get(3),                     // Operation
            string.Empty,               // Entity Type
            Get(5),                     // Entity ID
            string.Empty,               // Parent ID
            string.Empty,               // Expected RowVersion
            string.Empty,               // Previous RowVersion
            string.Empty,               // New RowVersion
            string.Empty,               // Before Value
            string.Empty,               // After Value
            reason,                     // Reason (Action; Version/ADR and Notes folded in)
            string.Empty,               // Repository
            string.Empty,               // Project
            string.Empty,               // Branch
            string.Empty,               // Worktree
            string.Empty,               // Files/Globs
            string.Empty,               // Preflight Verdict
            Join(resultEvidence, status), // Result (Evidence + Status)
            resultEvidence,             // Evidence
            string.Empty,               // Error Code
            string.Empty,               // Error Message
            string.Empty,               // Duration
            string.Empty,               // Human Review Status
            string.Empty                // Created At
        };
    }

    private static string ConvertTimestamp(string value)
    {
        if (value is "") return string.Empty;

        // The source sheet writes timestamps as Excel serial numbers. Where the cell was
        // stored as text (already-ISO or a literal), it passes through untouched.
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var serial))
        {
            try
            {
                var utc = DateTime.SpecifyKind(DateTime.FromOADate(serial), DateTimeKind.Utc);
                return utc.ToString("o", CultureInfo.InvariantCulture);
            }
            catch (ArgumentOutOfRangeException)
            {
                // Not a valid OADate serial; keep the original text rather than fabricate.
                return value;
            }
        }

        return value;
    }

    private static string InferActorType(string actorName)
    {
        if (string.IsNullOrWhiteSpace(actorName)) return string.Empty;
        if (string.Equals(actorName.Trim(), "Unassigned", StringComparison.OrdinalIgnoreCase))
            return string.Empty; // not an agent, and not a human name -- do not fabricate

        foreach (var marker in AgentNameMarkers)
        {
            if (actorName.Contains(marker, StringComparison.OrdinalIgnoreCase))
                return nameof(ActorType.Agent);
        }

        return nameof(ActorType.Human);
    }

    private static string Join(string left, string right) =>
        left is "" ? right : right is "" ? left : left + "; " + right;

    // --- Workbook I/O ---------------------------------------------------------------

    private static string[] ReadHeaderRow(IXLWorksheet worksheet)
    {
        var row = worksheet.Row(HeaderRowNumber);
        var lastColumn = row.LastCellUsed()?.Address.ColumnNumber ?? 0;
        var header = new string[lastColumn];
        for (var column = 1; column <= lastColumn; column++)
        {
            var cell = row.Cell(column);
            header[column - 1] = cell.IsEmpty() ? string.Empty : cell.GetString().Trim();
        }
        return header;
    }

    private static string[] ReadSourceRow(IXLWorksheet worksheet, int rowNumber)
    {
        var values = new string[SourceColumnCount];
        var row = worksheet.Row(rowNumber);
        for (var column = 1; column <= SourceColumnCount; column++)
        {
            values[column - 1] = ReadCell(row.Cell(column));
        }
        return values;
    }

    private static string ReadCell(IXLCell cell)
    {
        if (cell.IsEmpty()) return string.Empty;

        // Column B (Timestamp) is stored as an Excel serial number; read the raw value so
        // ConvertTimestamp can turn it into ISO UTC. Text cells pass through trimmed.
        if (cell.DataType == XLDataType.Number || cell.DataType == XLDataType.DateTime)
        {
            var serial = cell.DataType == XLDataType.DateTime
                ? cell.GetDateTime().ToOADate()
                : cell.GetDouble();
            return serial.ToString("R", CultureInfo.InvariantCulture);
        }

        return cell.GetString().Trim();
    }

    private static void WriteRow(IXLWorksheet worksheet, int rowNumber, IReadOnlyList<string> values)
    {
        var row = worksheet.Row(rowNumber);
        for (var column = 0; column < values.Count; column++)
        {
            row.Cell(column + 1).SetValue(values[column]);
        }
    }

    // --- Verification ----------------------------------------------------------------

    private static List<string> Verify(
        IXLWorksheet worksheet,
        IReadOnlyList<MappedRow> expected,
        int sourceRowCount)
    {
        var errors = new List<string>();

        var header = ReadHeaderRow(worksheet);
        if (!HeadersEqual(header, TargetHeaders))
        {
            errors.Add($"Header row does not match the {TargetColumnCount}-column target schema after write.");
        }

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? HeaderRowNumber;
        var rowCount = Math.Max(0, lastRow - HeaderRowNumber);
        if (rowCount != sourceRowCount)
        {
            errors.Add($"Data row count changed during migration: {sourceRowCount} -> {rowCount}.");
        }

        foreach (var mapped in expected)
        {
            var actual = ReadTargetRow(worksheet, mapped.WorksheetRowNumber);
            for (var column = 0; column < TargetColumnCount; column++)
            {
                if (actual[column] != mapped.Values[column])
                {
                    errors.Add(
                        $"Row {mapped.WorksheetRowNumber} column '{TargetHeaders[column]}': " +
                        $"expected '{mapped.Values[column]}', read '{actual[column]}'.");
                }
            }
        }

        return errors;
    }

    private static string[] ReadTargetRow(IXLWorksheet worksheet, int rowNumber)
    {
        var values = new string[TargetColumnCount];
        for (var column = 1; column <= TargetColumnCount; column++)
        {
            values[column - 1] = ReadCell(worksheet.Row(rowNumber).Cell(column));
        }
        return values;
    }

    private static bool HeadersEqual(IReadOnlyList<string> actual, string[] expected)
    {
        if (actual.Count != expected.Length) return false;
        for (var index = 0; index < expected.Length; index++)
        {
            if (!string.Equals(actual[index], expected[index], StringComparison.Ordinal))
                return false;
        }
        return true;
    }

    private static IReadOnlyList<KeyValuePair<string, string?>> Zip(IReadOnlyList<string> values)
    {
        var pairs = new List<KeyValuePair<string, string?>>();
        for (var index = 0; index < values.Count; index++)
        {
            if (values[index] is not "")
            {
                pairs.Add(new KeyValuePair<string, string?>(TargetHeaders[index], values[index]));
            }
        }
        return pairs;
    }
}
