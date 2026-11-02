using WindowsBox.Core.Catalog.Models;
using WindowsBox.Core.Engine.Preview;
using WindowsBox.Core.Presentation;

namespace WindowsBox.Tests;

public class DryRunPresenterTests
{
    [Fact]
    public void Format_lists_planned_changes_without_claiming_apply()
    {
        var plan = new PreviewPlan
        {
            Profile = OptimizationProfile.Balanced,
            DiffSummary = "1 change",
            HonestyNote = "Demo",
            Changes =
            [
                new PreviewChange
                {
                    ItemId = "svc.diagtrack",
                    ServiceName = "DiagTrack",
                    Display = "Telemetry",
                    Current = ServiceStartType.Automatic,
                    Target = ServiceStartType.Manual,
                    Risk = 2,
                    ExpectedGainMb = 8,
                    Breaks = ["Optional diagnostic data upload"],
                    RequiresConfirm = false,
                },
            ],
            Skipped = [],
        };

        var text = DryRunPresenter.Format(plan);
        Assert.Contains("Dry-run", text, StringComparison.Ordinal);
        Assert.Contains("Planned changes (not applied)", text, StringComparison.Ordinal);
        Assert.Contains("DiagTrack: Automatic → Manual", text, StringComparison.Ordinal);
        Assert.Contains("breaks:", text, StringComparison.Ordinal);
    }
}
