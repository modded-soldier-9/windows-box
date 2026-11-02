using WindowsBox.Core.Catalog.Models;
using WindowsBox.Core.Engine.Journal;
using WindowsBox.Core.Engine.Preview;
using WindowsBox.Core.Presentation;

namespace WindowsBox.Tests;

public class PresentationTests
{
    [Fact]
    public void Preview_diff_presenter_maps_risk_labels_and_cards()
    {
        var plan = new PreviewPlan
        {
            Profile = OptimizationProfile.Privacy,
            DiffSummary = "2 service(s); 1 → Manual; 1 → Disabled; est. −20 MB idle",
            EstimatedIdleRamSavedMb = 20,
            Changes =
            [
                new PreviewChange
                {
                    ItemId = "svc.diagtrack",
                    ServiceName = "DiagTrack",
                    Display = "Telemetry",
                    Current = ServiceStartType.Automatic,
                    Target = ServiceStartType.Disabled,
                    Risk = 2,
                    ExpectedGainMb = 12,
                },
                new PreviewChange
                {
                    ItemId = "svc.trkwks",
                    ServiceName = "TrkWks",
                    Display = "Link tracking",
                    Current = ServiceStartType.Automatic,
                    Target = ServiceStartType.Manual,
                    Risk = 1,
                    ExpectedGainMb = 8,
                },
            ],
        };

        var cards = PreviewDiffPresenter.ToCards(plan);
        Assert.Equal(2, cards.Count);
        Assert.Equal("Situational", cards[0].RiskLabel);
        Assert.Equal("Safe", cards[1].RiskLabel);
        Assert.Contains("→ Disabled", PreviewDiffPresenter.FormatPlainText(plan), StringComparison.Ordinal);
    }

    [Fact]
    public void History_presenter_marks_journals_with_undo()
    {
        var journals = new[]
        {
            new ApplyJournal
            {
                Id = Guid.NewGuid().ToString("N"),
                CreatedAt = DateTimeOffset.UtcNow,
                Profile = "Balanced",
                MachineBuild = "26100",
                Edition = "Pro",
                Entries =
                [
                    new JournalEntry
                    {
                        ItemId = "svc.trkwks",
                        ServiceName = "TrkWks",
                        PriorStartType = ServiceStartType.Automatic,
                        TargetStartType = ServiceStartType.Manual,
                        Status = JournalEntryStatus.Applied,
                    },
                ],
            },
        };

        var rows = HistoryPresenter.ToRows(journals);
        Assert.Single(rows);
        Assert.True(rows[0].CanUndo);
        Assert.Contains("applied", rows[0].Summary, StringComparison.OrdinalIgnoreCase);
    }
}
