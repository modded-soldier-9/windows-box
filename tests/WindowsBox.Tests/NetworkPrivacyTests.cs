using WindowsBox.Core.Network;

namespace WindowsBox.Tests;

public class NetworkPrivacyTests
{
    [Fact]
    public void Fake_capture_is_one_shot_and_formats_report()
    {
        var module = new NetworkPrivacyModule(new FakeNetworkPrivacyCapture());
        var report = module.Run();
        Assert.Equal(2, report.Entries.Count);
        Assert.Contains(report.Entries, e => e.LooksMicrosoft);
        var text = module.FormatPlainText(report);
        Assert.Contains("One-shot snapshot", text, StringComparison.Ordinal);
        Assert.Contains("settings-win.data.microsoft.com", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Active_capture_does_not_throw_and_exits_with_report()
    {
        var module = new NetworkPrivacyModule(new ActiveTcpNetworkPrivacyCapture());
        var report = module.Run(new NetworkPrivacyOptions { IncludeLoopback = false });
        Assert.NotNull(report.Summary);
        Assert.True(report.CapturedAt <= DateTimeOffset.UtcNow.AddMinutes(1));
    }
}
