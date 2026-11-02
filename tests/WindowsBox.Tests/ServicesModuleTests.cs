using WindowsBox.Core.Catalog;
using WindowsBox.Core.Catalog.Models;
using WindowsBox.Core.Engine;
using WindowsBox.Core.Engine.Journal;
using WindowsBox.Core.Services;
using WindowsBox.Tests.Fakes;

namespace WindowsBox.Tests;

public class ServicesModuleTests
{
    private static string CatalogDir =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "catalog", "services"));

    private static string InventoryPath =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs", "catalogs", "services-full.json"));

    [Fact]
    public void Full_inventory_loads_about_290_services()
    {
        Assert.True(File.Exists(InventoryPath), InventoryPath);
        var inventory = new FullServicesInventoryLoader().LoadFile(InventoryPath);
        Assert.True(inventory.Services.Count >= 280, $"got {inventory.Services.Count}");
        Assert.Contains(inventory.Services, s => s.PerUser);
        Assert.Contains(inventory.Services, s => s.TouchPolicy == TouchPolicy.PolicyOnly);
        Assert.Contains(inventory.Services, s => s.HonestyLabel == "unproven");
    }

    [Fact]
    public void Services_module_advises_policy_route_for_protected()
    {
        var services = new FakeServiceController();
        var pipeline = new ApplyPipeline(
            services,
            new FakeRegistry(),
            new FakeSystemProbe(),
            new FileJournalStore(Path.Combine(Path.GetTempPath(), "wbox-svc-" + Guid.NewGuid().ToString("N"))));

        var module = ServicesModule.Load(pipeline, CatalogDir, InventoryPath);
        Assert.True(module.InventoryCount >= 280);
        Assert.True(module.ApplyReadyCount >= 80);

        var advice = module.Advise(OptimizationProfile.Privacy);
        Assert.Contains(advice, a => a.PolicyRoute);
        Assert.Contains(advice, a => a.PerUser);
        Assert.DoesNotContain(
            advice.Where(a => a.Actionable && a.Target == ServiceStartType.Disabled),
            a => a.Risk > 2);
    }

    [Fact]
    public void Search_finds_diagtrack()
    {
        var pipeline = new ApplyPipeline(
            new FakeServiceController(),
            new FakeRegistry(),
            new FakeSystemProbe(),
            new FileJournalStore(Path.Combine(Path.GetTempPath(), "wbox-search-" + Guid.NewGuid().ToString("N"))));
        var module = ServicesModule.Load(pipeline, CatalogDir, InventoryPath);
        var hits = module.Search("diagtrack");
        Assert.NotEmpty(hits);
    }
}
