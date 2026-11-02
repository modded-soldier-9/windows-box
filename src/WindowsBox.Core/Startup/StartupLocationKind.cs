namespace WindowsBox.Core.Startup;

/// <summary>
/// Autoruns-inspired coverage of Windows autostart locations.
/// Risk 0–4 matches the shared engine scale.
/// </summary>
public enum StartupLocationKind
{
    RunKeyHkcu,
    RunKeyHklm,
    RunOnceHkcu,
    RunOnceHklm,
    StartupFolderUser,
    StartupFolderCommon,
    ScheduledTask,
    Service,
    Driver,
    ShellExtension,
    WinlogonUserinit,
    AppInitDlls,
    ExplorerLoad,
}
