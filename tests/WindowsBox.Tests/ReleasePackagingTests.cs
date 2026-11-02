namespace WindowsBox.Tests;

public class ReleasePackagingTests
{
    [Fact]
    public void Release_workflow_publishes_winui_app_into_portable_zip()
    {
        var path = FindRepoFile(Path.Combine(".github", "workflows", "release.yml"));
        var text = File.ReadAllText(path);

        Assert.Contains("WindowsBox.App.csproj", text, StringComparison.Ordinal);
        Assert.Contains("artifacts/app", text, StringComparison.Ordinal);
        Assert.Contains("pack-portable.ps1", text, StringComparison.Ordinal);
        // Guard against regressing to CLI+Elevator-only releases (nothing to launch).
        Assert.DoesNotContain(
            "Compress-Archive -Path artifacts/wbox/*,artifacts/elevator/*",
            text,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Pack_portable_script_requires_app_and_catalog()
    {
        var path = FindRepoFile(Path.Combine("build", "pack-portable.ps1"));
        var text = File.ReadAllText(path);

        Assert.Contains("WindowsBox.App.exe", text, StringComparison.Ordinal);
        Assert.Contains("catalog", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("plugins", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("missing required binary", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("*.pdb", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sign_script_includes_winui_app()
    {
        var path = FindRepoFile(Path.Combine("build", "sign.ps1"));
        var text = File.ReadAllText(path);
        Assert.Contains("artifacts/app/WindowsBox.App.exe", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Pack_portable_fails_when_app_publish_output_missing()
    {
        var script = FindRepoFile(Path.Combine("build", "pack-portable.ps1"));
        var root = Path.GetDirectoryName(Path.GetDirectoryName(script))
                   ?? throw new InvalidOperationException("repo root");
        var temp = Path.Combine(Path.GetTempPath(), "windowsbox-pack-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            // Stage dirs without WindowsBox.App.exe — pack must refuse.
            var app = Path.Combine(temp, "app");
            var wbox = Path.Combine(temp, "wbox");
            var elevator = Path.Combine(temp, "elevator");
            var catalog = Path.Combine(temp, "catalog");
            Directory.CreateDirectory(app);
            Directory.CreateDirectory(wbox);
            Directory.CreateDirectory(elevator);
            Directory.CreateDirectory(Path.Combine(catalog, "services"));
            File.WriteAllText(Path.Combine(wbox, "wbox.exe"), "stub");
            File.WriteAllText(Path.Combine(elevator, "WindowsBox.Elevator.exe"), "stub");
            File.WriteAllText(Path.Combine(catalog, "services", "example.yaml"), "id: example");

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = ResolvePwsh(),
                WorkingDirectory = root,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            psi.ArgumentList.Add("-NoProfile");
            psi.ArgumentList.Add("-File");
            psi.ArgumentList.Add(script);
            psi.ArgumentList.Add("-Tag");
            psi.ArgumentList.Add("test");
            psi.ArgumentList.Add("-OutDir");
            psi.ArgumentList.Add(Path.Combine(temp, "dist"));
            psi.ArgumentList.Add("-StageDir");
            psi.ArgumentList.Add(Path.Combine(temp, "stage"));
            psi.ArgumentList.Add("-AppDir");
            psi.ArgumentList.Add(app);
            psi.ArgumentList.Add("-WboxDir");
            psi.ArgumentList.Add(wbox);
            psi.ArgumentList.Add("-ElevatorDir");
            psi.ArgumentList.Add(elevator);
            psi.ArgumentList.Add("-CatalogDir");
            psi.ArgumentList.Add(catalog);
            psi.ArgumentList.Add("-PluginsDir");
            psi.ArgumentList.Add(Path.Combine(temp, "missing-plugins"));

            using var process = System.Diagnostics.Process.Start(psi)
                                ?? throw new InvalidOperationException("failed to start pwsh");
            var stderr = process.StandardError.ReadToEnd();
            var stdout = process.StandardOutput.ReadToEnd();
            process.WaitForExit(60_000);
            Assert.NotEqual(0, process.ExitCode);
            var combined = stdout + stderr;
            Assert.Contains("WindowsBox.App.exe", combined, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(temp))
            {
                Directory.Delete(temp, recursive: true);
            }
        }
    }

    [Fact]
    public void Pack_portable_includes_app_catalog_and_excludes_pdbs()
    {
        var script = FindRepoFile(Path.Combine("build", "pack-portable.ps1"));
        var root = Path.GetDirectoryName(Path.GetDirectoryName(script))
                   ?? throw new InvalidOperationException("repo root");
        var temp = Path.Combine(Path.GetTempPath(), "windowsbox-pack-ok-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            var app = Path.Combine(temp, "app");
            var wbox = Path.Combine(temp, "wbox");
            var elevator = Path.Combine(temp, "elevator");
            var catalog = Path.Combine(temp, "catalog");
            var plugins = Path.Combine(temp, "plugins");
            Directory.CreateDirectory(app);
            Directory.CreateDirectory(wbox);
            Directory.CreateDirectory(elevator);
            Directory.CreateDirectory(Path.Combine(catalog, "services"));
            Directory.CreateDirectory(Path.Combine(plugins, "sample"));
            File.WriteAllText(Path.Combine(app, "WindowsBox.App.exe"), "app");
            File.WriteAllText(Path.Combine(app, "WindowsBox.App.pdb"), "pdb");
            File.WriteAllText(Path.Combine(wbox, "wbox.exe"), "cli");
            File.WriteAllText(Path.Combine(wbox, "wbox.pdb"), "pdb");
            File.WriteAllText(Path.Combine(elevator, "WindowsBox.Elevator.exe"), "el");
            File.WriteAllText(Path.Combine(catalog, "services", "bits.yaml"), "id: bits");
            File.WriteAllText(Path.Combine(plugins, "sample", "plugin.yaml"), "id: sample");

            var dist = Path.Combine(temp, "dist");
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = ResolvePwsh(),
                WorkingDirectory = root,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            psi.ArgumentList.Add("-NoProfile");
            psi.ArgumentList.Add("-File");
            psi.ArgumentList.Add(script);
            psi.ArgumentList.Add("-Tag");
            psi.ArgumentList.Add("test");
            psi.ArgumentList.Add("-OutDir");
            psi.ArgumentList.Add(dist);
            psi.ArgumentList.Add("-StageDir");
            psi.ArgumentList.Add(Path.Combine(temp, "stage"));
            psi.ArgumentList.Add("-AppDir");
            psi.ArgumentList.Add(app);
            psi.ArgumentList.Add("-WboxDir");
            psi.ArgumentList.Add(wbox);
            psi.ArgumentList.Add("-ElevatorDir");
            psi.ArgumentList.Add(elevator);
            psi.ArgumentList.Add("-CatalogDir");
            psi.ArgumentList.Add(catalog);
            psi.ArgumentList.Add("-PluginsDir");
            psi.ArgumentList.Add(plugins);

            using var process = System.Diagnostics.Process.Start(psi)
                                ?? throw new InvalidOperationException("failed to start pwsh");
            var stderr = process.StandardError.ReadToEnd();
            var stdout = process.StandardOutput.ReadToEnd();
            process.WaitForExit(60_000);
            Assert.True(process.ExitCode == 0, stdout + stderr);

            var zipPath = Path.Combine(dist, "WindowsBox-test-win-x64.zip");
            Assert.True(File.Exists(zipPath), "expected zip at " + zipPath);
            Assert.True(File.Exists(zipPath + ".sha256"));

            using var zip = System.IO.Compression.ZipFile.OpenRead(zipPath);
            var names = zip.Entries.Select(e => e.FullName.Replace('\\', '/')).ToArray();
            Assert.Contains(names, n => n.Equals("WindowsBox.App.exe", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(names, n => n.Equals("wbox.exe", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(names, n => n.Equals("WindowsBox.Elevator.exe", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(names, n => n.Contains("catalog/services/", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(names, n => n.Contains("plugins/", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(names, n => n.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            if (Directory.Exists(temp))
            {
                Directory.Delete(temp, recursive: true);
            }
        }
    }

    private static string ResolvePwsh()
    {
        var home = Environment.GetEnvironmentVariable("PWSH")
                   ?? Environment.GetEnvironmentVariable("POWERSHELL");
        if (!string.IsNullOrWhiteSpace(home) && File.Exists(home))
        {
            return home;
        }

        foreach (var name in new[] { "pwsh", "pwsh.exe", "powershell", "powershell.exe" })
        {
            var found = FindOnPath(name);
            if (found is not null)
            {
                return found;
            }
        }

        throw new InvalidOperationException("pwsh/powershell not found on PATH");
    }

    private static string? FindOnPath(string fileName)
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(dir, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
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
