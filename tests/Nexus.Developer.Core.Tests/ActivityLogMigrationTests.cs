using System.Globalization;
using ClosedXML.Excel;
using Nexus.Developer.Infrastructure.DevelopmentControl;
using Xunit;

namespace Nexus.Developer.Core.Tests;

// WI-07-0.2.2: Activity Log migration -- widens the workbook's 11-column Activity Log
// sheet to the full 34-column ActivityLogEntry schema in place. These tests drive the
// migration against constructed ClosedXML workbooks (never the live file): dry-run never
// writes, commit mode rewrites header + data rows with the exact field mapping, re-runs
// are idempotent no-ops, and the sheet is verified after every write.
public class ActivityLogMigrationTests
{
    private static readonly string[] SourceHeaders =
    {
        "Activity ID", "Timestamp", "Actor / Worker", "Activity Type", "Change ID",
        "Node / Area", "Action", "Result / Evidence", "Version / ADR", "Status", "Notes"
    };

    // A live-style row: Excel serial timestamp (46259.61805555555 = 2026-08-25T14:50Z).
    private static readonly object?[] LiveRow =
    {
        "ACT-20260825-001", 46259.61805555555, "Codex", "implement", "CHG-20260825-001",
        "WI-07-0.2.1", "Implemented DevelopmentControl DTOs", "All 41 DTO tests pass",
        "v3.25", "Completed", "Verified clean by Durai"
    };

    [Fact]
    public void DryRun_LeavesTheFileUntouched()
    {
        var path = Fixture(LiveRow);
        var before = File.ReadAllBytes(path);

        var result = ActivityLogMigration.Migrate(path); // dry run is the default

        Assert.True(result.DryRun);
        Assert.False(result.AlreadyMigrated);
        Assert.False(result.WroteFile);
        Assert.True(result.VerificationPassed);
        Assert.Empty(result.VerificationErrors);
        Assert.Equal(1, result.SourceRowCount);
        Assert.Equal(34, result.TargetColumnCount);
        Assert.Equal(before, File.ReadAllBytes(path)); // byte-for-byte unchanged

        using var reopened = new XLWorkbook(path);
        var sheet = reopened.Worksheet(ActivityLogMigration.SheetName);
        Assert.Equal("Activity Type", sheet.Cell(4, 4).GetString()); // still 11-col source
    }

    [Fact]
    public void CommitMode_WidensTheSheetAndPreservesRowCount()
    {
        var path = Fixture(LiveRow);
        var before = File.ReadAllBytes(path);

        var result = ActivityLogMigration.Migrate(path, dryRun: false);

        Assert.False(result.DryRun);
        Assert.True(result.WroteFile);
        Assert.True(result.VerificationPassed);
        Assert.Empty(result.VerificationErrors);
        Assert.NotEqual(before, File.ReadAllBytes(path)); // file was rewritten

        using var reopened = new XLWorkbook(path);
        var sheet = reopened.Worksheet(ActivityLogMigration.SheetName);
        Assert.Equal(34, sheet.Row(4).LastCellUsed()!.Address.ColumnNumber);
        Assert.Equal("Timestamp UTC", sheet.Cell(4, 2).GetString());
        Assert.Equal("Actor Type", sheet.Cell(4, 3).GetString());
        Assert.Equal("Created At", sheet.Cell(4, 34).GetString());

        // Header is the last used row region; exactly one data row (row 5) preserved.
        Assert.Equal(5, sheet.LastRowUsed()!.RowNumber());
    }

    [Fact]
    public void CommitMode_MapsEverySourceColumnExactly()
    {
        var path = Fixture(LiveRow);
        var result = ActivityLogMigration.Migrate(path, dryRun: false);

        Assert.True(result.VerificationPassed);
        Assert.Empty(result.VerificationErrors);

        using var reopened = new XLWorkbook(path);
        var sheet = reopened.Worksheet(ActivityLogMigration.SheetName);
        string Get(int column) => sheet.Cell(5, column).GetString();

        Assert.Equal("ACT-20260825-001", Get(1));                      // Activity ID
        Assert.Equal(Iso(46259.61805555555), Get(2));                  // Timestamp UTC
        Assert.Equal("Agent", Get(3));                                 // Actor Type (Codex)
        Assert.Equal("", Get(4));                                      // Actor ID (none in source)
        Assert.Equal("Codex", Get(5));                                 // Actor Name
        Assert.Equal("", Get(6));                                      // Source
        Assert.Equal("", Get(7));                                      // Chat Platform
        Assert.Equal("", Get(8));                                      // Chat/Session ID
        Assert.Equal("", Get(9));                                      // Prompt ID
        Assert.Equal("CHG-20260825-001", Get(10));                     // Change ID
        Assert.Equal("", Get(11));                                     // Correlation ID
        Assert.Equal("implement", Get(12));                            // Operation
        Assert.Equal("", Get(13));                                     // Entity Type
        Assert.Equal("WI-07-0.2.1", Get(14));                          // Entity ID
        Assert.Equal("", Get(15));                                     // Parent ID
        Assert.Equal("", Get(16));                                     // Expected RowVersion
        Assert.Equal("", Get(17));                                     // Previous RowVersion
        Assert.Equal("", Get(18));                                     // New RowVersion
        Assert.Equal("", Get(19));                                     // Before Value
        Assert.Equal("", Get(20));                                     // After Value
        Assert.Equal(
            "Implemented DevelopmentControl DTOs; Version/ADR: v3.25; Notes: Verified clean by Durai",
            Get(21));                                                  // Reason (Action + folds)
        Assert.Equal("", Get(22));                                     // Repository
        Assert.Equal("", Get(23));                                     // Project
        Assert.Equal("", Get(24));                                     // Branch
        Assert.Equal("", Get(25));                                     // Worktree
        Assert.Equal("", Get(26));                                     // Files/Globs
        Assert.Equal("", Get(27));                                     // Preflight Verdict
        Assert.Equal("All 41 DTO tests pass; Completed", Get(28));     // Result (Evidence + Status)
        Assert.Equal("All 41 DTO tests pass", Get(29));                // Evidence
        Assert.Equal("", Get(30));                                     // Error Code
        Assert.Equal("", Get(31));                                     // Error Message
        Assert.Equal("", Get(32));                                     // Duration
        Assert.Equal("", Get(33));                                     // Human Review Status
        Assert.Equal("", Get(34));                                     // Created At
    }

