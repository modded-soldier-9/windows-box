using System.Text.Json;
using System.Text.Json.Serialization;

namespace WindowsBox.Core.Engine.Journal;

public interface IJournalStore
{
    Task SaveAsync(ApplyJournal journal, CancellationToken cancellationToken = default);

    Task<ApplyJournal?> LoadAsync(string journalId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ApplyJournal>> ListAsync(CancellationToken cancellationToken = default);
}

public sealed class FileJournalStore : IJournalStore
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
        _root = rootDirectory;
        Directory.CreateDirectory(_root);
    }

    public async Task SaveAsync(ApplyJournal journal, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(_root, $"{journal.Id}.json");
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, journal, JsonOptions, cancellationToken);
    }

    public async Task<ApplyJournal?> LoadAsync(string journalId, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(_root, $"{journalId}.json");
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
}
