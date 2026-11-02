namespace WindowsBox.Core.Startup;

public sealed class StartupEntry
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required StartupLocationKind Location { get; init; }

    public required string Command { get; init; }

    public string? Publisher { get; init; }

    public bool IsMicrosoft { get; init; }

    public bool Enabled { get; init; } = true;

    /// <summary>Risk 0 (leave alone) … 4 (login/security critical).</summary>
    public int Risk { get; init; }

    public string RiskLabel => Risk switch
    {
        0 => "Never touched",
        1 => "Safe",
        2 => "Situational",
        3 => "Advanced",
        4 => "Dangerous",
        _ => $"Risk {Risk}",
    };

    public string? SignatureStatus { get; init; }

    public string LocationLabel => Location switch
    {
        StartupLocationKind.RunKeyHkcu => @"HKCU\...\Run",
        StartupLocationKind.RunKeyHklm => @"HKLM\...\Run",
        StartupLocationKind.RunOnceHkcu => @"HKCU\...\RunOnce",
        StartupLocationKind.RunOnceHklm => @"HKLM\...\RunOnce",
        StartupLocationKind.StartupFolderUser => "Startup folder (user)",
        StartupLocationKind.StartupFolderCommon => "Startup folder (common)",
        StartupLocationKind.ScheduledTask => "Scheduled task",
        StartupLocationKind.Service => "Service (Auto)",
        StartupLocationKind.Driver => "Driver",
        StartupLocationKind.ShellExtension => "Shell extension",
        StartupLocationKind.WinlogonUserinit => "Winlogon Userinit",
        StartupLocationKind.AppInitDlls => "AppInit_DLLs",
        StartupLocationKind.ExplorerLoad => "Explorer Load",
        _ => Location.ToString(),
    };
}
