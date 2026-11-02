using WindowsBox.Core.Catalog.Models;

namespace WindowsBox.Core.Engine.Preview;

public sealed class PreviewPlan
{
    public required OptimizationProfile Profile { get; init; }

    public required IReadOnlyList<PreviewChange> Changes { get; init; }

    public IReadOnlyList<PreviewSkip> Skipped { get; init; } = [];

    public int EstimatedIdleRamSavedMb { get; init; }

    /// <summary>Human-readable one-line summary of the diff (counts by target).</summary>
    public string DiffSummary { get; init; } = string.Empty;

    public string HonestyNote { get; init; } =
        "Idle RAM estimates are provisional until measured on-device. Honesty labels mark unproven claims.";
}

public sealed class PreviewChange
{
    public required string ItemId { get; init; }

    public required string ServiceName { get; init; }

    public required string Display { get; init; }

    public required ServiceStartType Current { get; init; }

    public required ServiceStartType Target { get; init; }

    /// <summary>Risk 0 (never) … 4 (dangerous).</summary>
    public int Risk { get; init; }

    /// <summary>Provisional expected idle RAM gain in MB for this single change.</summary>
    public int ExpectedGainMb { get; init; }

    public IReadOnlyList<string> Breaks { get; init; } = [];

    public bool RequiresConfirm { get; init; }

    public string? HonestyLabel { get; init; }

    public string DiffLine => $"{ServiceName}: {Current} → {Target} (risk {Risk}, ~{ExpectedGainMb} MB)";
}

public sealed class PreviewSkip
{
    public required string ItemId { get; init; }

    public required string ServiceName { get; init; }

    public required string Reason { get; init; }
}
