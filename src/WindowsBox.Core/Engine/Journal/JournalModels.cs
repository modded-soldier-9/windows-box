using WindowsBox.Core.Catalog.Models;

namespace WindowsBox.Core.Engine.Journal;

public sealed class ApplyJournal
{
    public const int CurrentVersion = 1;

    public int Version { get; init; } = CurrentVersion;

    public required string Id { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required string Profile { get; init; }

    public required string MachineBuild { get; init; }

    public required string Edition { get; init; }

    public required IReadOnlyList<JournalEntry> Entries { get; init; }

    public string? Notes { get; init; }
}

public sealed class JournalEntry
{
    public required string ItemId { get; init; }

    public required string ServiceName { get; init; }

    public required ServiceStartType PriorStartType { get; init; }

    public bool PriorDelayedAutostart { get; init; }

    public bool PriorHasTriggers { get; init; }

    public bool PriorWasRunning { get; init; }

    public required ServiceStartType TargetStartType { get; init; }

    public bool TargetDelayedAutostart { get; init; }

    public string? RegistryExport { get; init; }

    public JournalEntryStatus Status { get; set; } = JournalEntryStatus.Pending;

    public string? Error { get; set; }
}

public enum JournalEntryStatus
{
    Pending,
    Applied,
    Skipped,
    Failed,
    RolledBack,
}
