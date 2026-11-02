using WindowsBox.Core.Abstractions;
using WindowsBox.Core.Catalog.Models;
using WindowsBox.Core.Engine.Journal;

namespace WindowsBox.Core.Engine.Verify;

/// <summary>
/// Confirms applied journal entries still match live SCM state (tweaks "stuck").
/// Distinct from <see cref="Health.BasicHealthChecker"/> which guards critical services.
/// </summary>
public sealed class ApplyVerifier
{
    private readonly IServiceController _services;

    public ApplyVerifier(IServiceController services)
    {
        _services = services;
    }

    public VerifyResult Verify(ApplyJournal journal)
    {
        var items = new List<VerifyItem>();

        foreach (var entry in journal.Entries)
        {
            if (entry.Status is JournalEntryStatus.Skipped or JournalEntryStatus.RolledBack)
            {
                items.Add(new VerifyItem
                {
                    ItemId = entry.ItemId,
                    ServiceName = entry.ServiceName,
                    Passed = true,
                    Detail = $"n/a ({entry.Status})",
                    Relevant = false,
                });
                continue;
            }

            var live = _services.GetConfig(entry.ServiceName);
            if (live is null || !live.Exists)
            {
                items.Add(new VerifyItem
                {
                    ItemId = entry.ItemId,
                    ServiceName = entry.ServiceName,
                    Passed = false,
                    Detail = "Service missing after apply.",
                    Relevant = true,
                });
                continue;
            }

            var current = Normalize(live);
            // Applied (and Pending mid-crash) should match the target; Failed should remain at prior.
            var expected = entry.Status == JournalEntryStatus.Failed
                ? entry.PriorStartType
                : entry.TargetStartType;

            var ok = current == expected;
            items.Add(new VerifyItem
            {
                ItemId = entry.ItemId,
                ServiceName = entry.ServiceName,
                Passed = ok,
                Detail = ok
                    ? $"ok ({current})"
                    : $"expected {expected}, live {current}",
                Expected = expected,
                Actual = current,
                Relevant = true,
            });
        }

        var relevant = items.Where(i => i.Relevant).ToList();
        return new VerifyResult
        {
            JournalId = journal.Id,
            Passed = relevant.Count == 0 || relevant.All(i => i.Passed),
            Items = items,
        };
    }

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
}

public sealed class VerifyResult
{
    public required string JournalId { get; init; }

    public required bool Passed { get; init; }

    public required IReadOnlyList<VerifyItem> Items { get; init; }
}

public sealed class VerifyItem
{
    public required string ItemId { get; init; }

    public required string ServiceName { get; init; }

    public required bool Passed { get; init; }

    public string? Detail { get; init; }

    public ServiceStartType? Expected { get; init; }

    public ServiceStartType? Actual { get; init; }

    public bool Relevant { get; init; } = true;
}
