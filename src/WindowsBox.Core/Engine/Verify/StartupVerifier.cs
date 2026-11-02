using WindowsBox.Core.Abstractions;
using WindowsBox.Core.Engine.Drift;
using WindowsBox.Core.Engine.Journal;

namespace WindowsBox.Core.Engine.Verify;

/// <summary>
/// Runs on app open (and `wbox verify --quiet`). Confirms applied tweaks stuck
/// and surfaces Windows Update reverts. No resident agent — caller invokes once.
/// </summary>
public sealed class StartupVerifier
{
    private readonly IJournalStore _journals;
    private readonly ApplyVerifier _applyVerifier;
    private readonly DriftDetector _drift;

    public StartupVerifier(IJournalStore journals, ApplyVerifier applyVerifier, DriftDetector drift)
    {
        _journals = journals;
        _applyVerifier = applyVerifier;
        _drift = drift;
    }

    public static StartupVerifier Create(IJournalStore journals, IServiceController services) =>
        new(journals, new ApplyVerifier(services), new DriftDetector(services));

    public async Task<StartupVerifyReport> RunAsync(
        bool quiet = false,
        CancellationToken cancellationToken = default)
    {
        var journals = await _journals.ListAsync(cancellationToken);
        var drift = _drift.Inspect(journals);

        var latestApplied = journals
            .SelectMany(j => j.Entries.Select(e => (Journal: j, Entry: e)))
            .Where(x => x.Entry.Status == JournalEntryStatus.Applied)
            .GroupBy(x => x.Entry.ServiceName, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(x => x.Journal.CreatedAt).First().Journal)
            .DistinctBy(j => j.Id)
            .ToList();

        var verifyResults = latestApplied.Select(_applyVerifier.Verify).ToList();
        var stuckFailures = verifyResults.SelectMany(v => v.Items.Where(i => i.Relevant && !i.Passed)).ToList();

        return new StartupVerifyReport
        {
            RanAt = DateTimeOffset.UtcNow,
            Quiet = quiet,
            Drift = drift,
            JournalsInspected = journals.Count,
            StuckFailures = stuckFailures.Count,
            Passed = !drift.HasDrift && stuckFailures.Count == 0,
            Summary = BuildSummary(drift, stuckFailures.Count, journals.Count),
        };
    }

    private static string BuildSummary(DriftReport drift, int stuckFailures, int journals)
    {
        if (journals == 0)
        {
            return "No journals yet — nothing to verify.";
        }

        if (!drift.HasDrift && stuckFailures == 0)
        {
            return $"All {drift.Matched.Count} tracked service(s) match journal targets.";
        }

        return
            $"Drift: {drift.Drifted.Count} reverted, {drift.Missing.Count} missing; " +
            $"stuck-check failures: {stuckFailures}. Re-apply is opt-in.";
    }
}

public sealed class StartupVerifyReport
{
    public required DateTimeOffset RanAt { get; init; }

    public required bool Quiet { get; init; }

    public required DriftReport Drift { get; init; }

    public required int JournalsInspected { get; init; }

    public required int StuckFailures { get; init; }

    public required bool Passed { get; init; }

    public required string Summary { get; init; }
}
