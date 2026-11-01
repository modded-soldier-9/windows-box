using WindowsBox.Core.Abstractions;

namespace WindowsBox.Core.Engine.Health;

public sealed class HealthCheckResult
{
    public required bool Passed { get; init; }

    public required IReadOnlyList<HealthCheckItem> Items { get; init; }
}

public sealed class HealthCheckItem
{
    public required string Name { get; init; }

    public required bool Passed { get; init; }

    public string? Detail { get; init; }
}

public interface IHealthChecker
{
    HealthCheckResult Run();
}

/// <summary>
/// M0 skeleton: verifies catalogued never-touch security services still exist/configured.
/// Full network/audio/WU checks land in M1.
/// </summary>
public sealed class BasicHealthChecker : IHealthChecker
{
    private static readonly string[] CriticalServices =
    [
        "RpcSs", "BFE", "mpssvc", "EventLog", "PlugPlay", "Power",
    ];

    private readonly IServiceController _services;

    public BasicHealthChecker(IServiceController services)
    {
        _services = services;
    }

    public HealthCheckResult Run()
    {
        var items = new List<HealthCheckItem>();
        foreach (var name in CriticalServices)
        {
            var cfg = _services.GetConfig(name);
            var ok = cfg is { Exists: true } &&
                     cfg.StartType is Catalog.Models.ServiceStartType.Automatic
                         or Catalog.Models.ServiceStartType.AutomaticDelayed
                         or Catalog.Models.ServiceStartType.Manual
                         or Catalog.Models.ServiceStartType.ManualTrigger;

            items.Add(new HealthCheckItem
            {
                Name = $"service:{name}",
                Passed = ok,
                Detail = ok ? "present" : "missing or unexpected",
            });
        }

        return new HealthCheckResult
        {
            Passed = items.All(i => i.Passed),
            Items = items,
        };
    }
}