    [Fact]
    public void CommitMode_IsIdempotent()
    {
        var path = Fixture(LiveRow);
        ActivityLogMigration.Migrate(path, dryRun: false);

        var afterFirst = File.ReadAllBytes(path);
        var second = ActivityLogMigration.Migrate(path, dryRun: false);

        Assert.True(second.AlreadyMigrated);
        Assert.False(second.WroteFile);
        Assert.True(second.VerificationPassed);
        Assert.Equal(afterFirst, File.ReadAllBytes(path)); // untouched by the re-run
    }

    [Fact]
    public void BlankRowsSurvive_AndRowCountIsPreserved()
    {
        var path = Fixture(
            new object?[] { "ACT-20260825-001", 46259.61805555555, "Codex", "a", "", "N1", "A", "", "", "", "" },
            new object?[SourceHeaders.Length], // fully blank row
            new object?[] { "ACT-20260830-002", 46264.0, "Claude (Cowork)", "b", "", "N2", "B", "OK", "", "In Progress", "" });

        var result = ActivityLogMigration.Migrate(path, dryRun: false);

        Assert.True(result.VerificationPassed);
        Assert.Empty(result.VerificationErrors);
        Assert.Equal(3, result.SourceRowCount);

        using var reopened = new XLWorkbook(path);
        var sheet = reopened.Worksheet(ActivityLogMigration.SheetName);
        Assert.Equal(7, sheet.LastRowUsed()!.RowNumber()); // 3 data rows preserved
        Assert.Equal("ACT-20260830-002", sheet.Cell(7, 1).GetString());
        Assert.Equal("In Progress", sheet.Cell(7, 28).GetString().Split("; ")[1]);
    }

    [Theory]
    [InlineData("Codex", "Agent")]
    [InlineData("Claude (Cowork)", "Agent")]
    [InlineData("Claude (Cowork) + DeepCode", "Agent")]
    [InlineData("ChatGPT", "Agent")]
    [InlineData("Durai", "Human")]
    [InlineData("Unassigned", "")] // neither an agent nor a human name: never fabricate
    [InlineData("", "")]
    public void ActorType_IsInferredFromTheWorkerName(string worker, string expectedType)
    {
        var path = Fixture(
            new object?[] { "ACT-20260830-003", 46264.0, worker, "op", "", "N", "A", "", "", "", "" });
        ActivityLogMigration.Migrate(path, dryRun: false);

        using var reopened = new XLWorkbook(path);
        var sheet = reopened.Worksheet(ActivityLogMigration.SheetName);
        Assert.Equal(expectedType, sheet.Cell(5, 3).GetString());
    }

    [Fact]
    public void UnknownHeaderSchema_Throws()
    {
        var path = Fixture(LiveRow);
        using (var workbook = new XLWorkbook(path))
        {
            var sheet = workbook.Worksheet(ActivityLogMigration.SheetName);
            sheet.Cell(4, 12).SetValue("Mystery Column"); // 12 columns, neither schema
            workbook.Save();
        }

        Assert.Throws<InvalidOperationException>(() => ActivityLogMigration.Migrate(path));
    }

    [Fact]
    public void MissingActivityLogSheet_Throws()
    {
        var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"nexus-dev-{Guid.NewGuid():N}"));
        var path = Path.Combine(dir.FullName, "no-sheet.xlsx");
        using (var workbook = new XLWorkbook())
        {
            workbook.AddWorksheet("Not The Log").Cell(1, 1).SetValue("x");
            workbook.SaveAs(path);
        }

        Assert.Throws<InvalidOperationException>(() => ActivityLogMigration.Migrate(path));
    }

    // --- Fixture helpers -----------------------------------------------------------

    private static string Fixture(params object?[][] dataRows)
    {
        var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"nexus-dev-{Guid.NewGuid():N}"));
        var path = Path.Combine(dir.FullName, "activity-log.xlsx");
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet(ActivityLogMigration.SheetName);
        for (var column = 0; column < SourceHeaders.Length; column++)
        {
            sheet.Cell(4, column + 1).SetValue(SourceHeaders[column]);
        }

        for (var row = 0; row < dataRows.Length; row++)
        {
            for (var column = 0; column < dataRows[row].Length; column++)
            {
                var value = dataRows[row][column];
                if (value is null)
                {
                    continue;
                }

                var cell = sheet.Cell(5 + row, column + 1);
                if (value is double serial)
                {
                    cell.SetValue(serial); // numeric Excel serial (Timestamp column)
                }
                else
                {
                    cell.SetValue(value.ToString()!);
                }
            }
        }

        workbook.SaveAs(path);
        return path;
    }

    // Mirror of the migration's serial -> ISO conversion, for exact assertion.
    private static string Iso(double serial) =>
        DateTime.SpecifyKind(DateTime.FromOADate(serial), DateTimeKind.Utc)
            .ToString("o", CultureInfo.InvariantCulture);
}
