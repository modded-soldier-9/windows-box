namespace WindowsBox.Core.Plugins;

public sealed class PluginManifest
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public string Description { get; init; } = "";

    public string Version { get; init; } = "0.0.0";

    public string Kind { get; init; } = "service-check";

    public bool Builtin { get; init; }

    public string? Entry { get; init; }

    public IReadOnlyList<string> Sources { get; init; } = [];

    public string? SourceDirectory { get; init; }
}
