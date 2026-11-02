using WindowsBox.Core.Abstractions;
using WindowsBox.Core.Catalog.Models;
using WindowsBox.Core.Engine;
using WindowsBox.Core.Engine.Journal;
using WindowsBox.Core.Engine.Restore;
using WindowsBox.Tests.Fakes;

namespace WindowsBox.Tests;

public class DriftAndVerifyTests
{
    private static CatalogDocument SingleTrk() =>
        new()
        {
            Services =
            [
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
                    },
                    Sources = ["https://example.com/trkwks"],
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
    public async Task Verify_passes_after_successful_apply()
    {
        var services = new FakeServiceController();
        services.Seed("TrkWks", ServiceStartType.Automatic);
        SeedCritical(services);

        var journalDir = TempDir("verify");
        var backupDir = TempDir("backup");
        try
        {
            var pipeline = new ApplyPipeline(
                services,
                new FakeRegistry(),
                new FakeSystemProbe(),
                new FileJournalStore(journalDir),
                restoreKit: new OfflineRestoreKit(backupDir));

            var result = await pipeline.ApplyAsync(SingleTrk(), OptimizationProfile.Balanced);
            Assert.True(result.Verify.Passed);
            Assert.NotNull(result.Restore);
            Assert.True(File.Exists(result.Restore.CommandFile));
            Assert.True(File.Exists(result.Restore.RegistryFile));
            Assert.Contains("sc.exe config", File.ReadAllText(result.Restore.CommandFile), StringComparison.Ordinal);
        }
        finally
        {
            Cleanup(journalDir, backupDir);
        }
    }

    [Fact]
    public async Task Drift_detector_flags_windows_update_style_revert()
    {
        var services = new FakeServiceController();
        services.Seed("TrkWks", ServiceStartType.Automatic);
        SeedCritical(services);

        var journalDir = TempDir("drift");
        try
        {
            var pipeline = new ApplyPipeline(
                services,
                new FakeRegistry(),
                new FakeSystemProbe(),
                new FileJournalStore(journalDir));

            var result = await pipeline.ApplyAsync(SingleTrk(), OptimizationProfile.Balanced);
            Assert.Equal(ServiceStartType.Manual, services.GetConfig("TrkWks")!.StartType);

            // Simulate Windows Update restoring Automatic.
            services.Seed("TrkWks", ServiceStartType.Automatic);

            var drift = await pipeline.DetectDriftAsync();
            Assert.True(drift.HasDrift);
            Assert.Contains(drift.Drifted, d => d.ServiceName == "TrkWks" && d.Expected == ServiceStartType.Manual);
        }
        finally
        {
            Cleanup(journalDir);
        }
    }

    [Fact]
    public void Preview_includes_risk_gain_and_diff_summary()
    {
        var services = new FakeServiceController();
        services.Seed("TrkWks", ServiceStartType.Automatic);
        services.Seed("DiagTrack", ServiceStartType.Automatic);
        SeedCritical(services);

        var catalog = new CatalogDocument
        {
            Services =
            [
                new ServiceCatalogEntry
                {
                    Id = "svc.trkwks",
                    Name = "TrkWks",
                    Display = "TrkWks",
                    Category = "performance",
                    Description = "x",
                    Defaults = new Dictionary<string, ServiceStartType> { ["pro"] = ServiceStartType.Automatic },
                    Builds = ">=22621",
                    Risk = 1,
                    TouchPolicy = TouchPolicy.Allow,
                    Recommend = new Dictionary<OptimizationProfile, ServiceStartType>
                    {
                        [OptimizationProfile.Privacy] = ServiceStartType.Manual,
                    },
                    Sources = ["https://example.com"],
                },
                new ServiceCatalogEntry
                {
                    Id = "svc.diagtrack",
                    Name = "DiagTrack",
                    Display = "Telemetry",
                    Category = "telemetry",
                    Description = "x",
                    Defaults = new Dictionary<string, ServiceStartType> { ["pro"] = ServiceStartType.Automatic },
                    Builds = ">=22621",
                    Risk = 2,
                    TouchPolicy = TouchPolicy.Allow,
                    Recommend = new Dictionary<OptimizationProfile, ServiceStartType>
                    {
                        [OptimizationProfile.Privacy] = ServiceStartType.Disabled,
                    },
                    Sources = ["https://example.com"],
                },
            ],
        };

        var pipeline = new ApplyPipeline(
            services,
            new FakeRegistry(),
            new FakeSystemProbe(),
            new FileJournalStore(TempDir("preview")));

        var plan = pipeline.DetectAndPreview(catalog, OptimizationProfile.Privacy);
        Assert.Equal(2, plan.Changes.Count);
        Assert.Contains(plan.Changes, c => c.Risk == 2 && c.ExpectedGainMb == 12);
        Assert.Contains(plan.Changes, c => c.Risk == 1 && c.ExpectedGainMb == 8);
        Assert.Contains("→ Manual", plan.DiffSummary, StringComparison.Ordinal);
        Assert.Contains("→ Disabled", plan.DiffSummary, StringComparison.Ordinal);
        Assert.Equal(20, plan.EstimatedIdleRamSavedMb);
    }

    [Fact]
    public void Platform_adapters_factory_returns_unsupported_off_windows()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var services = WindowsBox.Core.Platform.PlatformAdapters.CreateServiceController();
        Assert.False(services.Exists("TrkWks"));
        Assert.Throws<PlatformNotSupportedException>(() =>
            services.SetStartType("TrkWks", ServiceStartType.Manual));

        var probe = WindowsBox.Core.Platform.PlatformAdapters.CreateSystemProbe();
        Assert.Throws<PlatformNotSupportedException>(() => probe.Probe());
    }

    private static string TempDir(string tag)
    {
        var path = Path.Combine(Path.GetTempPath(), $"wbox-{tag}-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void Cleanup(params string[] dirs)
    {
        foreach (var dir in dirs)
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }
}
