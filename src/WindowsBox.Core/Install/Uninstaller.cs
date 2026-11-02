using WindowsBox.Core.Scheduling;

namespace WindowsBox.Core.Install;

public sealed class UninstallPlan
{
    public required IReadOnlyList<string> Actions { get; init; }

    public required IReadOnlyList<string> RegistryKeys { get; init; }

    public required string VerifyTaskPath { get; init; }
}

public sealed class UninstallResult
{
    public required bool Ok { get; init; }

    public required IReadOnlyList<string> Completed { get; init; }

    public required IReadOnlyList<string> Skipped { get; init; }

    public string? Detail { get; init; }
}

/// <summary>
/// Removes Windows Box install residue: <c>\WindowsBox\Verify</c> task artifacts,
/// known registry keys, and optional ProgramData folders when requested.
/// Never phones home. Safe to run dry (plan only).
/// </summary>
public sealed class Uninstaller
{
    public static readonly string[] DefaultRegistryKeys =
    [
        @"HKCU\Software\WindowsBox",
        @"HKLM\Software\WindowsBox",
        @"HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\WindowsBox",
    ];

    private readonly IVerifyTaskInstaller _verifyTask;
    private readonly IRegistryKeyCleaner _registry;
    private readonly string? _programDataRoot;

    public Uninstaller(
        IVerifyTaskInstaller verifyTask,
        IRegistryKeyCleaner? registry = null,
        string? programDataRoot = null)
    {
        _verifyTask = verifyTask;
        _registry = registry ?? new NoOpRegistryKeyCleaner();
        _programDataRoot = programDataRoot;
    }

    public UninstallPlan Plan()
    {
        var actions = new List<string>
        {
            $"Uninstall scheduled task {VerifyTaskDefinition.FullPath}",
            "Remove verify-task opt-in marker and XML artifact",
        };
        actions.AddRange(DefaultRegistryKeys.Select(k => $"Delete registry key {k} (if present)"));
        if (!string.IsNullOrWhiteSpace(_programDataRoot))
        {
            actions.Add($"Optionally remove directory {_programDataRoot} (journals/backups/config)");
        }

        return new UninstallPlan
        {
            Actions = actions,
            RegistryKeys = DefaultRegistryKeys,
            VerifyTaskPath = VerifyTaskDefinition.FullPath,
        };
    }

    public UninstallResult Run(bool removeProgramData = false)
    {
        var completed = new List<string>();
        var skipped = new List<string>();

        var status = _verifyTask.GetStatus();
        _verifyTask.Uninstall();
        completed.Add(status.Installed
            ? $"Removed verify task artifacts for {VerifyTaskDefinition.FullPath}"
            : $"Verify task artifacts already absent ({VerifyTaskDefinition.FullPath})");

        foreach (var key in DefaultRegistryKeys)
        {
            if (_registry.TryDeleteKey(key))
            {
                completed.Add($"Deleted {key}");
            }
            else
            {
                skipped.Add($"Absent or locked: {key}");
            }
        }

        if (removeProgramData && !string.IsNullOrWhiteSpace(_programDataRoot) && Directory.Exists(_programDataRoot))
        {
            Directory.Delete(_programDataRoot, recursive: true);
            completed.Add($"Removed {_programDataRoot}");
        }
        else if (removeProgramData)
        {
            skipped.Add("ProgramData root not present");
        }

        return new UninstallResult
        {
            Ok = true,
            Completed = completed,
            Skipped = skipped,
            Detail = $"Uninstall finished. Task path {VerifyTaskDefinition.FullPath} cleared from installer state.",
        };
    }
}

public interface IRegistryKeyCleaner
{
    /// <summary>Returns true when a key was deleted.</summary>
    bool TryDeleteKey(string keyPath);
}

public sealed class NoOpRegistryKeyCleaner : IRegistryKeyCleaner
{
    public bool TryDeleteKey(string keyPath) => false;
}

/// <summary>
/// Records intended registry deletions for tests / offline planning.
/// Real Windows deletion is performed by uninstall.ps1 or Elevator when elevated.
/// </summary>
public sealed class RecordingRegistryKeyCleaner : IRegistryKeyCleaner
{
    private readonly HashSet<string> _existing;
    public List<string> Deleted { get; } = [];

    public RecordingRegistryKeyCleaner(IEnumerable<string>? existingKeys = null)
    {
        _existing = new HashSet<string>(existingKeys ?? [], StringComparer.OrdinalIgnoreCase);
    }

    public bool TryDeleteKey(string keyPath)
    {
        if (!_existing.Contains(keyPath))
        {
            return false;
        }

        _existing.Remove(keyPath);
        Deleted.Add(keyPath);
        return true;
    }
}
