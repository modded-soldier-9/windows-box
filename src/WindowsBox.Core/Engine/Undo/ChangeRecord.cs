using System.Text.Json.Serialization;

namespace WindowsBox.Core.Engine.Undo;

/// <summary>
/// Generic prior/target snapshot for any tweak area. Service applies still use
/// <see cref="Journal.JournalEntry"/>; this record is the shared undo contract
/// future modules (startup, privacy, network) append alongside.
/// </summary>
public sealed class ChangeRecord
{
    public required string ItemId { get; init; }

    public required ChangeKind Kind { get; init; }

    /// <summary>Stable address: service name, registry path, task path, etc.</summary>
    public required string Target { get; init; }

    /// <summary>Serialized prior value (never assumed defaults — always measured).</summary>
    public required string PriorValue { get; init; }

    /// <summary>Serialized intended value after apply.</summary>
    public required string TargetValue { get; init; }

    public bool IsOneWay { get; init; }

    public string? Notes { get; init; }

    [JsonIgnore]
    public bool CanUndo => !IsOneWay && !string.IsNullOrWhiteSpace(PriorValue);
}

public sealed class UndoBatch
{
    public const int CurrentVersion = 1;

    public int Version { get; init; } = CurrentVersion;

    public required string Id { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required string Source { get; init; }

    public required IReadOnlyList<ChangeRecord> Changes { get; init; }

    public string? LinkedJournalId { get; init; }
}
