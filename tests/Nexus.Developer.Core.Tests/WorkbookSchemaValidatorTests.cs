using ClosedXML.Excel;
using Nexus.Developer.Infrastructure.DevelopmentControl;
using Xunit;

namespace Nexus.Developer.Core.Tests;

// WI-07-0.2.2: workbook schema validation. The validator compares each tracked sheet's
// header row against the DevelopmentControl contract obtained by reflection, so these
// tests lock the workbook's ACTUAL column layouts (captured from the live
// NEXUS_DEVELOPMENT_CONTROL.xlsx) against the Node/ActiveChange/AuditFinding records.
// Master Roadmap is expected to diverge (wider roadmap sheet, and the store-managed
// RowVersion/IsDeleted/CreatedAt/UpdatedAt are not sheet columns); Active Changes and
// Audit Findings must map onto their contracts cleanly.
public class WorkbookSchemaValidatorTests
{
    // Live sheet layouts, header row 5, verbatim from the workbook.
    private static readonly string[] MasterRoadmapHeaders =
    {
        "Node ID", "Parent ID", "Node Type", "Sort Key", "Hierarchy Path", "Layer", "Phase",
        "Name", "Outcome / Purpose", "Dependencies", "Parallel Safe", "Projects",
        "Files / Globs", "Schema Contexts", "Contracts / APIs", "Gate",
        "Acceptance Criteria", "Status", "Breakdown Complete", "Manual Progress",
        "Derived Progress", "Reported Progress", "Owner", "Priority", "Risk", "Source",
        "Notes", "Simple Goal", "Current Evidence", "Next Action", "Column1", "Column2",
        "Column3"
    };

    private static readonly string[] ActiveChangesHeaders =
    {
        "Change ID", "Node ID", "Milestone / Feature", "Summary", "Requested By", "Worker",
        "Repositories", "Projects", "Files / Globs", "Schema Contexts", "Contracts / APIs",
        "Status", "Preflight Verdict", "Conflicts With", "Dependency On", "Risk", "Branch",
        "Worktree", "Started At", "Last Heartbeat", "Completed At", "Result / Evidence",
        "Change Version", "Session / Chat", "Notes", "Version History ID", "ADR ID",
        "Affected Nodes", "Change Type", "Validation Result"
    };

    private static readonly string[] AuditFindingsHeaders =
    {
        "Finding ID", "Severity", "Area", "Repository", "Evidence", "Impact",
        "Required Action", "Roadmap Link", "Status", "Owner", "Due Gate", "Verification",
        "Notes"
    };

    [Fact]
    public void MasterRoadmap_ReportsTheKnownGapAgainstNode()
    {
        using var fixture = CreateFixture();
        var report = WorkbookSchemaValidator.Validate(fixture.Workbook);

        var sheet = Assert.Single(report.Sheets, s => s.SheetName == "Master Roadmap");
        Assert.Equal("Node", sheet.ContractTypeName);
        Assert.Equal(5, sheet.HeaderRow);
        Assert.Equal(33, sheet.ColumnCount);

        Assert.Equal(27, sheet.MatchedColumns.Count);
        Assert.Equal(
            new[] { "Simple Goal", "Current Evidence", "Next Action", "Column1", "Column2", "Column3" },
            sheet.PresentButUnmappedColumns);

        // Store-managed lifecycle fields are not Master Roadmap columns.
        Assert.Equal(
            new[] { "RowVersion", "IsDeleted", "CreatedAt", "UpdatedAt" },
            sheet.ContractFieldsWithoutColumn);
    }

    [Fact]
    public void ActiveChanges_MapsOntoItsContractExactly()
    {
        using var fixture = CreateFixture();
        var report = WorkbookSchemaValidator.Validate(fixture.Workbook);

        var sheet = Assert.Single(report.Sheets, s => s.SheetName == "Active Changes");
        Assert.Equal("ActiveChange", sheet.ContractTypeName);
        Assert.Equal(30, sheet.ColumnCount);
        Assert.Equal(30, sheet.MatchedColumns.Count);
        Assert.Empty(sheet.PresentButUnmappedColumns);
        Assert.Empty(sheet.ContractFieldsWithoutColumn);
    }

    [Fact]
    public void AuditFindings_MapsOntoItsContractExactly()
    {
        using var fixture = CreateFixture();
        var report = WorkbookSchemaValidator.Validate(fixture.Workbook);

        var sheet = Assert.Single(report.Sheets, s => s.SheetName == "Audit Findings");
        Assert.Equal("AuditFinding", sheet.ContractTypeName);
        Assert.Equal(13, sheet.ColumnCount);
        Assert.Equal(13, sheet.MatchedColumns.Count);
        Assert.Empty(sheet.PresentButUnmappedColumns);
        Assert.Empty(sheet.ContractFieldsWithoutColumn);
    }

    [Fact]
    public void Validate_MissingSheet_Throws()
    {
        using var workbook = new XLWorkbook();
        workbook.AddWorksheet("Some Other Sheet");

        Assert.Throws<InvalidOperationException>(() => WorkbookSchemaValidator.Validate(workbook));
    }

    private static FixtureWorkbook CreateFixture() =>
        new(new Dictionary<string, string[]>
        {
            ["Master Roadmap"] = MasterRoadmapHeaders,
            ["Active Changes"] = ActiveChangesHeaders,
            ["Audit Findings"] = AuditFindingsHeaders
        });

    private sealed class FixtureWorkbook : IDisposable
    {
        public FixtureWorkbook(IReadOnlyDictionary<string, string[]> sheets)
        {
            Workbook = new XLWorkbook();
            foreach (var (name, headers) in sheets)
            {
                var sheet = Workbook.AddWorksheet(name);
                for (var column = 0; column < headers.Length; column++)
                {
                    sheet.Cell(5, column + 1).SetValue(headers[column]);
                }
            }
        }

        public XLWorkbook Workbook { get; }

        public void Dispose() => Workbook.Dispose();
    }
}
