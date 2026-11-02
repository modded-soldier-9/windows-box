namespace WindowsBox.Core.Scheduling;

public interface IVerifyTaskInstaller
{
    VerifyTaskStatus GetStatus();

    void Install(string wboxExecutablePath, VerifyTaskDefinition? definition = null);

    void Uninstall();
}

public sealed class VerifyTaskStatus
{
    public required bool Installed { get; init; }

    public string? Path { get; init; }

    public string? Detail { get; init; }
}

/// <summary>
/// Writes the task XML artifact and records intent. Actual schtasks registration
/// is Windows-only and performed by the Elevator when the user opts in — this
/// class never silently registers anything.
/// </summary>
public sealed class FileVerifyTaskInstaller : IVerifyTaskInstaller
{
    private readonly string _artifactDir;

    public FileVerifyTaskInstaller(string artifactDirectory)
    {
        _artifactDir = Path.GetFullPath(artifactDirectory);
        Directory.CreateDirectory(_artifactDir);
    }

    public string XmlPath => Path.Combine(_artifactDir, "WindowsBox.Verify.xml");

    public string MarkerPath => Path.Combine(_artifactDir, "verify-task.opt-in");

    public VerifyTaskStatus GetStatus()
    {
        var installed = File.Exists(MarkerPath) && File.Exists(XmlPath);
        return new VerifyTaskStatus
        {
            Installed = installed,
            Path = installed ? VerifyTaskDefinition.FullPath : null,
            Detail = installed
                ? "Opt-in marker present; register with schtasks using the generated XML (Elevator)."
                : "Not opted in.",
        };
    }

    public void Install(string wboxExecutablePath, VerifyTaskDefinition? definition = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(wboxExecutablePath);
        definition ??= new VerifyTaskDefinition();
        File.WriteAllText(XmlPath, definition.ToSchtasksXml(wboxExecutablePath));
        File.WriteAllText(
            MarkerPath,
            $"""
            opted-in={DateTimeOffset.UtcNow:u}
            task={VerifyTaskDefinition.FullPath}
            exe={wboxExecutablePath}
            note=Re-apply after drift remains opt-in; this task only runs wbox verify --quiet.
            """);
    }

    public void Uninstall()
    {
        if (File.Exists(MarkerPath))
        {
            File.Delete(MarkerPath);
        }

        if (File.Exists(XmlPath))
        {
            File.Delete(XmlPath);
        }
    }
}
