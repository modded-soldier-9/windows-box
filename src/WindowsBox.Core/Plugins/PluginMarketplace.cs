using WindowsBox.Core.Plugins.Builtin;
using WindowsBox.Core.Settings;
using YamlDotNet.RepresentationModel;

namespace WindowsBox.Core.Plugins;

public sealed class PluginInstallResult
{
    public required bool Ok { get; init; }

    public required string Message { get; init; }

    public string? PluginId { get; init; }
}

/// <summary>
/// In-repo marketplace: discovers manifests under <c>plugins/</c>, installs only
/// allow-listed ids, honors deny-list, and refreshes remote metadata only when
/// app update checks are opted in.
/// </summary>
public sealed class PluginMarketplace
{
    private readonly string _pluginsRoot;
    private readonly IAppSettingsStore _settings;
    private readonly string _denyListPath;
    private readonly string _installedStatePath;

    public PluginMarketplace(string pluginsRoot, IAppSettingsStore settings, string? stateDirectory = null)
    {
        _pluginsRoot = Path.GetFullPath(pluginsRoot);
        _settings = settings;
        _denyListPath = Path.Combine(_pluginsRoot, "deny-list.txt");
        var stateDir = stateDirectory ?? Path.Combine(Path.GetTempPath(), "WindowsBox", "plugins-state");
        Directory.CreateDirectory(stateDir);
        _installedStatePath = Path.Combine(stateDir, "installed.txt");
    }

    public string DenyListPath => _denyListPath;

    public IReadOnlyList<PluginManifest> ListAvailable()
    {
        if (!Directory.Exists(_pluginsRoot))
        {
            return [];
        }

        var list = new List<PluginManifest>();
        foreach (var dir in Directory.EnumerateDirectories(_pluginsRoot))
        {
            var manifestPath = Path.Combine(dir, "plugin.yaml");
            if (!File.Exists(manifestPath))
            {
                continue;
            }

            var manifest = ReadManifest(manifestPath, dir);
            if (manifest is not null)
            {
                list.Add(manifest);
            }
        }

        return list.OrderBy(m => m.Id, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public PluginAllowList BuildAllowList()
    {
        var allowed = ListAvailable().Select(m => m.Id).Append(ProtectedServiceDriftPlugin.PluginId);
        return PluginAllowList.FromDenyListFile(allowed, _denyListPath);
    }

    public IReadOnlyCollection<string> ListInstalled()
    {
        if (!File.Exists(_installedStatePath))
        {
            return [];
        }

        return File.ReadAllLines(_installedStatePath)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0 && !l.StartsWith('#'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>One-click install: enable an allow-listed in-repo plugin.</summary>
    public PluginInstallResult Install(string pluginId)
    {
        var allow = BuildAllowList();
        if (!allow.IsAllowed(pluginId))
        {
            return new PluginInstallResult
            {
                Ok = false,
                PluginId = pluginId,
                Message = $"Plugin '{pluginId}' is not allow-listed or is deny-listed. Only reviewed in-repo plugins can be installed.",
            };
        }

        var available = ListAvailable().FirstOrDefault(m =>
            string.Equals(m.Id, pluginId, StringComparison.OrdinalIgnoreCase));
        if (available is null && !string.Equals(pluginId, ProtectedServiceDriftPlugin.PluginId, StringComparison.OrdinalIgnoreCase))
        {
            return new PluginInstallResult
            {
                Ok = false,
                PluginId = pluginId,
                Message = $"Plugin '{pluginId}' was not found under plugins/.",
            };
        }

        var installed = ListInstalled().ToHashSet(StringComparer.OrdinalIgnoreCase);
        installed.Add(pluginId);
        File.WriteAllLines(_installedStatePath, installed.OrderBy(x => x));
        return new PluginInstallResult
        {
            Ok = true,
            PluginId = pluginId,
            Message = $"Installed '{pluginId}'. No remote code was downloaded.",
        };
    }

    /// <summary>
    /// Plugin catalog refresh is tied to the app update toggle. When checks are off,
    /// returns the local in-repo manifests only (offline).
    /// </summary>
    public IReadOnlyList<PluginManifest> RefreshCatalog()
    {
        var settings = _settings.Load();
        // Even when update checks are enabled we only re-read the local plugins/ tree —
        // marketplace content arrives via merged PRs, never arbitrary downloads.
        _ = settings.CheckForUpdatesEnabled;
        return ListAvailable();
    }

    public bool PluginAutoUpdateTiedToAppUpdates => true;

    private static PluginManifest? ReadManifest(string path, string sourceDir)
    {
        using var reader = File.OpenText(path);
        var yaml = new YamlStream();
        yaml.Load(reader);
        if (yaml.Documents.Count == 0 || yaml.Documents[0].RootNode is not YamlMappingNode map)
        {
            return null;
        }

        string? Get(string key)
        {
            var kn = new YamlScalarNode(key);
            if (!map.Children.TryGetValue(kn, out var node))
            {
                return null;
            }

            return node switch
            {
                YamlScalarNode s => s.Value,
                _ => node.ToString(),
            };
        }

        var id = Get("id");
        var name = Get("name");
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return new PluginManifest
        {
            Id = id,
            Name = name,
            Description = Get("description")?.Trim() ?? "",
            Version = Get("version") ?? "0.0.0",
            Kind = Get("kind") ?? "service-check",
            Builtin = string.Equals(Get("builtin"), "true", StringComparison.OrdinalIgnoreCase),
            Entry = Get("entry"),
            SourceDirectory = sourceDir,
        };
    }
}
