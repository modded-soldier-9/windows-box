using WindowsBox.Core.Abstractions;
using WindowsBox.Core.Catalog.Models;
using WindowsBox.Core.Engine;
using WindowsBox.Core.Engine.Journal;
using WindowsBox.Tests.Fakes;

namespace WindowsBox.Tests;

public class ApplyPipelineTests
{
    private static CatalogDocument SampleCatalog() =>
        new()
        {
            Services =
            [
                new ServiceCatalogEntry
                {
                    Id = "svc.diagtrack",
                    Name = "DiagTrack",
                    Display = "Telemetry",
                    Category = "telemetry",
                    Description = "Telemetry",
                    Defaults = new Dictionary<string, ServiceStartType>
                    {
                        ["pro"] = ServiceStartType.Automatic,
                    },
                    Builds = ">=22621",
                    Risk = 2,
                    TouchPolicy = TouchPolicy.Allow,
                    Recommend = new Dictionary<OptimizationProfile, ServiceStartType>
                    {
                        [OptimizationProfile.Privacy] = ServiceStartType.Disabled,
                        [OptimizationProfile.Balanced] = ServiceStartType.Manual,
                    },
                    Sources = ["https://example.com/diagtrack"],
                },
                new ServiceCatalogEntry
                {
                    Id = "svc.trkwks",
                    Name = "TrkWks",
                    Display = "Distributed Link Tracking Client",
                    Category = "performance",
                    Description = "Link tracking",
                    Defaults = new Dictionary<string, ServiceStartType>
                    {
                        ["pro"] = ServiceStartType.Automatic,
                    },
                    Builds = ">=22621",
                    Risk = 1,
                    TouchPolicy = TouchPolicy.Allow,
                    Recommend = new Dictionary<OptimizationProfile, ServiceStartType>
                    {
                        [OptimizationProfile.Balanced] = ServiceStartType.Manual,
                        [OptimizationProfile.Privacy] = ServiceStartType.Manual,
                    },
                    Sources = ["https://example.com/trkwks"],
                },
                new ServiceCatalogEntry
                {
                    Id = "svc.windefend",
                    Name = "WinDefend",
                    Display = "Defender",
                    Category = "security",
                    Description = "Antivirus",
                    Defaults = new Dictionary<string, ServiceStartType>
                    {
                        ["pro"] = ServiceStartType.Automatic,
                    },
                    Builds = ">=22621",
                    Risk = 0,
                    Protected = true,
                    TouchPolicy = TouchPolicy.Never,
                    Sources = ["https://example.com/defender"],
                },
            ],
        };

    private static void SeedCritical(FakeServiceController services)
    {
        foreach (var n in new[] { "RpcSs", "BFE", "mpssvc", "EventLog", "PlugPlay", "Power" })
        {
            services.Seed(n, ServiceStartType.Automatic);
        }
    }

