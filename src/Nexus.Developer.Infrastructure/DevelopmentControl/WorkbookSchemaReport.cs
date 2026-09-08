namespace Nexus.Developer.Infrastructure.DevelopmentControl;

// Results of the WI-07-0.2.2 workbook-schema validation: for each tracked sheet, which
// workbook header columns map onto the DevelopmentControl contract (MatchedColumns),
// which workbook columns have no contract field (PresentButUnmappedColumns), and which
// contract fields the sheet does not carry (ContractFieldsWithoutColumn).
//
// Deliberately NOT a pass/fail verdict. Master Roadmap is expected to diverge from Node:
// it is a wider roadmap sheet (33 columns incl. Simple Goal / Current Evidence / Next
// Action and three blank bookkeeping columns), and RowVersion / IsDeleted / CreatedAt /
// UpdatedAt live in Version History / are store-managed rather than being sheet columns.
// Active Changes and Audit Findings should map onto their contracts cleanly.
public sealed record WorkbookSchemaReport(IReadOnlyList<SheetSchemaReport> Sheets);

public sealed record SheetSchemaReport(
    string SheetName,
    string ContractTypeName,
    int HeaderRow,
    int ColumnCount,
    IReadOnlyList<string> MatchedColumns,
    IReadOnlyList<string> PresentButUnmappedColumns,
    IReadOnlyList<string> ContractFieldsWithoutColumn);
