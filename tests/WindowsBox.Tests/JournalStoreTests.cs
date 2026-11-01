using WindowsBox.Core.Catalog.Models;
using WindowsBox.Core.Engine.Journal;

namespace WindowsBox.Tests;

public class JournalStoreTests
{
    [Fact]
    public async Task Round_trips_journal_via_atomic_write()
    {
        var dir = Path.Combine(Path.GetTempPath(), "wbox-js-" + Guid.NewGuid().ToString("N"));
        var store = new FileJournalStore(dir);
        var id = Guid.NewGuid().ToString("N");

        try
        {
            var journal = new ApplyJournal
            {
                Id = id,
                CreatedAt = DateTimeOffset.UtcNow,
                Profile = "Balanced",
                MachineBuild = "26100",
                Edition = "Professional",
                Entries =
                [
                    new JournalEntry
                    {
                        ItemId = "svc.trkwks",
                        ServiceName = "TrkWks",
                        PriorStartType = ServiceStartType.Automatic,
                        TargetStartType = ServiceStartType.Manual,
                        Status = JournalEntryStatus.Pending,
                    },
                ],
            };

            await store.SaveAsync(journal);
            var loaded = await store.LoadAsync(id);
            Assert.NotNull(loaded);
            Assert.Equal(id, loaded.Id);
            Assert.Single(loaded.Entries);
            Assert.Equal(ServiceStartType.Automatic, loaded.Entries[0].PriorStartType);
            Assert.False(File.Exists(Path.Combine(dir, id + ".json.tmp")));
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }

    [Theory]
    [InlineData("../escape")]
    [InlineData("..\\escape")]
    [InlineData("not-a-guid")]
    [InlineData("")]
    public async Task Rejects_unsafe_journal_ids(string badId)
    {
        var dir = Path.Combine(Path.GetTempPath(), "wbox-js-safe-" + Guid.NewGuid().ToString("N"));
        var store = new FileJournalStore(dir);
        try
        {
            await Assert.ThrowsAsync<ArgumentException>(() => store.LoadAsync(badId));
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }
}
