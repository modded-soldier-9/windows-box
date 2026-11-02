using WindowsBox.Core.Catalog.Models;
using WindowsBox.Core.Engine;
using WindowsBox.Core.Engine.Journal;
using WindowsBox.Core.Engine.Undo;
using WindowsBox.Core.Engine.Verify;
using WindowsBox.Tests.Fakes;

namespace WindowsBox.Tests;

public class UndoLayerTests
{
    private static CatalogDocument Catalog() =>
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
    public async Task Undo_layer_records_priors_and_restores_via_batch()
    {
        var services = new FakeServiceController();
        services.Seed("TrkWks", ServiceStartType.Automatic);
        SeedCritical(services);
        var registry = new FakeRegistry();

        var journalDir = Temp("j");
        var undoDir = Temp("u");
        try
        {
            var undo = new FileUndoLayer(undoDir, services, registry);
            var pipeline = new ApplyPipeline(
                services,
                registry,
                new FakeSystemProbe(),
                new FileJournalStore(journalDir),
                undo: undo);

            var result = await pipeline.ApplyAsync(Catalog(), OptimizationProfile.Balanced);
            Assert.NotNull(result.UndoBatch);
            Assert.All(result.UndoBatch.Changes, c => Assert.True(c.CanUndo));
            Assert.Equal(ServiceStartType.Manual, services.GetConfig("TrkWks")!.StartType);

            var undoResult = await undo.UndoAsync(result.UndoBatch.Id);
            Assert.True(undoResult.Passed);
            Assert.Equal(ServiceStartType.Automatic, services.GetConfig("TrkWks")!.StartType);
        }
        finally
        {
            Cleanup(journalDir, undoDir);
        }
    }

    [Fact]
    public async Task Startup_verifier_flags_wu_style_revert_quietly()
    {
        var services = new FakeServiceController();
        services.Seed("TrkWks", ServiceStartType.Automatic);
        SeedCritical(services);

        var journalDir = Temp("sv");
        try
        {
            var store = new FileJournalStore(journalDir);
            var pipeline = new ApplyPipeline(services, new FakeRegistry(), new FakeSystemProbe(), store);
            await pipeline.ApplyAsync(Catalog(), OptimizationProfile.Balanced);

            services.Seed("TrkWks", ServiceStartType.Automatic); // WU revert

            var report = await StartupVerifier.Create(store, services).RunAsync(quiet: true);
            Assert.False(report.Passed);
            Assert.True(report.Drift.HasDrift);
            Assert.Contains("reverted", report.Summary, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Cleanup(journalDir);
        }
    }

    [Fact]
    public void Change_record_rejects_empty_prior_as_non_undoable()
    {
        var record = new ChangeRecord
        {
            ItemId = "x",
            Kind = ChangeKind.RegistryValue,
            Target = @"HKLM\Software\Test!Flag",
            PriorValue = "   ",
            TargetValue = "1",
            IsOneWay = false,
        };

        Assert.False(record.CanUndo);
    }

    private static string Temp(string tag)
    {
        var path = Path.Combine(Path.GetTempPath(), $"wbox-undo-{tag}-" + Guid.NewGuid().ToString("N"));
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
