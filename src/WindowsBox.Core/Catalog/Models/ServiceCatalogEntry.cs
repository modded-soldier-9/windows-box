namespace WindowsBox.Core.Catalog.Models;

public sealed class ServiceCatalogEntry
{
    public int SchemaVersion { get; init; } = 1;

    public required string Id { get; init; }

    public string Area { get; init; } = "services";

    public required string Name { get; init; }

    public required string Display { get; init; }

    public required string Category { get; init; }

    public required string Description { get; init; }

    public required IReadOnlyDictionary<string, ServiceStartType> Defaults { get; init; }

    public required string Builds { get; init; }

    public int Risk { get; init; }

    public bool Protected { get; init; }

    public bool PerUser { get; init; }

    public bool Optional { get; init; }

    public TouchPolicy TouchPolicy { get; init; } = TouchPolicy.Allow;

    public IReadOnlyDictionary<OptimizationProfile, ServiceStartType> Recommend { get; init; } =
        new Dictionary<OptimizationProfile, ServiceStartType>();

    public IReadOnlyList<string> DependsOn { get; init; } = [];

    public IReadOnlyList<string> Dependents { get; init; } = [];

    public IReadOnlyList<string> Breaks { get; init; } = [];

    public ServiceGates Gates { get; init; } = new();

    public IReadOnlyList<OptimizationProfile> Profiles { get; init; } = [];

    public IReadOnlyList<PolicyAlternative> Alternatives { get; init; } = [];

    public string? Notes { get; init; }

    public string? HonestyLabel { get; init; }

    public IReadOnlyList<string> Sources { get; init; } = [];

    public string? SourcePath { get; init; }
}

public sealed class ServiceGates
{
    public IReadOnlyList<string> Not { get; init; } = [];

    public IReadOnlyList<string> HardwareAny { get; init; } = [];

    public IReadOnlyList<string> FeatureAny { get; init; } = [];
}

public sealed class PolicyAlternative
{
    public required string Policy { get; init; }

    public string? Value { get; init; }

    public required string Description { get; init; }
}
