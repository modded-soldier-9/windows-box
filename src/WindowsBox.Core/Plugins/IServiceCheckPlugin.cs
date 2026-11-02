using WindowsBox.Core.Abstractions;
using WindowsBox.Core.Catalog.Models;

namespace WindowsBox.Core.Plugins;

/// <summary>
/// Extension point for additional service checks without forking Windows Box.
/// Plugins have no sandbox — only reviewed plugins merged into this repository
/// are installable (enforced by <see cref="PluginAllowList"/>).
/// </summary>
public interface IServiceCheckPlugin
{
    string Id { get; }

    string DisplayName { get; }

    string Description { get; }

    IReadOnlyList<ServiceCheckFinding> Evaluate(
        CatalogDocument catalog,
        IServiceController services,
        ISystemProbe probe);
}

public sealed class ServiceCheckFinding
{
    public required string ItemId { get; init; }

    public required string ServiceName { get; init; }

    public required string Summary { get; init; }

    public int Severity { get; init; }
}
