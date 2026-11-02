namespace WindowsBox.Core.Scheduling;

/// <summary>
/// Declarative definition for the opt-in <c>\WindowsBox\Verify</c> scheduled task.
/// Runs <c>wbox verify --quiet</c> after Windows Update (event 19) and shortly after
/// startup. Notifies only on drift; re-apply is opt-in. Never a resident agent —
/// the task launches a one-shot process that exits.
/// </summary>
public sealed class VerifyTaskDefinition
{
    public const string TaskFolder = @"\WindowsBox";
    public const string TaskName = "Verify";
    public const string FullPath = @"\WindowsBox\Verify";

    /// <summary>Minutes after logon / startup before verify runs.</summary>
    public int StartupDelayMinutesMin { get; init; } = 2;

    public int StartupDelayMinutesMax { get; init; } = 5;

    /// <summary>Minutes after Windows Update success (Event Log Microsoft-Windows-WindowsUpdateClient/Operational Event ID 19).</summary>
    public int WindowsUpdateDelayMinutes { get; init; } = 10;

    public string Arguments { get; init; } = "verify --quiet";

    public string EventLog { get; init; } = "Microsoft-Windows-WindowsUpdateClient/Operational";

    public int WindowsUpdateEventId { get; init; } = 19;

    public string ToSchtasksXml(string wboxExecutablePath)
    {
        // Randomized startup delay expressed as the midpoint; Task Scheduler XML
        // uses a fixed Delay — operators may edit. WU trigger uses EventTrigger.
        var startupDelay = TimeSpan.FromMinutes((StartupDelayMinutesMin + StartupDelayMinutesMax) / 2.0);
        var wuDelay = TimeSpan.FromMinutes(WindowsUpdateDelayMinutes);
        var exe = System.Security.SecurityElement.Escape(wboxExecutablePath) ?? wboxExecutablePath;
        var args = System.Security.SecurityElement.Escape(Arguments) ?? Arguments;

        return
            $"""
            <?xml version="1.0" encoding="UTF-16"?>
            <Task version="1.4" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
              <RegistrationInfo>
                <Author>Windows Box</Author>
                <Description>One-shot drift verify after startup and Windows Update. Notifies only on drift; exits immediately. No resident agent.</Description>
              </RegistrationInfo>
              <Triggers>
                <LogonTrigger>
                  <Enabled>true</Enabled>
                  <Delay>PT{(int)startupDelay.TotalMinutes}M</Delay>
                </LogonTrigger>
                <EventTrigger>
                  <Enabled>true</Enabled>
                  <Subscription>&lt;QueryList&gt;&lt;Query Id="0" Path="{EventLog}"&gt;&lt;Select Path="{EventLog}"&gt;*[System[(EventID={WindowsUpdateEventId})]]&lt;/Select&gt;&lt;/Query&gt;&lt;/QueryList&gt;</Subscription>
                  <Delay>PT{(int)wuDelay.TotalMinutes}M</Delay>
                </EventTrigger>
              </Triggers>
              <Principals>
                <Principal id="Author">
                  <LogonType>InteractiveToken</LogonType>
                  <RunLevel>LeastPrivilege</RunLevel>
                </Principal>
              </Principals>
              <Settings>
                <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
                <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
                <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>
                <AllowHardTerminate>true</AllowHardTerminate>
                <StartWhenAvailable>true</StartWhenAvailable>
                <RunOnlyIfNetworkAvailable>false</RunOnlyIfNetworkAvailable>
                <AllowStartOnDemand>true</AllowStartOnDemand>
                <Enabled>true</Enabled>
                <Hidden>false</Hidden>
                <ExecutionTimeLimit>PT5M</ExecutionTimeLimit>
                <Priority>7</Priority>
              </Settings>
              <Actions Context="Author">
                <Exec>
                  <Command>{exe}</Command>
                  <Arguments>{args}</Arguments>
                </Exec>
              </Actions>
            </Task>
            """;
    }

    public string ToInstallCommand(string wboxExecutablePath) =>
        $"schtasks /Create /TN \"{FullPath.TrimStart('\\')}\" /XML \"{{xml-path}}\" /F";

    public string ToUninstallCommand() =>
        $"schtasks /Delete /TN \"{FullPath.TrimStart('\\')}\" /F";
}
