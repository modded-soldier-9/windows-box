using WindowsBox.Core.Abstractions;
using WindowsBox.Core.Catalog.Models;

namespace WindowsBox.Core.Plugins.Builtin;

/// <summary>
/// Sample in-repo plugin: flags catalog services with touch_policy never/leave that are missing.
/// Demonstrates the extension point without forking Core.
/// </summary>
public sealed class ProtectedServiceDriftPlugin : IServiceCheckPlugin
{
    public const string PluginId = "plugin.protected-service-drift";

    public string Id => PluginId;

    public string DisplayName => "Protected service presence";

    public string Description =>
        "Reports catalog services marked never/leave that are absent from the live SCM.";

    public IReadOnlyList<ServiceCheckFinding> Evaluate(
        CatalogDocument catalog,
        IServiceController services,
        ISystemProbe probe)
    {
        var findings = new List<ServiceCheckFinding>();
        foreach (var entry in catalog.Services.Where(s =>
                     s.TouchPolicy is TouchPolicy.Never or TouchPolicy.Leave))
        {
            if (!services.Exists(entry.Name))
            {
                findings.Add(new ServiceCheckFinding
                {
                    ItemId = entry.Id,
                    ServiceName = entry.Name,
                    Summary = $"Protected/leave service '{entry.Name}' is not present.",
                    Severity = 2,
                });
            }
        }

        return findings;
    }
}
