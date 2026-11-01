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

    [Fact]
    public async Task Preview_apply_rollback_round_trip_restores_prior_values()
    {
        var services = new FakeServiceController();
        services.Seed("DiagTrack", ServiceStartType.Automatic, running: true);
        services.Seed("TrkWks", ServiceStartType.Automatic);
        services.Seed("WinDefend", ServiceStartType.Automatic);
        services.Seed("RpcSs", ServiceStartType.Automatic);
        services.Seed("BFE", ServiceStartType.Automatic);
        services.Seed("mpssvc", ServiceStartType.Automatic);
        services.Seed("EventLog", ServiceStartType.Automatic);
        services.Seed("PlugPlay", ServiceStartType.Automatic);
        services.Seed("Power", ServiceStartType.Automatic);

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

            var result = await pipeline.ApplyAsync(SampleCatalog(), OptimizationProfile.Privacy);
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
        foreach (var n in new[] { "RpcSs", "BFE", "mpssvc", "EventLog", "PlugPlay", "Power" })
        {
            services.Seed(n, ServiceStartType.Automatic);
        }

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
}
