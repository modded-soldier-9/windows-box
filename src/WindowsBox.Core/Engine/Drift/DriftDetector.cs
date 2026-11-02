using WindowsBox.Core.Abstractions;
using WindowsBox.Core.Catalog.Models;
using WindowsBox.Core.Engine.Journal;

namespace WindowsBox.Core.Engine.Drift;

/// <summary>
/// Compares journaled targets to live SCM state. Used on app open / `wbox verify`
/// to surface Windows Update (or other) reverts — never as a resident agent.
/// </summary>
public sealed class DriftDetector
{
    private readonly IServiceController _services;

    public DriftDetector(IServiceController services)
    {
        _services = services;
    }

    public DriftReport Inspect(IEnumerable<ApplyJournal> journals)
    {
        // Latest applied target per service wins.
        var intended = new Dictionary<string, IntendedState>(StringComparer.OrdinalIgnoreCase);

        foreach (var journal in journals.OrderBy(j => j.CreatedAt))
        {
            foreach (var entry in journal.Entries)
            {
                if (entry.Status is not (JournalEntryStatus.Applied or JournalEntryStatus.Pending))
                {
                    continue;
                }

                intended[entry.ServiceName] = new IntendedState(
                    entry.ItemId,
                    entry.ServiceName,
                    entry.TargetStartType,
                    entry.TargetDelayedAutostart,
                    journal.Id,
                    journal.CreatedAt);
            }
        }

        var drifted = new List<DriftItem>();
        var matched = new List<DriftItem>();
        var missing = new List<DriftItem>();

        foreach (var state in intended.Values)
        {
            var live = _services.GetConfig(state.ServiceName);
            if (live is null || !live.Exists)
            {
                missing.Add(ToItem(state, current: null, reason: "Service missing."));
                continue;
            }

            var current = Normalize(live);
            if (current == state.TargetStartType)
            {
                // Delayed flag only matters for AutomaticDelayed.
                if (state.TargetStartType == ServiceStartType.AutomaticDelayed &&
                    live.DelayedAutostart != state.TargetDelayedAutostart)
                {
                    drifted.Add(ToItem(state, current, "DelayedAutostart mismatch."));
                }
                else
                {
                    matched.Add(ToItem(state, current, "Matches journal target."));
                }
            }
            else
            {
                drifted.Add(ToItem(
                    state,
                    current,
                    "Live start type differs from journal target (possible Windows Update revert)."));
            }
        }

        return new DriftReport
        {
            InspectedAt = DateTimeOffset.UtcNow,
            Matched = matched,
            Drifted = drifted,
            Missing = missing,
        };
    }

    private static DriftItem ToItem(IntendedState state, ServiceStartType? current, string reason) =>
        new()
        {
            ItemId = state.ItemId,
            ServiceName = state.ServiceName,
            Expected = state.TargetStartType,
            Actual = current,
            JournalId = state.JournalId,
            JournalCreatedAt = state.JournalCreatedAt,
            Reason = reason,
        };

    private static ServiceStartType Normalize(ServiceConfig live)
    {
        if (live.StartType == ServiceStartType.Automatic && live.DelayedAutostart)
        {
            return ServiceStartType.AutomaticDelayed;
        }

        if (live.StartType == ServiceStartType.Manual && live.HasTriggers)
        {
            return ServiceStartType.ManualTrigger;
        }

        return live.StartType;
    }

    private sealed record IntendedState(
        string ItemId,
        string ServiceName,
        ServiceStartType TargetStartType,
        bool TargetDelayedAutostart,
        string JournalId,
        DateTimeOffset JournalCreatedAt);
}

public sealed class DriftReport
{
    public required DateTimeOffset InspectedAt { get; init; }

    public required IReadOnlyList<DriftItem> Matched { get; init; }

    public required IReadOnlyList<DriftItem> Drifted { get; init; }

    public required IReadOnlyList<DriftItem> Missing { get; init; }

    public bool HasDrift => Drifted.Count > 0 || Missing.Count > 0;
}

public sealed class DriftItem
{
    public required string ItemId { get; init; }

    public required string ServiceName { get; init; }

    public required ServiceStartType Expected { get; init; }

    public ServiceStartType? Actual { get; init; }

    public required string JournalId { get; init; }

    public required DateTimeOffset JournalCreatedAt { get; init; }

    public required string Reason { get; init; }
}
