using WindowsBox.Core.Abstractions;
using WindowsBox.Core.Catalog.Models;
using WindowsBox.Core.Engine.Health;
using WindowsBox.Core.Engine.Journal;
using WindowsBox.Core.Engine.Preview;

namespace WindowsBox.Core.Engine;

public sealed class ApplyPipeline
{
    private readonly IServiceController _services;
    private readonly IRegistry _registry;
    private readonly ISystemProbe _probe;
    private readonly IJournalStore _journals;
    private readonly IHealthChecker _health;
    private readonly PreviewPlanner _planner;

    public ApplyPipeline(
        IServiceController services,
        IRegistry registry,
        ISystemProbe probe,
        IJournalStore journals,
        IHealthChecker? health = null)
    {
        _services = services;
        _registry = registry;
        _probe = probe;
        _journals = journals;
        _health = health ?? new BasicHealthChecker(services);
        _planner = new PreviewPlanner(services, probe);
    }

    public PreviewPlan DetectAndPreview(CatalogDocument catalog, OptimizationProfile profile) =>
        _planner.Build(catalog, profile);

    public async Task<ApplyResult> ApplyAsync(
        CatalogDocument catalog,
        OptimizationProfile profile,
        bool dryRun = false,
        IEnumerable<string>? onlyItemIds = null,
        CancellationToken cancellationToken = default)
    {
        var plan = _planner.Build(catalog, profile);
        var selected = onlyItemIds is null
            ? plan.Changes
            : plan.Changes.Where(c => onlyItemIds.Contains(c.ItemId, StringComparer.OrdinalIgnoreCase)).ToList();

        var probe = _probe.Probe();
        var journalId = Guid.NewGuid().ToString("N");
        var entries = new List<JournalEntry>();

        foreach (var change in selected)
        {
            var live = _services.GetConfig(change.ServiceName)
                       ?? throw new InvalidOperationException($"Service vanished: {change.ServiceName}");

            string? regExport = null;
            try
            {
                regExport = _registry.ExportKey(
                    RegistryHive.LocalMachine,
                    $@"SYSTEM\CurrentControlSet\Services\{change.ServiceName}");
            }
            catch (Exception ex)
            {
                regExport = $"# export failed: {ex.Message}";
            }

            entries.Add(new JournalEntry
            {
                ItemId = change.ItemId,
                ServiceName = change.ServiceName,
                PriorStartType = change.Current,
                PriorDelayedAutostart = live.DelayedAutostart,
                PriorHasTriggers = live.HasTriggers,
                PriorWasRunning = live.IsRunning,
                TargetStartType = change.Target,
                TargetDelayedAutostart = change.Target == ServiceStartType.AutomaticDelayed,
                RegistryExport = regExport,
                Status = JournalEntryStatus.Pending,
            });
        }

        var journal = new ApplyJournal
        {
            Id = journalId,
            CreatedAt = DateTimeOffset.UtcNow,
            Profile = profile.ToString(),
            MachineBuild = probe.Build.ToString(),
            Edition = probe.Edition,
            Entries = entries,
            Notes = dryRun ? "dry-run" : null,
        };

        if (dryRun)
        {
            foreach (var entry in entries)
            {
                entry.Status = JournalEntryStatus.Skipped;
            }

            await _journals.SaveAsync(journal, cancellationToken);
            return new ApplyResult
            {
                Journal = journal,
                DryRun = true,
                Health = _health.Run(),
            };
        }

        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                _services.SetStartType(
                    entry.ServiceName,
                    entry.TargetStartType,
                    delayedAutostart: entry.TargetDelayedAutostart);
                entry.Status = JournalEntryStatus.Applied;
            }
            catch (Exception ex)
            {
                entry.Status = JournalEntryStatus.Failed;
                entry.Error = ex.Message;
            }
        }

        await _journals.SaveAsync(journal, cancellationToken);
        var health = _health.Run();

        return new ApplyResult
        {
            Journal = journal,
            DryRun = false,
            Health = health,
        };
    }

    public async Task<ApplyJournal> RollbackAsync(
        string journalId,
        CancellationToken cancellationToken = default)
    {
        var journal = await _journals.LoadAsync(journalId, cancellationToken)
                      ?? throw new InvalidOperationException($"Journal not found: {journalId}");

        foreach (var entry in journal.Entries)
        {
            if (entry.Status is not (JournalEntryStatus.Applied or JournalEntryStatus.Failed))
            {
                continue;
            }

            try
            {
                _services.SetStartType(
                    entry.ServiceName,
                    entry.PriorStartType,
                    delayedAutostart: entry.PriorDelayedAutostart);
                entry.Status = JournalEntryStatus.RolledBack;
                entry.Error = null;
            }
            catch (Exception ex)
            {
                entry.Status = JournalEntryStatus.Failed;
                entry.Error = $"Rollback failed: {ex.Message}";
            }
        }

        await _journals.SaveAsync(journal, cancellationToken);
        return journal;
    }
}

public sealed class ApplyResult
{
    public required ApplyJournal Journal { get; init; }

    public bool DryRun { get; init; }

    public required HealthCheckResult Health { get; init; }
}
