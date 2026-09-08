namespace Nexus.Developer.Core.DevelopmentControl;

// One row of the workbook's Audit Findings sheet, whose exact column set (inspected
// directly from NEXUS_DEVELOPMENT_CONTROL.xlsx sheet "Audit Findings", header row 5) is:
//   Finding ID, Severity, Area, Repository, Evidence, Impact, Required Action,
//   Roadmap Link, Status, Owner, Due Gate, Verification, Notes.
// Severity ("Critical", ...) and Status ("Open", ...) are open vocabularies in the
// workbook, so they stay strings rather than being prematurely locked into enums.
public sealed record AuditFinding(
    string FindingId,
    string Severity,
    string Area,
    string Repository,
    string Evidence,
    string Impact,
    string RequiredAction,
    string RoadmapLink,
    string Status,
    string Owner,
    string DueGate,
    string Verification,
    string Notes);
