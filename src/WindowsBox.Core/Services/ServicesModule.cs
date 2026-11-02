using WindowsBox.Core.Catalog;
using WindowsBox.Core.Catalog.Models;
using WindowsBox.Core.Engine;
using WindowsBox.Core.Engine.Preview;

namespace WindowsBox.Core.Services;

/// <summary>
/// Flagship services module: Manual-not-Disabled, policy route for protected
/// services, per-user template warnings, profiles, risk 0–4.
/// </summary>
public sealed class ServicesModule
{
    private readonly ApplyPipeline _pipeline;
    private readonly CatalogDocument _applyCatalog;
    private readonly CatalogDocument _inventory;

    public ServicesModule(
        ApplyPipeline pipeline,
        CatalogDocument applyCatalog,
        CatalogDocument? fullInventory = null)
    {
        _pipeline = pipeline;
        _applyCatalog = applyCatalog;
        _inventory = fullInventory ?? applyCatalog;
    }

    public static ServicesModule Load(
        ApplyPipeline pipeline,
        string yamlCatalogDir,
        string? fullInventoryPath = null)
    {
        var apply = new ServiceCatalogLoader().LoadDirectory(yamlCatalogDir);
        CatalogDocument? inventory = null;
        if (!string.IsNullOrWhiteSpace(fullInventoryPath) && File.Exists(fullInventoryPath))
        {
            inventory = new FullServicesInventoryLoader().LoadFile(fullInventoryPath);
        }

        return new ServicesModule(pipeline, apply, inventory);
    }

    public int ApplyReadyCount => _applyCatalog.Services.Count;

    public int InventoryCount => _inventory.Services.Count;

    public PreviewPlan Preview(OptimizationProfile profile) =>
        _pipeline.DetectAndPreview(_applyCatalog, profile);

    public IReadOnlyList<ServiceAdvice> Advise(OptimizationProfile profile)
    {
        var list = new List<ServiceAdvice>();
        foreach (var entry in _inventory.Services)
        {
            if (entry.TouchPolicy == TouchPolicy.Never)
            {
                list.Add(Advice(entry, "Leave alone (never touch).", actionable: false));
                continue;
            }

            if (entry.TouchPolicy == TouchPolicy.PolicyOnly)
            {
                var alt = entry.Alternatives.FirstOrDefault()?.Description
                          ?? "Use policy alternative — service is protected.";
                list.Add(Advice(entry, alt, actionable: false, policyRoute: true));
                continue;
            }

            if (entry.PerUser)
            {
                list.Add(Advice(
                    entry,
                    "Per-user template: change the template key; running instances keep the old config until recreated. Feature may break for signed-in users.",
                    actionable: true,
                    perUser: true));
                continue;
            }

            if (!entry.Recommend.TryGetValue(profile, out var target))
            {
                list.Add(Advice(entry, $"No recommendation for {profile}.", actionable: false));
                continue;
            }

            if (target == ServiceStartType.Disabled && entry.Risk > 2)
            {
                target = ServiceStartType.Manual;
            }

            list.Add(Advice(
                entry,
                $"Recommend {target} for {profile} (risk {entry.Risk}).",
                actionable: entry.TouchPolicy == TouchPolicy.Allow,
                target: target));
        }

        return list;
    }

    public IReadOnlyList<ServiceCatalogEntry> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return _inventory.Services;
        }

        return _inventory.Services
            .Where(s =>
                s.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                s.Display.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                s.Id.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                s.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static ServiceAdvice Advice(
        ServiceCatalogEntry entry,
        string message,
        bool actionable,
        bool policyRoute = false,
        bool perUser = false,
        ServiceStartType? target = null) =>
        new()
        {
            ItemId = entry.Id,
            Name = entry.Name,
            Display = entry.Display,
            Risk = entry.Risk,
            TouchPolicy = entry.TouchPolicy,
            Message = message,
            Actionable = actionable,
            PolicyRoute = policyRoute,
            PerUser = perUser,
            Target = target,
            HonestyLabel = entry.HonestyLabel,
        };
}

public sealed class ServiceAdvice
{
    public required string ItemId { get; init; }

    public required string Name { get; init; }

    public required string Display { get; init; }

    public required int Risk { get; init; }

    public required TouchPolicy TouchPolicy { get; init; }

    public required string Message { get; init; }

    public bool Actionable { get; init; }

    public bool PolicyRoute { get; init; }

    public bool PerUser { get; init; }

    public ServiceStartType? Target { get; init; }

    public string? HonestyLabel { get; init; }
}