    [Fact]
    public async Task Preview_apply_rollback_round_trip_restores_prior_values()
    {
        var services = new FakeServiceController();
        services.Seed("DiagTrack", ServiceStartType.Automatic, running: true);
        services.Seed("TrkWks", ServiceStartType.Automatic);
        services.Seed("WinDefend", ServiceStartType.Automatic);
        SeedCritical(services);

        var registry = new FakeRegistry();
        registry.Seed(
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Services\DiagTrack",
            "Start",
            2);

        var probe = new FakeSystemProbe();
        var journalDir = Path.Combine(Path.GetTempPath(), "wbox-journals-" + Guid.NewGuid().ToString("N"));
        var store = new FileJournalStore(journalDir);
        var pipeline = new ApplyPipeline(services, registry, probe, store);

        try
        {
            var preview = pipeline.DetectAndPreview(SampleCatalog(), OptimizationProfile.Privacy);
            Assert.Contains(preview.Changes, c => c.ServiceName == "DiagTrack" && c.Target == ServiceStartType.Disabled);
            Assert.Contains(preview.Changes, c => c.ServiceName == "TrkWks" && c.Target == ServiceStartType.Manual);
            Assert.Contains(preview.Skipped, s => s.ServiceName == "WinDefend");

            var result = await pipeline.ApplyAsync(
                SampleCatalog(),
                OptimizationProfile.Privacy,
                confirmHighRisk: true);
            Assert.False(result.DryRun);
            Assert.True(result.Health.Passed);
            Assert.All(result.Journal.Entries, e => Assert.Equal(JournalEntryStatus.Applied, e.Status));

            Assert.Equal(ServiceStartType.Disabled, services.GetConfig("DiagTrack")!.StartType);
            Assert.Equal(ServiceStartType.Manual, services.GetConfig("TrkWks")!.StartType);

            var diag = result.Journal.Entries.Single(e => e.ServiceName == "DiagTrack");
            Assert.Equal(ServiceStartType.Automatic, diag.PriorStartType);
            Assert.Contains("DiagTrack", diag.RegistryExport);

            var rolled = await pipeline.RollbackAsync(result.Journal.Id);
            Assert.All(rolled.Entries, e => Assert.Equal(JournalEntryStatus.RolledBack, e.Status));
            Assert.Equal(ServiceStartType.Automatic, services.GetConfig("DiagTrack")!.StartType);
            Assert.Equal(ServiceStartType.Automatic, services.GetConfig("TrkWks")!.StartType);
        }
        finally
        {
            if (Directory.Exists(journalDir))
            {
                Directory.Delete(journalDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Dry_run_does_not_mutate_services()
    {
        var services = new FakeServiceController();
        services.Seed("TrkWks", ServiceStartType.Automatic);
        SeedCritical(services);

        var pipeline = new ApplyPipeline(
            services,
            new FakeRegistry(),
            new FakeSystemProbe(),
            new FileJournalStore(Path.Combine(Path.GetTempPath(), "wbox-dry-" + Guid.NewGuid().ToString("N"))));

        var result = await pipeline.ApplyAsync(SampleCatalog(), OptimizationProfile.Balanced, dryRun: true);
        Assert.True(result.DryRun);
        Assert.Equal(ServiceStartType.Automatic, services.GetConfig("TrkWks")!.StartType);
    }

    [Fact]
    public void Mdm_gate_skips_diagtrack_disable()
    {
        var services = new FakeServiceController();
        services.Seed("DiagTrack", ServiceStartType.Automatic);

        var catalog = new CatalogDocument
        {
            Services =
            [
                new ServiceCatalogEntry
                {
                    Id = "svc.diagtrack",
                    Name = "DiagTrack",
                    Display = "Telemetry",
                    Category = "telemetry",
                    Description = "Telemetry",
                    Defaults = new Dictionary<string, ServiceStartType> { ["pro"] = ServiceStartType.Automatic },
                    Builds = ">=22621",
                    Risk = 2,
                    TouchPolicy = TouchPolicy.Allow,
                    Recommend = new Dictionary<OptimizationProfile, ServiceStartType>
                    {
                        [OptimizationProfile.Privacy] = ServiceStartType.Disabled,
                    },
                    Gates = new ServiceGates { Not = ["mdm_enrolled"] },
                    Sources = ["https://example.com"],
                },
            ],
        };

        var probe = new FakeSystemProbe
        {
            Result = new SystemProbeResult
            {
                Edition = "Enterprise",
                Build = 26100,
                MdmEnrolled = true,
            },
        };

        var pipeline = new ApplyPipeline(
            services,
            new FakeRegistry(),
            probe,
            new FileJournalStore(Path.Combine(Path.GetTempPath(), "wbox-mdm-" + Guid.NewGuid().ToString("N"))));

        var plan = pipeline.DetectAndPreview(catalog, OptimizationProfile.Privacy);
        Assert.Empty(plan.Changes);
        Assert.Contains(plan.Skipped, s => s.Reason.Contains("mdm_enrolled", StringComparison.Ordinal));
    }

    [Fact]
    public async Task High_risk_changes_require_explicit_confirm()
    {
        var services = new FakeServiceController();
        services.Seed("DiagTrack", ServiceStartType.Automatic);
        SeedCritical(services);

        var pipeline = new ApplyPipeline(
            services,
            new FakeRegistry(),
            new FakeSystemProbe(),
            new FileJournalStore(Path.Combine(Path.GetTempPath(), "wbox-confirm-" + Guid.NewGuid().ToString("N"))));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            pipeline.ApplyAsync(SampleCatalog(), OptimizationProfile.Privacy, confirmHighRisk: false));

        Assert.Contains("confirmHighRisk", ex.Message, StringComparison.Ordinal);
        Assert.Equal(ServiceStartType.Automatic, services.GetConfig("DiagTrack")!.StartType);
    }

    [Fact]
    public async Task Journal_is_persisted_before_apply_mutations()
    {
        var services = new FakeServiceController();
        services.Seed("TrkWks", ServiceStartType.Automatic);
        SeedCritical(services);

        var journalDir = Path.Combine(Path.GetTempPath(), "wbox-snap-" + Guid.NewGuid().ToString("N"));
        var store = new FileJournalStore(journalDir);
        var pipeline = new ApplyPipeline(services, new FakeRegistry(), new FakeSystemProbe(), store);

        services.BeforeSetStartType = _ => throw new OperationCanceledException("cancelled before mutate");

        try
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                pipeline.ApplyAsync(SampleCatalog(), OptimizationProfile.Balanced));

            Assert.Equal(ServiceStartType.Automatic, services.GetConfig("TrkWks")!.StartType);

            var journals = await store.ListAsync();
            Assert.NotEmpty(journals);
            var journal = journals[0];
            var entry = journal.Entries.Single(e => e.ServiceName == "TrkWks");
            Assert.Equal(ServiceStartType.Automatic, entry.PriorStartType);
            Assert.Equal(JournalEntryStatus.Pending, entry.Status);
        }
        finally
        {
            if (Directory.Exists(journalDir))
            {
                Directory.Delete(journalDir, recursive: true);
            }
        }
    }

    [Fact]
    public void Coerces_Disabled_to_Manual_when_risk_above_two()
    {
        var services = new FakeServiceController();
        services.Seed("SysMain", ServiceStartType.Automatic);

        var catalog = new CatalogDocument
        {
            Services =
            [
                new ServiceCatalogEntry
                {
                    Id = "svc.sysmain",
                    Name = "SysMain",
                    Display = "SysMain",
                    Category = "performance",
                    Description = "Prefetch",
                    Defaults = new Dictionary<string, ServiceStartType> { ["pro"] = ServiceStartType.Automatic },
                    Builds = ">=22621",
                    Risk = 3,
                    TouchPolicy = TouchPolicy.Allow,
                    Recommend = new Dictionary<OptimizationProfile, ServiceStartType>
                    {
                        [OptimizationProfile.Gamer] = ServiceStartType.Disabled,
                    },
                    Sources = ["https://example.com"],
                },
            ],
        };

        var pipeline = new ApplyPipeline(
            services,
            new FakeRegistry(),
            new FakeSystemProbe(),
            new FileJournalStore(Path.Combine(Path.GetTempPath(), "wbox-coerce-" + Guid.NewGuid().ToString("N"))));

        var plan = pipeline.DetectAndPreview(catalog, OptimizationProfile.Gamer);
        Assert.Contains(plan.Changes, c => c.ServiceName == "SysMain" && c.Target == ServiceStartType.Manual);
    }

    [Fact]
    public void Blocks_when_automatic_dependent_exists()
    {
        var services = new FakeServiceController();
        services.Seed("Spooler", ServiceStartType.Automatic, dependents: ["PrintNotify"]);
        services.Seed("PrintNotify", ServiceStartType.Automatic, dependsOn: ["Spooler"]);

        var catalog = new CatalogDocument
        {
            Services =
            [
                new ServiceCatalogEntry
                {
                    Id = "svc.spooler",
                    Name = "Spooler",
                    Display = "Print Spooler",
                    Category = "hardware",
                    Description = "Printing",
                    Defaults = new Dictionary<string, ServiceStartType> { ["pro"] = ServiceStartType.Automatic },
                    Builds = ">=22621",
                    Risk = 3,
                    TouchPolicy = TouchPolicy.Allow,
                    Recommend = new Dictionary<OptimizationProfile, ServiceStartType>
                    {
                        [OptimizationProfile.Minimal] = ServiceStartType.Manual,
                    },
                    Sources = ["https://example.com"],
                },
            ],
        };

        var pipeline = new ApplyPipeline(
            services,
            new FakeRegistry(),
            new FakeSystemProbe(),
            new FileJournalStore(Path.Combine(Path.GetTempPath(), "wbox-dep-" + Guid.NewGuid().ToString("N"))));

        var plan = pipeline.DetectAndPreview(catalog, OptimizationProfile.Minimal);
        Assert.Empty(plan.Changes);
        Assert.Contains(plan.Skipped, s => s.Reason.Contains("Dependency", StringComparison.Ordinal));
    }

    [Fact]
    public void Per_user_templates_require_confirm_and_warn()
    {
        var services = new FakeServiceController();
        services.Seed("OneSyncSvc", ServiceStartType.AutomaticDelayed, delayed: true);

        var catalog = new CatalogDocument
        {
            Services =
            [
                new ServiceCatalogEntry
                {
                    Id = "svc.onesyncsvc",
                    Name = "OneSyncSvc",
                    Display = "Sync Host",
                    Category = "privacy",
                    Description = "Sync",
                    Defaults = new Dictionary<string, ServiceStartType> { ["pro"] = ServiceStartType.AutomaticDelayed },
                    Builds = ">=22621",
                    Risk = 3,
                    PerUser = true,
                    TouchPolicy = TouchPolicy.Allow,
                    Recommend = new Dictionary<OptimizationProfile, ServiceStartType>
                    {
                        [OptimizationProfile.Privacy] = ServiceStartType.Manual,
                    },
                    Sources = ["https://example.com"],
                },
            ],
        };

        var pipeline = new ApplyPipeline(
            services,
            new FakeRegistry(),
            new FakeSystemProbe(),
            new FileJournalStore(Path.Combine(Path.GetTempPath(), "wbox-pu-" + Guid.NewGuid().ToString("N"))));

        var plan = pipeline.DetectAndPreview(catalog, OptimizationProfile.Privacy);
        var change = Assert.Single(plan.Changes);
        Assert.True(change.RequiresConfirm);
        Assert.Contains(change.Breaks, b => b.Contains("Per-user template", StringComparison.Ordinal));
    }
}
