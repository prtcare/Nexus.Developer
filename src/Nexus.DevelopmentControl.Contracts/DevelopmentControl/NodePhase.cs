namespace Nexus.Developer.Core.DevelopmentControl;

// SP1-M04: controlled, lenient classification of the Master Roadmap's free-text "Phase" column.
// The workbook's Phase column is a free string (observed live values: P0..P5, blanks). This
// helper makes the strategic-phase rule EXPLICIT without adding a column/property and without
// ever throwing:
//   - roadmap phases P0..P5 are recognized generally, on any node type;
//   - strategic-wave labels SP1/SP2/SP3 are recognized ONLY on a feature-level node (the
//     canonical roadmap's strategic phase is feature-level and must not be mechanically
//     duplicated onto descendant runtime objects);
//   - absent/legacy/freeform tokens are accepted as Legacy -- never rejected, so the
//     authoritative workbook and older files keep reading exactly as they always have.
// Classification is informational: callers (the Excel read seam, future validation/preflight)
// use it to observe, never to block a read.
public static class NodePhase
{
    public static bool IsFeatureLevel(NodeType nodeType) => nodeType == NodeType.Feature;

    public static bool IsRoadmapPhase(string? phase)
        => TryNormalize(phase, out var normalized)
           && normalized.Length == 2
           && normalized[0] == 'P'
           && normalized[1] >= '0'
           && normalized[1] <= '5';

    public static bool IsStrategicWaveLabel(string? phase)
        => TryNormalize(phase, out var normalized)
           && normalized.Length == 3
           && normalized[0] == 'S'
           && normalized[1] == 'P'
           && normalized[2] >= '1'
           && normalized[2] <= '3';

    public static NodePhaseClassification Classify(NodeType nodeType, string? phase)
    {
        var isFeatureLevel = IsFeatureLevel(nodeType);

        if (IsRoadmapPhase(phase))
        {
            return new NodePhaseClassification(
                NodePhaseCategory.RoadmapPhase,
                Normalize(phase!),
                isFeatureLevel,
                StrategicWaveMisplaced: false);
        }

        if (IsStrategicWaveLabel(phase))
        {
            // Recognized as a strategic-wave label, but only feature-level nodes are the canonical
            // home for it. A descendant carrying SP1/SP2/SP3 is an informational, NON-blocking
            // observation -- reading proceeds unchanged (never a throw).
            return new NodePhaseClassification(
                NodePhaseCategory.StrategicWave,
                Normalize(phase!),
                isFeatureLevel,
                StrategicWaveMisplaced: !isFeatureLevel);
        }

        // Absent (null/blank), legacy, or freeform tokens all land here: accepted as legacy.
        var raw = string.IsNullOrWhiteSpace(phase) ? null : phase.Trim();
        return new NodePhaseClassification(NodePhaseCategory.Legacy, raw, isFeatureLevel, StrategicWaveMisplaced: false);
    }

    private static bool TryNormalize(string? phase, out string normalized)
    {
        if (string.IsNullOrWhiteSpace(phase))
        {
            normalized = "";
            return false;
        }

        normalized = phase.Trim().ToUpperInvariant();
        return true;
    }

    private static string Normalize(string phase) => phase.Trim().ToUpperInvariant();
}

public enum NodePhaseCategory
{
    // Absent, legacy, or freeform token -- accepted as-is (never rejected).
    Legacy = 0,

    // A recognized roadmap phase P0..P5 (valid on any node type).
    RoadmapPhase = 1,

    // A recognized strategic-wave label SP1/SP2/SP3 (canonically feature-level only).
    StrategicWave = 2
}

public sealed record NodePhaseClassification(
    NodePhaseCategory Category,
    string? Phase,
    bool IsFeatureLevel,
    bool StrategicWaveMisplaced);
