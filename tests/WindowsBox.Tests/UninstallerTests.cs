using WindowsBox.Core.Install;
using WindowsBox.Core.Scheduling;

namespace WindowsBox.Tests;

public class UninstallerTests
{
    [Fact]
    public void Plan_includes_verify_task_and_registry_keys()
    {
        var dir = Path.Combine(Path.GetTempPath(), "wbox-un-" + Guid.NewGuid().ToString("N"));
        try
        {
            var installer = new FileVerifyTaskInstaller(dir);
            var plan = new Uninstaller(installer, programDataRoot: Path.Combine(dir, "pdata")).Plan();
            Assert.Equal(VerifyTaskDefinition.FullPath, plan.VerifyTaskPath);
            Assert.Contains(plan.RegistryKeys, k => k.Contains("WindowsBox", StringComparison.Ordinal));
            Assert.Contains(plan.Actions, a => a.Contains("Verify", StringComparison.Ordinal));
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }

    [Fact]
    public void Run_removes_verify_artifacts_and_recorded_registry_keys()
    {
        var dir = Path.Combine(Path.GetTempPath(), "wbox-un-" + Guid.NewGuid().ToString("N"));
        try
        {
            var installer = new FileVerifyTaskInstaller(dir);
            installer.Install(@"C:\Tools\wbox.exe");
            Assert.True(installer.GetStatus().Installed);

            var registry = new RecordingRegistryKeyCleaner(Uninstaller.DefaultRegistryKeys);
            var pdata = Path.Combine(dir, "pdata");
            Directory.CreateDirectory(pdata);
            File.WriteAllText(Path.Combine(pdata, "keep.txt"), "x");

            var result = new Uninstaller(installer, registry, pdata).Run(removeProgramData: true);
            Assert.True(result.Ok);
            Assert.False(installer.GetStatus().Installed);
            Assert.Equal(Uninstaller.DefaultRegistryKeys.Length, registry.Deleted.Count);
            Assert.False(Directory.Exists(pdata));
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
