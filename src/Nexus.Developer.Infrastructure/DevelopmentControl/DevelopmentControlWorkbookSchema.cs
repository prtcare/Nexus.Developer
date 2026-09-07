using System.Globalization;
using ClosedXML.Excel;

namespace Nexus.Developer.Infrastructure.DevelopmentControl;

// SP1-M04: an explicit, strictly backward-compatible schema-version concept for the
// development-control workbook. Before this, the governed sheets' layout had NO version marker
// at all: every older workbook is a LEGACY workbook and must keep reading exactly as it always
// has -- no governed sheet gains a new required column, so WorksheetColumnMap.Required never
// throws for a workbook without the marker.
//
// The marker is carried on the Control Center sheet (the workbook's control/metadata home, which
// already holds the workbook/roadmap-version narrative) as a labelled key/value pair: a label
// cell whose normalized text equals WorkbookColumns.SchemaVersion ("schemaversion") with the
// integer schema version in the cell immediately to its right. The ExcelDevelopmentControlStore
// stamps this pair on every successful governed save, so the next save evolves a legacy workbook
// into a self-declaring current-version workbook.
public enum DevelopmentControlSchemaCategory
{
    // No meaningful marker: the documented pre-versioning baseline. Read exactly as before.
    Legacy = 0,

    // Marker present and equal to the current schema version this build understands.
    Current = 1,

    // Marker present and GREATER than the current schema version: a workbook written by a newer
    // build. Reading is tolerated; WRITING is refused as a controlled failure surfaced as a
    // mutation/validation result (never an uncaught exception thrown mid-write).
    Future = 2
}

public sealed record DevelopmentControlSchemaReadResult(
    DevelopmentControlSchemaCategory Category,
    int DeclaredVersion);

public static class DevelopmentControlWorkbookSchema
{
    // The current schema version. Version 1 is the FIRST versioned schema: the workbook as it is
    // once a governed save has stamped the marker. The pre-marker layout is the legacy baseline.
    public const int CurrentVersion = 1;

    // The documented legacy baseline: a workbook with no marker. Always 0.
    public const int LegacyBaselineVersion = 0;

    // The Control Center sheet is where the workbook's own version/control metadata already lives.
    private const string ControlCenterSheetName = "Control Center";

    // The label cell's default home when no marker exists yet: row 3 sits just below the Control
    // Center's changelog narrative (row 2) and above the summary tables (row 4+), so stamping it
    // never overwrites existing narrative or table cells. The reader scans a small window of
    // column A rather than assuming this exact row, so a marker placed elsewhere is still found.
    private const int DefaultMarkerRow = 3;

    // How far down column A the reader scans for the label cell. The Control Center is a compact
    // control surface, not a data grid; 20 rows comfortably covers the metadata region.
    private const int MaxSearchRow = 20;

    public static DevelopmentControlSchemaReadResult Read(IXLWorkbook workbook)
    {
        if (workbook is null) throw new ArgumentNullException(nameof(workbook));
        if (!workbook.Worksheets.TryGetWorksheet(ControlCenterSheetName, out var control))
        {
            // No Control Center sheet at all: nothing can be declared -- legacy baseline.
            return new DevelopmentControlSchemaReadResult(
                DevelopmentControlSchemaCategory.Legacy, LegacyBaselineVersion);
        }

        if (TryFindMarker(control, out var markerRow))
        {
            var valueText = CellText(control.Cell(markerRow, 2));
            var declared = int.TryParse(valueText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : 0;
            return Classify(declared);
        }

        return new DevelopmentControlSchemaReadResult(
            DevelopmentControlSchemaCategory.Legacy, LegacyBaselineVersion);
    }

    // Writes the current schema-version marker into a workbook. Never downgrades or clobbers a
    // future marker; no-ops when the workbook already declares the current version. A legacy
    // (or malformed) marker is brought up to the current version, which is exactly what turns the
    // next governed save into the workbook's schema-versioning step.
    public static void Stamp(IXLWorkbook workbook)
    {
        if (workbook is null) throw new ArgumentNullException(nameof(workbook));
        if (!workbook.Worksheets.TryGetWorksheet(ControlCenterSheetName, out var control)) return;

        var schema = Read(workbook);
        if (schema.Category is DevelopmentControlSchemaCategory.Current or DevelopmentControlSchemaCategory.Future)
        {
            return;
        }

        var row = TryFindMarker(control, out var existingRow) ? existingRow : DefaultMarkerRow;
        control.Cell(row, 1).SetValue("Schema Version");
        control.Cell(row, 2).SetValue(CurrentVersion);
    }

    // The controlled, clearly-documented outcome for a future-version workbook.
    public static string FutureVersionMessage(int declaredVersion) =>
        $"The development-control workbook declares schema version {declaredVersion.ToString(CultureInfo.InvariantCulture)}, "
        + $"which is newer than this build supports (current: {CurrentVersion.ToString(CultureInfo.InvariantCulture)}). "
        + "Refusing to write to a future-version workbook; upgrade the reader to support it.";

    private static DevelopmentControlSchemaReadResult Classify(int declaredVersion)
    {
        if (declaredVersion == CurrentVersion)
        {
            return new DevelopmentControlSchemaReadResult(DevelopmentControlSchemaCategory.Current, declaredVersion);
        }

        if (declaredVersion > CurrentVersion)
        {
            return new DevelopmentControlSchemaReadResult(DevelopmentControlSchemaCategory.Future, declaredVersion);
        }

        // A non-positive or otherwise meaningless declared value is not a marker this reader can
        // honour -- fall back to the legacy baseline (documented, never a throw).
        return new DevelopmentControlSchemaReadResult(DevelopmentControlSchemaCategory.Legacy, LegacyBaselineVersion);
    }

    private static bool TryFindMarker(IXLWorksheet control, out int markerRow)
    {
        markerRow = 0;
        var lastRow = control.LastCellUsed()?.Address.RowNumber ?? 0;
        var limit = Math.Min(lastRow, MaxSearchRow);
        for (var row = 1; row <= limit; row++)
        {
            var cell = control.Cell(row, 1);
            if (cell.IsEmpty()) continue;
            if (WorkbookColumns.Normalize(CellText(cell)) == WorkbookColumns.SchemaVersion)
            {
                markerRow = row;
                return true;
            }
        }
        return false;
    }

    private static string CellText(IXLCell cell)
    {
        if (cell.IsEmpty()) return "";
        return cell.GetString().Trim();
    }
}
