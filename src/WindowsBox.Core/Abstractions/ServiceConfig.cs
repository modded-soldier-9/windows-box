using WindowsBox.Core.Catalog.Models;

namespace WindowsBox.Core.Abstractions;

/// <summary>
/// Live SCM configuration snapshot for a service.
/// </summary>
public sealed class ServiceConfig
{
    public required string Name { get; init; }

    public required ServiceStartType StartType { get; init; }

    public bool DelayedAutostart { get; init; }

    public bool HasTriggers { get; init; }

    public bool IsRunning { get; init; }

    public IReadOnlyList<string> DependOnService { get; init; } = [];

    public IReadOnlyList<string> DependentServices { get; init; } = [];

    public string? DisplayName { get; init; }

    public bool Exists { get; init; } = true;
}
