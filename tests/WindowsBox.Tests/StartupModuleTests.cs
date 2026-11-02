using WindowsBox.Core.Startup;

namespace WindowsBox.Tests;

public class StartupModuleTests
{
    [Fact]
    public void Fake_enumerator_covers_autoruns_style_locations()
    {
        var entries = new FakeStartupEnumerator().Enumerate();
        var kinds = entries.Select(e => e.Location).Distinct().ToHashSet();

        Assert.Contains(StartupLocationKind.RunKeyHkcu, kinds);
        Assert.Contains(StartupLocationKind.RunKeyHklm, kinds);
        Assert.Contains(StartupLocationKind.StartupFolderUser, kinds);
        Assert.Contains(StartupLocationKind.ScheduledTask, kinds);
        Assert.Contains(StartupLocationKind.Service, kinds);
        Assert.Contains(StartupLocationKind.Driver, kinds);
        Assert.Contains(StartupLocationKind.ShellExtension, kinds);
        Assert.Contains(StartupLocationKind.WinlogonUserinit, kinds);
    }

    [Fact]
    public void Hide_microsoft_filter_works()
    {
        var entries = new FakeStartupEnumerator().Enumerate(new StartupEnumerateOptions { HideMicrosoft = true });
        Assert.All(entries, e => Assert.False(e.IsMicrosoft));
    }

    [Fact]
    public void Risk_classifier_marks_script_hosts_and_userinit()
    {
        Assert.Equal(4, StartupRiskClassifier.Classify(
            StartupLocationKind.WinlogonUserinit, true, @"C:\Windows\system32\userinit.exe"));
        Assert.Equal(3, StartupRiskClassifier.Classify(
            StartupLocationKind.RunKeyHkcu, false, "powershell -w hidden -c calc"));
        Assert.Equal(0, StartupRiskClassifier.Classify(
            StartupLocationKind.RunKeyHklm, true, @"%windir%\system32\SecurityHealthSystray.exe"));
    }
}
