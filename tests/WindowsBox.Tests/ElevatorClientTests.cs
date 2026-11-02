using WindowsBox.Core.Catalog.Models;
using WindowsBox.Core.Elevation;
using WindowsBox.Core.Engine.Journal;

namespace WindowsBox.Tests;

public class ElevatorClientTests
{
    [Fact]
    public async Task Ping_on_non_windows_returns_ok_without_launching_missing_binary()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var client = new ElevatorClient(elevatorPath: "/nonexistent/WindowsBox.Elevator.dll");
        var result = await client.PingAsync();
        Assert.True(result.Ok);
        Assert.Contains("non-Windows", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApplyJournal_builds_plan_payload_without_host_mutation_off_windows()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var journal = new ApplyJournal
        {
            Id = Guid.NewGuid().ToString("N"),
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

        var work = Path.Combine(Path.GetTempPath(), "wbox-elev-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        try
        {
            var client = new ElevatorClient("/nonexistent/WindowsBox.Elevator.dll", work);
            var result = await client.ApplyJournalAsync(journal, requireElevation: false);
            Assert.True(result.Ok);
            Assert.True(File.Exists(Path.Combine(work, $"{journal.Id}-plan.json")));
            Assert.NotNull(client.LastSnapshot);
            Assert.Equal("pre-apply", client.LastSnapshot!.Purpose);
            Assert.True(File.Exists(client.LastSnapshot.ManifestPath));
            Assert.True(Directory.Exists(client.LastSnapshot.RestoreDirectory));
        }
        finally
        {
            Directory.Delete(work, recursive: true);
        }
    }
}

public class PreApplySnapshotterTests
{
    [Fact]
    public void Create_writes_restore_kit_and_manifest_before_mutation()
    {
        var dir = Path.Combine(Path.GetTempPath(), "wbox-snap-" + Guid.NewGuid().ToString("N"));
        try
        {
            var journal = new ApplyJournal
            {
                Id = Guid.NewGuid().ToString("N"),
                CreatedAt = DateTimeOffset.UtcNow,
                Profile = "Safe",
                MachineBuild = "26100",
                Edition = "Professional",
                Entries =
                [
                    new JournalEntry
                    {
                        ItemId = "svc.diagtrack",
                        ServiceName = "DiagTrack",
                        PriorStartType = ServiceStartType.Automatic,
                        TargetStartType = ServiceStartType.Manual,
                        RegistryExport = "Windows Registry Editor Version 5.00\r\n",
                        Status = JournalEntryStatus.Pending,
                    },
                ],
            };

            var snap = new PreApplySnapshotter(dir).Create(journal, "pre-apply");
            Assert.Equal(journal.Id, snap.JournalId);
            Assert.True(File.Exists(snap.ManifestPath));
            Assert.True(Directory.Exists(snap.RestoreDirectory));
            Assert.True(File.Exists(Path.Combine(snap.RestoreDirectory!, "restore-services.cmd")));
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
