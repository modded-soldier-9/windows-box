using WindowsBox.Core.Engine.Journal;

namespace WindowsBox.Core.Presentation;

public static class HistoryPresenter
{
    public static IReadOnlyList<HistoryRowModel> ToRows(IEnumerable<ApplyJournal> journals) =>
        journals
            .OrderByDescending(j => j.CreatedAt)
            .Select(j => new HistoryRowModel
            {
                JournalId = j.Id,
                When = j.CreatedAt,
                Profile = j.Profile,
                Summary =
                    $"{j.Entries.Count} item(s) — " +
                    $"{j.Entries.Count(e => e.Status == JournalEntryStatus.Applied)} applied, " +
                    $"{j.Entries.Count(e => e.Status == JournalEntryStatus.RolledBack)} rolled back, " +
                    $"{j.Entries.Count(e => e.Status == JournalEntryStatus.Failed)} failed",
                CanUndo = j.Entries.Any(e =>
                    e.Status is JournalEntryStatus.Applied or JournalEntryStatus.Pending or JournalEntryStatus.Failed),
            })
            .ToList();
}

public sealed class HistoryRowModel
{
    public required string JournalId { get; init; }

    public required DateTimeOffset When { get; init; }

    public required string Profile { get; init; }

    public required string Summary { get; init; }

    public bool CanUndo { get; init; }
}
