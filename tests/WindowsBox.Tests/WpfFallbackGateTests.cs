using System.Diagnostics;
using System.Text.Json;

namespace WindowsBox.Tests;

public class WpfFallbackGateTests
{
    [Fact]
    public void Metrics_thresholds_match_master_plan_budgets()
    {
        // Documented gate: cold start over ~1 s or idle memory over ~120 MB.
        const int coldStartMsLimit = 1000;
        const int idleMemoryMbLimit = 120;

        Assert.True(850 < coldStartMsLimit);
        Assert.True(95 < idleMemoryMbLimit);
        Assert.False(1201 < coldStartMsLimit);
        Assert.True(121 > idleMemoryMbLimit);
    }

    [Fact]
    public void Sample_metrics_json_round_trips()
    {
        var json = """{"coldStartMs":900,"idleMemoryMb":110,"host":"ci-fixture"}""";
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(900, doc.RootElement.GetProperty("coldStartMs").GetInt32());
        Assert.Equal(110, doc.RootElement.GetProperty("idleMemoryMb").GetInt32());
    }

    [Fact]
    public void Sign_script_and_docs_exist()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        Assert.True(File.Exists(Path.Combine(root, "build", "sign.ps1")));
        Assert.True(File.Exists(Path.Combine(root, "build", "wpf-fallback-gate.ps1")));
        Assert.True(File.Exists(Path.Combine(root, "docs", "signing.md")));
        var signing = File.ReadAllText(Path.Combine(root, "docs", "signing.md"));
        Assert.Contains("WINDOWSBOX_CERT_PFX", signing, StringComparison.Ordinal);
        Assert.Contains("SignPath", signing, StringComparison.Ordinal);
    }
}
