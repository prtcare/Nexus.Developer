using System.Text;
using ClosedXML.Excel;

namespace Nexus.Developer.Infrastructure.DevelopmentControl;

// WI-07-0.2.3: normalized header names for the governed sheets of the canonical
// NEXUS_DEVELOPMENT_CONTROL workbook, per the DB-M02 development-control map, plus the
// runtime header-row -> column resolver the Excel store uses so it never hard-codes a
// column letter and survives a header re-order.
//
// Normalization mirrors WorkbookSchemaValidator.Normalize exactly: non-alphanumeric
// characters are stripped and the remainder lower-cased ("Hierarchy Path" ->
// "hierarchypath"). Every constant below is already in that normalized form.
internal static class WorkbookColumns
{
    public static string Normalize(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (char.IsLetterOrDigit(ch)) builder.Append(char.ToLowerInvariant(ch));
        }
        return builder.ToString();
    }

    // --- Master Roadmap / Version History / Active Changes / Audit Findings / Activity Log ---
    public const string NodeId = "nodeid";
    public const string ParentId = "parentid";
    public const string NodeType = "nodetype";
    public const string SortKey = "sortkey";
    public const string HierarchyPath = "hierarchypath";
    public const string Layer = "layer";
    public const string Phase = "phase";
    public const string Name = "name";
    public const string OutcomePurpose = "outcomepurpose";
    public const string Dependencies = "dependencies";
    public const string ParallelSafe = "parallelsafe";
    public const string Projects = "projects";
    public const string FilesGlobs = "filesglobs";
    public const string SchemaContexts = "schemacontexts";
    public const string ContractsApis = "contractsapis";
    public const string Gate = "gate";
    public const string AcceptanceCriteria = "acceptancecriteria";
    public const string Status = "status";
    public const string BreakdownComplete = "breakdowncomplete";
    public const string ManualProgress = "manualprogress";
    public const string DerivedProgress = "derivedprogress";
    public const string ReportedProgress = "reportedprogress";
    public const string Owner = "owner";
    public const string Priority = "priority";
    public const string Risk = "risk";
    public const string Source = "source";
    public const string Notes = "notes";

    // --- Version History (columns Z..AJ) ---
    public const string BaselineVersion = "baselineversion";
    public const string RecordVersion = "recordversion";
    public const string EffectiveFrom = "effectivefrom";
    public const string IsCurrent = "iscurrent";
    public const string ChangeId = "changeid";
    public const string SupersedesVersion = "supersedesversion";
    public const string ChangeType = "changetype";
    public const string ChangeSummary = "changesummary";
    public const string AdrDecisionLink = "adrdecisionlink";

    // --- Active Changes ---
    public const string MilestoneOrFeature = "milestonefeature";
    public const string Summary = "summary";
    public const string RequestedBy = "requestedby";
    public const string Worker = "worker";
    public const string Repositories = "repositories";
    public const string AffectedNodes = "affectednodes";
    public const string PreflightVerdict = "preflightverdict";
    // The workbook's header is "Conflicts With" (plural), normalizing to "conflictswith".
    public const string ConflictsWith = "conflictswith";
    public const string DependencyOn = "dependencyon";
    public const string Branch = "branch";
    public const string Worktree = "worktree";
    public const string StartedAt = "startedat";
    public const string LastHeartbeat = "lastheartbeat";
    public const string CompletedAt = "completedat";
    public const string ResultOrEvidence = "resultevidence";
    public const string ChangeVersion = "changeversion";
    public const string SessionOrChat = "sessionchat";
    public const string VersionHistoryId = "versionhistoryid";
    public const string AdrId = "adrid";
    public const string ValidationResult = "validationresult";

    // --- Audit Findings ---
    public const string FindingId = "findingid";
    public const string Severity = "severity";
    public const string Area = "area";
    public const string Evidence = "evidence";
    public const string Impact = "impact";
    public const string RequiredAction = "requiredaction";
    public const string RoadmapLink = "roadmaplink";
    public const string DueGate = "duegate";
    public const string Verification = "verification";

    // --- Activity Log ---
    public const string ActivityId = "activityid";
    public const string TimestampUtc = "timestamputc";
    public const string ActorType = "actortype";
    public const string ActorId = "actorid";
    public const string ActorName = "actorname";
    public const string ChatPlatform = "chatplatform";
    public const string ChatSessionId = "chatsessionid";
    public const string PromptId = "promptid";
    public const string CorrelationId = "correlationid";
    public const string Operation = "operation";
    public const string EntityType = "entitytype";
    public const string EntityId = "entityid";
    public const string ActivityParentId = "parentid";
    public const string ExpectedRowVersion = "expectedrowversion";
    public const string PreviousRowVersion = "previousrowversion";
    public const string NewRowVersion = "newrowversion";
    public const string BeforeValue = "beforevalue";
    public const string AfterValue = "aftervalue";
    public const string Reason = "reason";
    public const string Repository = "repository";
    public const string Project = "project";
    public const string Result = "result";
    public const string ErrorCode = "errorcode";
    public const string ErrorMessage = "errormessage";
    public const string Duration = "duration";
    public const string HumanReviewStatus = "humanreviewstatus";
    public const string CreatedAt = "createdat";
}

// Resolves a governed sheet's header row to 1-based column indexes by normalized header
// text, so the store reads and writes by column NAME (the map's contract) rather than by
// a hard-coded letter. Required(name) throws with a precise message when a governed sheet
// is missing a column the store needs -- an anomaly that must be surfaced, not papered
// over (AF-010).
internal sealed class WorksheetColumnMap
{
    private readonly Dictionary<string, int> _columns;

    public WorksheetColumnMap(IXLWorksheet worksheet, int headerRow)
    {
        _columns = new Dictionary<string, int>(StringComparer.Ordinal);
        var header = worksheet.Row(headerRow);
        var lastColumn = header.LastCellUsed()?.Address.ColumnNumber ?? 0;
        for (var column = 1; column <= lastColumn; column++)
        {
            var cell = header.Cell(column);
            if (cell.IsEmpty()) continue;
            var normalized = WorkbookColumns.Normalize(cell.GetString());
            if (normalized.Length == 0) continue;
            if (!_columns.ContainsKey(normalized)) _columns.Add(normalized, column);
        }
    }

    public bool TryGet(string normalizedHeader, out int column) => _columns.TryGetValue(normalizedHeader, out column);

    public int Get(string normalizedHeader, int fallback) => _columns.TryGetValue(normalizedHeader, out var column) ? column : fallback;

    public int Required(string normalizedHeader)
    {
        if (_columns.TryGetValue(normalizedHeader, out var column)) return column;
        throw new InvalidOperationException(
            $"The development-control workbook is missing a required '{normalizedHeader}' column in its header row.");
    }
}
