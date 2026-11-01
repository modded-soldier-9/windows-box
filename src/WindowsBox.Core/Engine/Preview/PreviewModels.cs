using WindowsBox.Core.Catalog.Models;

namespace WindowsBox.Core.Engine.Preview;

public sealed class PreviewPlan
{
    public required OptimizationProfile Profile { get; init; }

    public required IReadOnlyList<PreviewChange> Changes { get; init; }

    public IReadOnlyList<PreviewSkip> Skipped { get; init; } = [];

    public int EstimatedIdleRamSavedMb { get; init; }

    public string HonestyNote { get; init; } =
        "Idle RAM estimates are provisional until measured on-device (M1+).";
}

public sealed class PreviewChange
{
    public required string ItemId { get; init; }

    public required string ServiceName { get; init; }

    public required string Display { get; init; }

    public required ServiceStartType Current { get; init; }

    public required ServiceStartType Target { get; init; }

    public int Risk { get; init; }

    public IReadOnlyList<string> Breaks { get; init; } = [];

    public bool RequiresConfirm { get; init; }

    public string? HonestyLabel { get; init; }
}

public sealed class PreviewSkip
{
    public required string ItemId { get; init; }

    public required string ServiceName { get; init; }

    public required string Reason { get; init; }
}
