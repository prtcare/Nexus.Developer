using System.Text;
using ClosedXML.Excel;
using Nexus.Developer.Core.DevelopmentControl;

namespace Nexus.Developer.Infrastructure.DevelopmentControl;

// WI-07-0.2.2: structural validation of NEXUS_DEVELOPMENT_CONTROL.xlsx against the
// DevelopmentControl contracts (WI-07-0.2.1). For each tracked sheet, its header row is
// read and compared to the corresponding record's constructor parameter names (obtained
// by reflection, so the comparison can never drift from the contract). Columns are
// matched case-insensitively with every non-alphanumeric character removed; a small
// per-sheet alias table reconciles the workbook's slash/space wordings with the
// contract's Or-composed field names ("Milestone / Feature" -> MilestoneOrFeature,
// "Hierarchy Path" -> Path). The result is an informational report (see
// WorkbookSchemaReport) -- the caller decides whether a gap is acceptable.
public static class WorkbookSchemaValidator
{
    private sealed record SheetDefinition(
        string SheetName,
        int HeaderRow,
        Type ContractType,
        IReadOnlyDictionary<string, string> Aliases); // normalized header -> contract field

    private static readonly SheetDefinition[] Definitions =
    {
        new(
            "Master Roadmap", 5, typeof(Node),
            new Dictionary<string, string>
            {
                ["hierarchypath"] = "Path",
                ["outcomepurpose"] = "Outcome"
            }),
        new(
            "Active Changes", 5, typeof(ActiveChange),
            new Dictionary<string, string>
            {
                ["milestonefeature"] = "MilestoneOrFeature",
                ["resultevidence"] = "ResultOrEvidence",
                ["sessionchat"] = "SessionOrChat"
            }),
        new(
            "Audit Findings", 5, typeof(AuditFinding),
            new Dictionary<string, string>())
    };

    public static WorkbookSchemaReport Validate(string workbookPath)
    {
        if (workbookPath is null) throw new ArgumentNullException(nameof(workbookPath));
        using var workbook = new XLWorkbook(workbookPath);
        return Validate(workbook);
    }

    public static WorkbookSchemaReport Validate(IXLWorkbook workbook)
    {
        var sheets = Definitions.Select(definition => ValidateSheet(workbook, definition)).ToArray();
        return new WorkbookSchemaReport(sheets);
    }

    private static SheetSchemaReport ValidateSheet(IXLWorkbook workbook, SheetDefinition definition)
    {
        if (!workbook.Worksheets.TryGetWorksheet(definition.SheetName, out var worksheet))
        {
            throw new InvalidOperationException(
                $"Workbook does not contain the required sheet '{definition.SheetName}'.");
        }

        var contractFields = ContractFields(definition.ContractType);
        var normalizedFields = contractFields
            .ToDictionary(field => Normalize(field), field => field, StringComparer.Ordinal);

        var header = ReadHeader(worksheet, definition.HeaderRow);
        var matched = new List<string>();
        var unmapped = new List<string>();

        foreach (var column in header)
        {
            var normalized = Normalize(column);
            if (normalizedFields.ContainsKey(normalized) ||
                (definition.Aliases.TryGetValue(normalized, out var alias) &&
                 normalizedFields.ContainsKey(Normalize(alias))))
            {
                matched.Add(column);
            }
            else
            {
                unmapped.Add(column);
            }
        }

        var matchedFields = new HashSet<string>(StringComparer.Ordinal);
        foreach (var column in matched)
        {
            var normalized = Normalize(column);
            if (normalizedFields.TryGetValue(normalized, out var field))
            {
                matchedFields.Add(field);
            }
            else if (definition.Aliases.TryGetValue(normalized, out var alias))
            {
                matchedFields.Add(alias);
            }
        }

        var withoutColumn = contractFields
            .Where(field => !matchedFields.Contains(field))
            .ToArray();

        return new SheetSchemaReport(
            definition.SheetName,
            definition.ContractType.Name,
            definition.HeaderRow,
            header.Length,
            matched,
            unmapped,
            withoutColumn);
    }

    private static string[] ContractFields(Type contractType) =>
        contractType.GetConstructors().Single().GetParameters().Select(p => p.Name!).ToArray();

    private static string[] ReadHeader(IXLWorksheet worksheet, int headerRow)
    {
        var row = worksheet.Row(headerRow);
        var lastColumn = row.LastCellUsed()?.Address.ColumnNumber ?? 0;
        var header = new string[lastColumn];
        for (var column = 1; column <= lastColumn; column++)
        {
            var cell = row.Cell(column);
            header[column - 1] = cell.IsEmpty() ? string.Empty : cell.GetString().Trim();
        }
        return header;
    }

    private static string Normalize(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }
        return builder.ToString();
    }
}
