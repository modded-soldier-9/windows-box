using WindowsBox.Core.Catalog;
using WindowsBox.Core.Catalog.Models;

namespace WindowsBox.Tests;

public class CatalogLoaderTests
{
    private static string CatalogDir =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "catalog", "services"));

    [Fact]
    public void Loads_all_service_yaml_files()
    {
        Assert.True(Directory.Exists(CatalogDir), $"expected catalog at {CatalogDir}");

        var loader = new ServiceCatalogLoader();
        var catalog = loader.LoadDirectory(CatalogDir);

        Assert.True(catalog.Services.Count >= 80, $"expected >= 80 services, got {catalog.Services.Count}");
        Assert.NotNull(catalog.FindById("svc.diagtrack"));
        Assert.NotNull(catalog.FindByName("SysMain"));
    }

    [Fact]
    public void DiagTrack_recommends_Disabled_for_privacy_and_Manual_for_balanced()
    {
        var catalog = new ServiceCatalogLoader().LoadDirectory(CatalogDir);
        var entry = catalog.FindById("svc.diagtrack");
        Assert.NotNull(entry);

        Assert.Equal(2, entry.Risk);
        Assert.Equal(ServiceStartType.Disabled, entry.Recommend[OptimizationProfile.Privacy]);
        Assert.Equal(ServiceStartType.Manual, entry.Recommend[OptimizationProfile.Balanced]);
    }

    [Fact]
    public void Prefer_Manual_not_Disabled_for_high_risk_recommendations()
    {
        var catalog = new ServiceCatalogLoader().LoadDirectory(CatalogDir);

        foreach (var entry in catalog.Services.Where(e => e.TouchPolicy == TouchPolicy.Allow && e.Risk > 2))
        {
            foreach (var (profile, start) in entry.Recommend)
            {
                Assert.True(
                    start != ServiceStartType.Disabled,
                    $"{entry.Id} profile {profile} should prefer Manual at risk {entry.Risk}");
            }
        }
    }

    [Fact]
    public void Security_services_are_never_touch()
    {
        var catalog = new ServiceCatalogLoader().LoadDirectory(CatalogDir);
        foreach (var name in new[] { "WinDefend", "mpssvc", "BFE", "SecurityHealthService" })
        {
            var entry = catalog.FindByName(name);
            Assert.NotNull(entry);
            Assert.Equal(0, entry.Risk);
            Assert.Equal(TouchPolicy.Never, entry.TouchPolicy);
        }
    }

    [Fact]
    public void Rejects_invalid_yaml()
    {
        var dir = Path.Combine(Path.GetTempPath(), "wbox-catalog-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "bad.yaml"), "id: not-valid\n");
            Assert.Throws<CatalogLoadException>(() => new ServiceCatalogLoader().LoadDirectory(dir));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
