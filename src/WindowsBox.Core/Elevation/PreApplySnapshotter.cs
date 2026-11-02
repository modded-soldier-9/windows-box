using System.Text.Json;
using System.Text.Json.Serialization;
using WindowsBox.Core.Engine.Journal;
using WindowsBox.Core.Engine.Restore;

namespace WindowsBox.Core.Elevation;

public sealed class PreApplySnapshot
{
    public required string Id { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public required string JournalId { get; init; }

    public required string Purpose { get; init; }

    public string? RestoreDirectory { get; init; }

    public string? ManifestPath { get; init; }
}

/// <summary>
/// Mandatory local backup taken before Elevator mutates services.
/// Writes offline restore kit artifacts; never contacts the network.
/// </summary>
public sealed class PreApplySnapshotter
{
    private readonly OfflineRestoreKit _restoreKit;
    private readonly string _manifestDirectory;

    public PreApplySnapshotter(string backupRootDirectory)
    {
        var root = Path.GetFullPath(backupRootDirectory);
        Directory.CreateDirectory(root);
        _restoreKit = new OfflineRestoreKit(root);
        _manifestDirectory = Path.Combine(root, "pre-apply");
        Directory.CreateDirectory(_manifestDirectory);
    }

    public PreApplySnapshot Create(ApplyJournal journal, string purpose)
    {
        ArgumentNullException.ThrowIfNull(journal);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        var artifacts = _restoreKit.Write(journal);
        var snapshot = new PreApplySnapshot
        {
            Id = Guid.NewGuid().ToString("N"),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            JournalId = journal.Id,
            Purpose = purpose,
            RestoreDirectory = artifacts.Directory,
        };

        var manifestPath = Path.Combine(_manifestDirectory, $"{journal.Id}-{purpose}.json");
        using (var stream = File.Create(manifestPath))
        {
            JsonSerializer.Serialize(stream, snapshot, SnapshotJsonContext.Default.PreApplySnapshot);
        }

        return new PreApplySnapshot
        {
            Id = snapshot.Id,
            CreatedAtUtc = snapshot.CreatedAtUtc,
            JournalId = snapshot.JournalId,
            Purpose = snapshot.Purpose,
            RestoreDirectory = snapshot.RestoreDirectory,
            ManifestPath = manifestPath,
        };
    }
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true)]
[JsonSerializable(typeof(PreApplySnapshot))]
internal partial class SnapshotJsonContext : JsonSerializerContext;
