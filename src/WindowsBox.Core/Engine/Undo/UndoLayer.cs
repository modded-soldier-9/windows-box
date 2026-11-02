using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using WindowsBox.Core.Abstractions;
using WindowsBox.Core.Catalog.Models;
using WindowsBox.Core.Engine.Journal;

namespace WindowsBox.Core.Engine.Undo;

public interface IUndoLayer
{
    /// <summary>
    /// Builds an undo batch from an apply journal. Rejects one-way entries unless confirmed.
    /// </summary>
    UndoBatch FromJournal(ApplyJournal journal, bool confirmOneWay = false);

    Task SaveAsync(UndoBatch batch, CancellationToken cancellationToken = default);

    Task<UndoBatch?> LoadAsync(string batchId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UndoBatch>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Replays prior values for every reversible change in the batch.</summary>
    Task<UndoResult> UndoAsync(string batchId, CancellationToken cancellationToken = default);
}

public sealed partial class FileUndoLayer : IUndoLayer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly string _root;
    private readonly IServiceController _services;
    private readonly IRegistry _registry;

    public FileUndoLayer(string rootDirectory, IServiceController services, IRegistry registry)
    {
        _root = Path.GetFullPath(rootDirectory);
        Directory.CreateDirectory(_root);
        _services = services;
        _registry = registry;
    }

    public UndoBatch FromJournal(ApplyJournal journal, bool confirmOneWay = false)
    {
        var changes = new List<ChangeRecord>(journal.Entries.Count);
        foreach (var entry in journal.Entries)
        {
            var record = new ChangeRecord
            {
                ItemId = entry.ItemId,
                Kind = ChangeKind.ServiceStartType,
                Target = entry.ServiceName,
                PriorValue = FormatService(entry.PriorStartType, entry.PriorDelayedAutostart),
                TargetValue = FormatService(entry.TargetStartType, entry.TargetDelayedAutostart),
                IsOneWay = false,
                Notes = entry.RegistryExport is null ? null : "registry export captured",
            };

            if (record.IsOneWay && !confirmOneWay)
            {
                throw new InvalidOperationException(
                    $"Refusing one-way change without confirmOneWay: {record.ItemId}");
            }

            if (!record.CanUndo)
            {
                throw new InvalidOperationException(
                    $"Change has no undo prior: {record.ItemId} ({record.Target})");
            }

            changes.Add(record);
        }

        return new UndoBatch
        {
            Id = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTimeOffset.UtcNow,
            Source = "apply-journal",
            Changes = changes,
            LinkedJournalId = journal.Id,
        };
    }

    public async Task SaveAsync(UndoBatch batch, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(batch.Id);
        var temp = path + ".tmp";
        await using (var stream = File.Create(temp))
        {
            await JsonSerializer.SerializeAsync(stream, batch, JsonOptions, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }

        File.Move(temp, path, overwrite: true);
    }

    public async Task<UndoBatch?> LoadAsync(string batchId, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(batchId);
        if (!File.Exists(path))
        {
            return null;
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<UndoBatch>(stream, JsonOptions, cancellationToken);
    }

    public async Task<IReadOnlyList<UndoBatch>> ListAsync(CancellationToken cancellationToken = default)
    {
        var list = new List<UndoBatch>();
        foreach (var file in Directory.EnumerateFiles(_root, "*.json").OrderByDescending(f => f))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using var stream = File.OpenRead(file);
            var batch = await JsonSerializer.DeserializeAsync<UndoBatch>(stream, JsonOptions, cancellationToken);
            if (batch is not null)
            {
                list.Add(batch);
            }
        }

        return list;
    }

    public async Task<UndoResult> UndoAsync(string batchId, CancellationToken cancellationToken = default)
    {
        var batch = await LoadAsync(batchId, cancellationToken)
                    ?? throw new InvalidOperationException($"Undo batch not found: {batchId}");

        var results = new List<UndoItemResult>();
        foreach (var change in batch.Changes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!change.CanUndo)
            {
                results.Add(new UndoItemResult
                {
                    ItemId = change.ItemId,
                    Target = change.Target,
                    Ok = false,
                    Detail = "One-way / missing prior — skipped.",
                });
                continue;
            }

            try
            {
                ApplyPrior(change);
                results.Add(new UndoItemResult
                {
                    ItemId = change.ItemId,
                    Target = change.Target,
                    Ok = true,
                    Detail = $"Restored prior {change.PriorValue}",
                });
            }
            catch (Exception ex)
            {
                results.Add(new UndoItemResult
                {
                    ItemId = change.ItemId,
                    Target = change.Target,
                    Ok = false,
                    Detail = ex.Message,
                });
            }
        }

        return new UndoResult
        {
            BatchId = batch.Id,
            Items = results,
            Passed = results.All(r => r.Ok),
        };
    }

