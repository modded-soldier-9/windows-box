using WindowsBox.Core.Scheduling;

namespace WindowsBox.Tests;

public class VerifyTaskTests
{
    [Fact]
    public void Xml_includes_wu_event_19_and_startup_delay()
    {
        var def = new VerifyTaskDefinition();
        var xml = def.ToSchtasksXml(@"C:\Tools\wbox.exe");
        Assert.Contains("EventID=19", xml, StringComparison.Ordinal);
        Assert.Contains("WindowsUpdateClient/Operational", xml, StringComparison.Ordinal);
        Assert.Contains("verify --quiet", xml, StringComparison.Ordinal);
        Assert.Contains("LogonTrigger", xml, StringComparison.Ordinal);
        Assert.Contains("PT3M", xml, StringComparison.Ordinal); // midpoint of 2–5
        Assert.Contains("PT10M", xml, StringComparison.Ordinal);
    }

    [Fact]
    public void Installer_is_opt_in_and_uninstallable()
    {
        var dir = Path.Combine(Path.GetTempPath(), "wbox-task-" + Guid.NewGuid().ToString("N"));
        var installer = new FileVerifyTaskInstaller(dir);
        try
        {
            Assert.False(installer.GetStatus().Installed);
            installer.Install(@"C:\Tools\wbox.exe");
            Assert.True(installer.GetStatus().Installed);
            Assert.True(File.Exists(installer.XmlPath));
            installer.Uninstall();
            Assert.False(installer.GetStatus().Installed);
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
