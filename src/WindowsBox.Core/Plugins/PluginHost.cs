using WindowsBox.Core.Abstractions;
using WindowsBox.Core.Catalog.Models;

namespace WindowsBox.Core.Plugins;

/// <summary>
/// Hosts in-process service-check plugins. Only allow-listed, non-denied plugins run.
/// There is no sandbox — do not load code that is not merged into this repo.
/// </summary>
public sealed class PluginHost
{
    private readonly PluginAllowList _allowList;
    private readonly List<IServiceCheckPlugin> _plugins = [];

    public PluginHost(PluginAllowList allowList)
    {
        _allowList = allowList;
    }

    public IReadOnlyList<IServiceCheckPlugin> Registered => _plugins;

    public bool TryRegister(IServiceCheckPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        if (!_allowList.IsAllowed(plugin.Id))
        {
            return false;
        }

        if (_plugins.Any(p => string.Equals(p.Id, plugin.Id, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        _plugins.Add(plugin);
        return true;
    }

    public IReadOnlyList<ServiceCheckFinding> RunAll(
        CatalogDocument catalog,
        IServiceController services,
        ISystemProbe probe)
    {
        var findings = new List<ServiceCheckFinding>();
        foreach (var plugin in _plugins)
        {
            findings.AddRange(plugin.Evaluate(catalog, services, probe));
        }

        return findings;
    }
}
