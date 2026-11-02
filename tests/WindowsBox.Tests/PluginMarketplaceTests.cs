using WindowsBox.Core.Plugins;
using WindowsBox.Core.Plugins.Builtin;
using WindowsBox.Core.Settings;

namespace WindowsBox.Tests;

public class PluginMarketplaceTests
{
    [Fact]
    public void Lists_in_repo_manifest_and_installs_when_allowed()
    {
        var root = FindPluginsRoot();
        var state = Path.Combine(Path.GetTempPath(), "wbox-mkt-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(state);
        try
        {
            var store = new FileAppSettingsStore(Path.Combine(state, "cfg"));
            var market = new PluginMarketplace(root, store, state);
            var available = market.ListAvailable();
            Assert.Contains(available, m => m.Id == ProtectedServiceDriftPlugin.PluginId);

            var result = market.Install(ProtectedServiceDriftPlugin.PluginId);
            Assert.True(result.Ok);
            Assert.Contains(ProtectedServiceDriftPlugin.PluginId, market.ListInstalled());
            Assert.True(market.PluginAutoUpdateTiedToAppUpdates);
        }
        finally
        {
            Directory.Delete(state, recursive: true);
        }
    }

    [Fact]
    public void Install_fails_when_deny_listed()
    {
        var root = Path.Combine(Path.GetTempPath(), "wbox-plroot-" + Guid.NewGuid().ToString("N"));
        var pluginDir = Path.Combine(root, "evil");
        Directory.CreateDirectory(pluginDir);
        File.WriteAllText(
            Path.Combine(pluginDir, "plugin.yaml"),
            "id: plugin.evil\nname: Evil\ndescription: no\nversion: 0.0.1\n");
        File.WriteAllText(Path.Combine(root, "deny-list.txt"), "plugin.evil\n");
        var state = Path.Combine(Path.GetTempPath(), "wbox-mkt-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(state);
        try
        {
            var market = new PluginMarketplace(root, new FileAppSettingsStore(Path.Combine(state, "cfg")), state);
            var result = market.Install("plugin.evil");
            Assert.False(result.Ok);
            Assert.Contains("deny", result.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
            Directory.Delete(state, recursive: true);
        }
    }

    private static string FindPluginsRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "plugins");
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "deny-list.txt")))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("plugins/ not found from " + AppContext.BaseDirectory);
    }
}
