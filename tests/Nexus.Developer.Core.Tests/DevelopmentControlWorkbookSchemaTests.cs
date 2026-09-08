using ClosedXML.Excel;
using Nexus.Developer.Infrastructure.DevelopmentControl;
using Xunit;

namespace Nexus.Developer.Core.Tests;

// SP1-M04: schema-version concept for the development-control workbook. The marker is a labelled
// "Schema Version" pair on the Control Center sheet (label whose normalized text is
// "schemaversion" with the integer version immediately to its right). These tests lock the
// backward-compatible read path: a workbook WITHOUT the marker is the documented legacy baseline
// and reads exactly as today (no required-column throw), a workbook WITH the current marker reads
// its version, and a future-version marker yields the controlled outcome -- never a throw.
public class DevelopmentControlWorkbookSchemaTests
{
    [Fact]
    public void Read_WorkbookWithoutMarker_IsTheLegacyBaseline()
    {
        using var workbook = new XLWorkbook();
        workbook.AddWorksheet("Control Center").Cell(2, 1).SetValue("Development Control\nWorkbook v3.26");

        var result = DevelopmentControlWorkbookSchema.Read(workbook);

        Assert.Equal(DevelopmentControlSchemaCategory.Legacy, result.Category);
        Assert.Equal(DevelopmentControlWorkbookSchema.LegacyBaselineVersion, result.DeclaredVersion);
    }

    [Fact]
    public void Read_WorkbookWithCurrentMarker_ReadsItsVersion()
    {
        using var workbook = new XLWorkbook();
        var control = workbook.AddWorksheet("Control Center");
        control.Cell(2, 1).SetValue("Development Control\nWorkbook v3.26");
        control.Cell(3, 1).SetValue("Schema Version");
        control.Cell(3, 2).SetValue(DevelopmentControlWorkbookSchema.CurrentVersion);

        var result = DevelopmentControlWorkbookSchema.Read(workbook);

        Assert.Equal(DevelopmentControlSchemaCategory.Current, result.Category);
        Assert.Equal(1, result.DeclaredVersion);
    }

    [Fact]
    public void Read_FutureVersionMarker_YieldsTheControlledOutcome()
    {
        using var workbook = new XLWorkbook();
        var control = workbook.AddWorksheet("Control Center");
        control.Cell(3, 1).SetValue("Schema Version");
        control.Cell(3, 2).SetValue(999);

        var result = DevelopmentControlWorkbookSchema.Read(workbook);

        Assert.Equal(DevelopmentControlSchemaCategory.Future, result.Category);
        Assert.Equal(999, result.DeclaredVersion);
        Assert.Contains("newer than this build supports", DevelopmentControlWorkbookSchema.FutureVersionMessage(999));
    }

    [Fact]
    public void Read_MarkerLabelFoundElsewhereInColumnA_IsHonoured()
    {
        // The reader scans a small window of column A rather than assuming a fixed row, so a
        // marker placed anywhere in that window is found.
        using var workbook = new XLWorkbook();
        var control = workbook.AddWorksheet("Control Center");
        control.Cell(2, 1).SetValue("Development Control");
        control.Cell(10, 1).SetValue("Schema Version");
        control.Cell(10, 2).SetValue(1);

        var result = DevelopmentControlWorkbookSchema.Read(workbook);

        Assert.Equal(DevelopmentControlSchemaCategory.Current, result.Category);
        Assert.Equal(1, result.DeclaredVersion);
    }

    [Fact]
    public void Read_MalformedMarkerValue_IsTreatedAsLegacy()
    {
        using var workbook = new XLWorkbook();
        var control = workbook.AddWorksheet("Control Center");
        control.Cell(3, 1).SetValue("Schema Version");
        control.Cell(3, 2).SetValue("not-a-number");

        var result = DevelopmentControlWorkbookSchema.Read(workbook);

        Assert.Equal(DevelopmentControlSchemaCategory.Legacy, result.Category);
        Assert.Equal(0, result.DeclaredVersion);
    }

    [Fact]
    public void Read_WorkbookWithNoControlCenterSheet_IsLegacy()
    {
        using var workbook = new XLWorkbook();
        workbook.AddWorksheet("Some Other Sheet");

        var result = DevelopmentControlWorkbookSchema.Read(workbook);

        Assert.Equal(DevelopmentControlSchemaCategory.Legacy, result.Category);
        Assert.Equal(0, result.DeclaredVersion);
    }

    [Fact]
    public void Stamp_OnALegacyWorkbook_WritesTheCurrentMarker()
    {
        using var workbook = new XLWorkbook();
        workbook.AddWorksheet("Control Center").Cell(2, 1).SetValue("Development Control");

        DevelopmentControlWorkbookSchema.Stamp(workbook);
        var result = DevelopmentControlWorkbookSchema.Read(workbook);

        Assert.Equal(DevelopmentControlSchemaCategory.Current, result.Category);
        Assert.Equal(1, result.DeclaredVersion);
    }

    [Fact]
    public void Stamp_OnAFutureVersionWorkbook_DoesNotDowngradeOrClobber()
    {
        using var workbook = new XLWorkbook();
        var control = workbook.AddWorksheet("Control Center");
        control.Cell(3, 1).SetValue("Schema Version");
        control.Cell(3, 2).SetValue(5);

        DevelopmentControlWorkbookSchema.Stamp(workbook);
        var result = DevelopmentControlWorkbookSchema.Read(workbook);

        Assert.Equal(DevelopmentControlSchemaCategory.Future, result.Category);
        Assert.Equal(5, result.DeclaredVersion);
    }
}
