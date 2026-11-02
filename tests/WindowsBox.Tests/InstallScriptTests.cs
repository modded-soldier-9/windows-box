namespace WindowsBox.Tests;

public class InstallScriptTests
{
    [Fact]
    public void Install_ps1_enforces_sha256_and_no_secondary_iex()
    {
        var path = FindRepoFile("install.ps1");
        var text = File.ReadAllText(path);
        Assert.Contains("SHA256", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("MISMATCH", text, StringComparison.Ordinal);
        Assert.Contains("Authenticode", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("browser_download_url", text, StringComparison.Ordinal);
        Assert.Contains("Does NOT download or invoke any other remote scripts", text, StringComparison.Ordinal);
        // Downloads use Invoke-WebRequest for the release ZIP + checksum only — no nested iex.
        Assert.Contains("Invoke-WebRequest", text, StringComparison.Ordinal);
        Assert.DoesNotContain("iex (", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Invoke-Expression", text, StringComparison.OrdinalIgnoreCase);
        // After a verified expand, the installer must be able to launch the WinUI shell.
        Assert.Contains("WindowsBox.App.exe", text, StringComparison.Ordinal);
        Assert.Contains("Start-Process", text, StringComparison.Ordinal);
        Assert.Contains("NoLaunch", text, StringComparison.Ordinal);
        Assert.Contains("packaging bug", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Trust_guide_exists_and_explains_inspection()
    {
        var path = FindRepoFile(Path.Combine("docs", "trust-guide.md"));
        var text = File.ReadAllText(path);
        Assert.Contains("Inspect first", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SHA256", text, StringComparison.Ordinal);
        Assert.Contains("install.ps1", text, StringComparison.Ordinal);
    }

    private static string FindRepoFile(string relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, relative);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException(relative);
    }
}
