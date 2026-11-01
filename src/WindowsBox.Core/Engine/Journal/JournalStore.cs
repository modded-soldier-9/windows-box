using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace WindowsBox.Core.Engine.Journal;

public interface IJournalStore
{
    Task SaveAsync(ApplyJournal journal, CancellationToken cancellationToken = default);

    Task<ApplyJournal?> LoadAsync(string journalId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ApplyJournal>> ListAsync(CancellationToken cancellationToken = default);
}

public sealed partial class FileJournalStore : IJournalStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly string _root;

    public FileJournalStore(string rootDirectory)
    {
        _root = Path.GetFullPath(rootDirectory);
        Directory.CreateDirectory(_root);
    }

    public async Task SaveAsync(ApplyJournal journal, CancellationToken cancellationToken = default)
    {
        var path = ResolveJournalPath(journal.Id);
        var tempPath = path + ".tmp";

        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, journal, JsonOptions, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }

        File.Move(tempPath, path, overwrite: true);
    }

    public async Task<ApplyJournal?> LoadAsync(string journalId, CancellationToken cancellationToken = default)
    {
        var path = ResolveJournalPath(journalId);
        if (!File.Exists(path))
        {
            return null;
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<ApplyJournal>(stream, JsonOptions, cancellationToken);
    }

    public async Task<IReadOnlyList<ApplyJournal>> ListAsync(CancellationToken cancellationToken = default)
    {
        var list = new List<ApplyJournal>();
        foreach (var file in Directory.EnumerateFiles(_root, "*.json").OrderByDescending(f => f))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using var stream = File.OpenRead(file);
            var journal = await JsonSerializer.DeserializeAsync<ApplyJournal>(stream, JsonOptions, cancellationToken);
            if (journal is not null)
            {
                list.Add(journal);
            }
        }

        return list;
    }

    private string ResolveJournalPath(string journalId)
    {
        if (string.IsNullOrWhiteSpace(journalId) || !SafeJournalId().IsMatch(journalId))
        {
            throw new ArgumentException(
                "Journal id must be a 32-character lowercase hex Guid (N format).",
                nameof(journalId));
        }

        var path = Path.GetFullPath(Path.Combine(_root, $"{journalId}.json"));
        var rootWithSep = _root.EndsWith(Path.DirectorySeparatorChar)
            ? _root
            : _root + Path.DirectorySeparatorChar;

        if (!path.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(Path.GetDirectoryName(path), _root, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Journal path escapes the journal root.", nameof(journalId));
        }

        return path;
    }

    [GeneratedRegex("^[0-9a-f]{32}$")]
    private static partial Regex SafeJournalId();
}
