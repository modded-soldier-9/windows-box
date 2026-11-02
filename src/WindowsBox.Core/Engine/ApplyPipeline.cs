using WindowsBox.Core.Abstractions;
using WindowsBox.Core.Catalog.Models;
using WindowsBox.Core.Engine.Drift;
using WindowsBox.Core.Engine.Health;
using WindowsBox.Core.Engine.Journal;
using WindowsBox.Core.Engine.Preview;
using WindowsBox.Core.Engine.Restore;
using WindowsBox.Core.Engine.Undo;
using WindowsBox.Core.Engine.Verify;

namespace WindowsBox.Core.Engine;

public sealed class ApplyPipeline
{
    private readonly IServiceController _services;
    private readonly IRegistry _registry;
    private readonly ISystemProbe _probe;
    private readonly IJournalStore _journals;
    private readonly IHealthChecker _health;
    private readonly PreviewPlanner _planner;
    private readonly ApplyVerifier _verifier;
    private readonly DriftDetector _drift;
    private readonly OfflineRestoreKit? _restoreKit;
    private readonly IUndoLayer? _undo;

    public ApplyPipeline(
        IServiceController services,
        IRegistry registry,
        ISystemProbe probe,
        IJournalStore journals,
        IHealthChecker? health = null,
        OfflineRestoreKit? restoreKit = null,
        IUndoLayer? undo = null)
    {
        _services = services;
        _registry = registry;
        _probe = probe;
        _journals = journals;
        _health = health ?? new BasicHealthChecker(services);
        _planner = new PreviewPlanner(services, probe);
        _verifier = new ApplyVerifier(services);
        _drift = new DriftDetector(services);
        _restoreKit = restoreKit;
        _undo = undo;
    }

    public PreviewPlan DetectAndPreview(CatalogDocument catalog, OptimizationProfile profile) =>
        _planner.Build(catalog, profile);

    public async Task<ApplyResult> ApplyAsync(
        CatalogDocument catalog,
        OptimizationProfile profile,
        bool dryRun = false,
        bool confirmHighRisk = false,
        IEnumerable<string>? onlyItemIds = null,
        CancellationToken cancellationToken = default)
    {
        var plan = _planner.Build(catalog, profile);
        var selected = onlyItemIds is null
            ? plan.Changes.ToList()
            : plan.Changes.Where(c => onlyItemIds.Contains(c.ItemId, StringComparer.OrdinalIgnoreCase)).ToList();

        if (!confirmHighRisk && selected.Any(c => c.RequiresConfirm))
        {
            var blocked = selected.Where(c => c.RequiresConfirm).Select(c => c.ItemId);
            throw new InvalidOperationException(
                "High-risk or Disabled changes require confirmHighRisk=true. Blocked: " +
                string.Join(", ", blocked));
        }

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

        // Snapshot first — undo must survive crash/cancel during apply.
        await _journals.SaveAsync(journal, cancellationToken);

        OfflineRestoreArtifacts? restore = null;
        if (!dryRun && _restoreKit is not null)
        {
            restore = _restoreKit.Write(journal);
        }

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
                Verify = _verifier.Verify(journal),
                Restore = restore,
                Plan = plan,
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
            catch (OperationCanceledException)
            {
                await _journals.SaveAsync(journal, CancellationToken.None);
                throw;
            }
            catch (Exception ex)
            {
                entry.Status = JournalEntryStatus.Failed;
                entry.Error = ex.Message;
            }

            await _journals.SaveAsync(journal, cancellationToken);
        }

        var health = _health.Run();
        var verify = _verifier.Verify(journal);

        UndoBatch? undoBatch = null;
        if (_undo is not null)
        {
            undoBatch = _undo.FromJournal(journal);
            await _undo.SaveAsync(undoBatch, cancellationToken);
        }

        return new ApplyResult
        {
            Journal = journal,
            DryRun = false,
            Health = health,
            Verify = verify,
            Restore = restore,
            Plan = plan,
            UndoBatch = undoBatch,
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
            if (entry.Status is not (JournalEntryStatus.Applied or JournalEntryStatus.Failed or JournalEntryStatus.Pending))
            {
                continue;
            }

            // Pending means snapshot existed but apply never completed — restore priors anyway.
            if (entry.Status == JournalEntryStatus.Pending)
            {
                var live = _services.GetConfig(entry.ServiceName);
                if (live is null || Normalize(live) == entry.PriorStartType)
                {
                    entry.Status = JournalEntryStatus.RolledBack;
                    continue;
                }
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

    public VerifyResult VerifyJournal(ApplyJournal journal) => _verifier.Verify(journal);

    public async Task<DriftReport> DetectDriftAsync(CancellationToken cancellationToken = default)
    {
        var journals = await _journals.ListAsync(cancellationToken);
        return _drift.Inspect(journals);
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

public sealed class ApplyResult
{
    public required ApplyJournal Journal { get; init; }

    public bool DryRun { get; init; }

    public required HealthCheckResult Health { get; init; }

    public required VerifyResult Verify { get; init; }

    public OfflineRestoreArtifacts? Restore { get; init; }

    public PreviewPlan? Plan { get; init; }

    public UndoBatch? UndoBatch { get; init; }
}
