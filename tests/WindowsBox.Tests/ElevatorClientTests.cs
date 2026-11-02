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
        }
        finally
        {
            Directory.Delete(work, recursive: true);
        }
    }
}