    private void ApplyPrior(ChangeRecord change)
    {
        switch (change.Kind)
        {
            case ChangeKind.ServiceStartType:
                var (start, delayed) = ParseService(change.PriorValue);
                _services.SetStartType(change.Target, start, delayed);
                break;
            case ChangeKind.RegistryValue:
            case ChangeKind.PolicyValue:
                ApplyRegistryPrior(change);
                break;
            case ChangeKind.ScheduledTaskState:
            case ChangeKind.StartupEntry:
                throw new NotSupportedException(
                    $"{change.Kind} undo lands with the startup/tasks modules; prior is journaled already.");
            default:
            {
                // Exhaustive switch: new kinds must be handled or refuse loudly.
                ChangeKind unknown = change.Kind;
                throw new ArgumentOutOfRangeException(nameof(change), unknown, "Unhandled change kind.");
            }
        }
    }

    private void ApplyRegistryPrior(ChangeRecord change)
    {
        // Target format: HKLM\path!ValueName or HKCU\path!ValueName
        var bang = change.Target.LastIndexOf('!');
        if (bang <= 0)
        {
            throw new InvalidOperationException($"Registry target missing value name: {change.Target}");
        }

        var keyPath = change.Target[..bang];
        var valueName = change.Target[(bang + 1)..];
        var hive = keyPath.StartsWith("HKCU", StringComparison.OrdinalIgnoreCase)
            ? RegistryHive.CurrentUser
            : RegistryHive.LocalMachine;
        var subKey = StripHivePrefix(keyPath);

        if (change.PriorValue.StartsWith("dword:", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(change.PriorValue[6..], System.Globalization.NumberStyles.HexNumber, null, out var dword))
        {
            _registry.SetValue(hive, subKey, valueName, dword, RegistryValueKind.DWord);
            return;
        }

        _registry.SetValue(hive, subKey, valueName, change.PriorValue, RegistryValueKind.String);
    }

    private static string StripHivePrefix(string keyPath)
    {
        foreach (var prefix in new[] { @"HKLM\", @"HKCU\", @"HKEY_LOCAL_MACHINE\", @"HKEY_CURRENT_USER\" })
        {
            if (keyPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return keyPath[prefix.Length..];
            }
        }

        return keyPath;
    }

    private static string FormatService(ServiceStartType start, bool delayed) =>
        delayed && start is ServiceStartType.Automatic or ServiceStartType.AutomaticDelayed
            ? $"{start}|delayed=1"
            : start.ToString();

    private static (ServiceStartType Start, bool Delayed) ParseService(string value)
    {
        var parts = value.Split('|');
        if (!Enum.TryParse<ServiceStartType>(parts[0], ignoreCase: true, out var start))
        {
            throw new InvalidOperationException($"Bad service prior: {value}");
        }

        var delayed = parts.Any(p => p.Equals("delayed=1", StringComparison.OrdinalIgnoreCase))
                      || start == ServiceStartType.AutomaticDelayed;
        return (start, delayed);
    }

    private string ResolvePath(string batchId)
    {
        if (string.IsNullOrWhiteSpace(batchId) || !SafeId().IsMatch(batchId))
        {
            throw new ArgumentException("Undo batch id must be 32-char lowercase hex.", nameof(batchId));
        }

        var path = Path.GetFullPath(Path.Combine(_root, $"{batchId}.json"));
        var rootWithSep = _root.EndsWith(Path.DirectorySeparatorChar)
            ? _root
            : _root + Path.DirectorySeparatorChar;
        if (!path.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Undo path escapes root.", nameof(batchId));
        }

        return path;
    }

    [GeneratedRegex("^[0-9a-f]{32}$")]
    private static partial Regex SafeId();
}

public sealed class UndoResult
{
    public required string BatchId { get; init; }

    public required bool Passed { get; init; }

    public required IReadOnlyList<UndoItemResult> Items { get; init; }
}

public sealed class UndoItemResult
{
    public required string ItemId { get; init; }

    public required string Target { get; init; }

    public required bool Ok { get; init; }

    public string? Detail { get; init; }
}
