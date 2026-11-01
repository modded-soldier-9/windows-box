using WindowsBox.Core.Abstractions;
using WindowsBox.Core.Catalog.Models;

namespace WindowsBox.Tests.Fakes;

public sealed class FakeServiceController : IServiceController
{
    private readonly Dictionary<string, ServiceConfig> _services =
        new(StringComparer.OrdinalIgnoreCase);

    public void Seed(ServiceConfig config) => _services[config.Name] = config;

    public void Seed(
        string name,
        ServiceStartType startType,
        bool delayed = false,
        bool triggers = false,
        bool running = false,
        IEnumerable<string>? dependsOn = null,
        IEnumerable<string>? dependents = null)
    {
        Seed(new ServiceConfig
        {
            Name = name,
            StartType = startType,
            DelayedAutostart = delayed,
            HasTriggers = triggers,
            IsRunning = running,
            DependOnService = dependsOn?.ToList() ?? [],
            DependentServices = dependents?.ToList() ?? [],
            Exists = true,
            DisplayName = name,
        });
    }

    public bool Exists(string serviceName) =>
        _services.TryGetValue(serviceName, out var cfg) && cfg.Exists;

    public ServiceConfig? GetConfig(string serviceName) =>
        _services.TryGetValue(serviceName, out var cfg) ? cfg : null;

    public IReadOnlyList<string> EnumerateServices() => _services.Keys.OrderBy(k => k).ToList();

    /// <summary>Optional hook so tests can cancel mid-apply after snapshot.</summary>
    public Action<string>? BeforeSetStartType { get; set; }

    public void SetStartType(string serviceName, ServiceStartType startType, bool delayedAutostart = false)
    {
        BeforeSetStartType?.Invoke(serviceName);

        if (!_services.TryGetValue(serviceName, out var existing) || !existing.Exists)
        {
            throw new InvalidOperationException($"Service not found: {serviceName}");
        }

        var normalized = startType switch
        {
            ServiceStartType.AutomaticDelayed => ServiceStartType.Automatic,
            ServiceStartType.ManualTrigger => ServiceStartType.Manual,
            _ => startType,
        };

        _services[serviceName] = new ServiceConfig
        {
            Name = existing.Name,
            StartType = normalized,
            DelayedAutostart = delayedAutostart || startType == ServiceStartType.AutomaticDelayed,
            HasTriggers = startType == ServiceStartType.ManualTrigger || existing.HasTriggers,
            IsRunning = existing.IsRunning,
            DependOnService = existing.DependOnService,
            DependentServices = existing.DependentServices,
            DisplayName = existing.DisplayName,
            Exists = true,
        };
    }
}
