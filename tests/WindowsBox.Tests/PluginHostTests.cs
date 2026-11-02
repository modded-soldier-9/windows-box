using WindowsBox.Core.Catalog.Models;
using WindowsBox.Core.Plugins;
using WindowsBox.Core.Plugins.Builtin;
using WindowsBox.Tests.Fakes;

namespace WindowsBox.Tests;

public class PluginHostTests
{
    [Fact]
    public void Host_rejects_plugins_not_on_allow_list()
    {
        var host = new PluginHost(new PluginAllowList(["plugin.other"]));
        Assert.False(host.TryRegister(new ProtectedServiceDriftPlugin()));
        Assert.Empty(host.Registered);
    }

    [Fact]
    public void Deny_list_disables_allowed_plugin_fast()
    {
        var dir = Path.Combine(Path.GetTempPath(), "wbox-pl-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var deny = Path.Combine(dir, "deny.txt");
            File.WriteAllText(deny, "# kill switch\n" + ProtectedServiceDriftPlugin.PluginId + "\n");
            var allow = PluginAllowList.FromDenyListFile(
                [ProtectedServiceDriftPlugin.PluginId],
                deny);
            var host = new PluginHost(allow);
            Assert.False(host.TryRegister(new ProtectedServiceDriftPlugin()));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Builtin_plugin_reports_missing_protected_service()
    {
        var host = new PluginHost(new PluginAllowList([ProtectedServiceDriftPlugin.PluginId]));
        Assert.True(host.TryRegister(new ProtectedServiceDriftPlugin()));

        var catalog = new CatalogDocument
        {
            Services =
            [
                new ServiceCatalogEntry
                {
                    Id = "svc.windefend",
                    Name = "WinDefend",
                    Display = "Windows Defender",
                    Category = "security",
                    Description = "Defender",
                    Defaults = new Dictionary<string, ServiceStartType>
                    {
                        ["pro"] = ServiceStartType.Automatic,
                    },
                    Builds = ">=22621",
                    Risk = 0,
                    TouchPolicy = TouchPolicy.Never,
                    Recommend = new Dictionary<OptimizationProfile, ServiceStartType>(),
                    Sources = ["https://example.test"],
                },
            ],
        };

        var services = new FakeServiceController(); // empty — WinDefend missing
        var findings = host.RunAll(catalog, services, new FakeSystemProbe());
        Assert.Contains(findings, f => f.ServiceName == "WinDefend");
    }
}
