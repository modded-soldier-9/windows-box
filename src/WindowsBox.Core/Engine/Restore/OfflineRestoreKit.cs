using System.Text;
using WindowsBox.Core.Engine.Journal;

namespace WindowsBox.Core.Engine.Restore;

/// <summary>
/// Writes Safe Mode / WinRE recoverable .reg + .cmd siblings next to journals.
/// Nothing scheduled; pure offline artifacts under %ProgramData%\WindowsBox\backups.
/// </summary>
public sealed class OfflineRestoreKit
{
    private readonly string _root;

    public OfflineRestoreKit(string backupRootDirectory)
    {
        _root = Path.GetFullPath(backupRootDirectory);
        Directory.CreateDirectory(_root);
    }

    public OfflineRestoreArtifacts Write(ApplyJournal journal)
    {
        var dir = Path.Combine(_root, journal.Id);
        Directory.CreateDirectory(dir);

        var regPath = Path.Combine(dir, "restore-services.reg");
        var cmdPath = Path.Combine(dir, "restore-services.cmd");
        var readmePath = Path.Combine(dir, "README.txt");

        var reg = new StringBuilder();
        reg.AppendLine("Windows Registry Editor Version 5.00");
        reg.AppendLine();
        reg.AppendLine("; Windows Box offline restore — measured priors from journal " + journal.Id);
        reg.AppendLine("; Generated " + DateTimeOffset.UtcNow.ToString("u"));
        reg.AppendLine();

        var cmd = new StringBuilder();
        cmd.AppendLine("@echo off");
        cmd.AppendLine("REM Windows Box offline restore for journal " + journal.Id);
        cmd.AppendLine("REM Run from an elevated cmd.exe (Safe Mode / WinRE OK).");
        cmd.AppendLine("setlocal");

        foreach (var entry in journal.Entries)
        {
            if (!string.IsNullOrWhiteSpace(entry.RegistryExport))
            {
                reg.AppendLine($"; --- {entry.ServiceName} ({entry.ItemId}) prior={entry.PriorStartType} ---");
                reg.AppendLine(entry.RegistryExport.TrimEnd());
                reg.AppendLine();
            }

            var start = ToScStartType(entry.PriorStartType);
            cmd.AppendLine($"sc.exe config \"{entry.ServiceName}\" start= {start}");
            if (entry.PriorDelayedAutostart &&
                entry.PriorStartType is Catalog.Models.ServiceStartType.Automatic
                    or Catalog.Models.ServiceStartType.AutomaticDelayed)
            {
                cmd.AppendLine(
                    $"reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\{entry.ServiceName}\" /v DelayedAutostart /t REG_DWORD /d 1 /f >nul");
            }
        }

        cmd.AppendLine("echo Restore commands completed. Reboot recommended.");
        cmd.AppendLine("endlocal");

        File.WriteAllText(regPath, reg.ToString(), Encoding.UTF8);
        File.WriteAllText(cmdPath, cmd.ToString(), Encoding.ASCII);
        File.WriteAllText(
            readmePath,
            """
            Windows Box offline restore kit
            ===============================
            1. Prefer restore-services.cmd from an elevated Command Prompt
               (Safe Mode or WinRE work when the app cannot start).
            2. restore-services.reg contains the measured service key export
               taken before apply — import only if you understand the contents.
            3. No network calls. No background agent. Delete this folder when done.
            """,
            Encoding.UTF8);

        return new OfflineRestoreArtifacts
        {
            Directory = dir,
            RegistryFile = regPath,
            CommandFile = cmdPath,
            ReadmeFile = readmePath,
        };
    }

    private static string ToScStartType(Catalog.Models.ServiceStartType startType) => startType switch
    {
        Catalog.Models.ServiceStartType.Automatic => "auto",
        Catalog.Models.ServiceStartType.AutomaticDelayed => "delayed-auto",
        Catalog.Models.ServiceStartType.Manual => "demand",
        Catalog.Models.ServiceStartType.ManualTrigger => "demand",
        Catalog.Models.ServiceStartType.Disabled => "disabled",
        _ => throw new ArgumentOutOfRangeException(nameof(startType), startType, null),
    };
}

public sealed class OfflineRestoreArtifacts
{
    public required string Directory { get; init; }

    public required string RegistryFile { get; init; }

    public required string CommandFile { get; init; }

    public required string ReadmeFile { get; init; }
}
