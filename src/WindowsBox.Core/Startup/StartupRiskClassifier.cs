namespace WindowsBox.Core.Startup;

public static class StartupRiskClassifier
{
    public static int Classify(StartupLocationKind location, bool isMicrosoft, string command)
    {
        if (location is StartupLocationKind.WinlogonUserinit or StartupLocationKind.AppInitDlls)
        {
            return 4;
        }

        if (location is StartupLocationKind.Driver)
        {
            return isMicrosoft ? 0 : 3;
        }

        if (location is StartupLocationKind.ShellExtension)
        {
            return isMicrosoft ? 1 : 3;
        }

        if (location is StartupLocationKind.Service)
        {
            return isMicrosoft ? 0 : 2;
        }

        if (LooksLikeScriptHost(command))
        {
            return 3;
        }

        if (isMicrosoft)
        {
            return location is StartupLocationKind.RunOnceHkcu or StartupLocationKind.RunOnceHklm ? 1 : 0;
        }

        return location switch
        {
            StartupLocationKind.ScheduledTask => 2,
            StartupLocationKind.StartupFolderUser or StartupLocationKind.StartupFolderCommon => 2,
            _ => 2,
        };
    }

    private static bool LooksLikeScriptHost(string command)
    {
        var lower = command.ToLowerInvariant();
        return lower.Contains("powershell", StringComparison.Ordinal)
               || lower.Contains("wscript", StringComparison.Ordinal)
               || lower.Contains("cscript", StringComparison.Ordinal)
               || lower.Contains("mshta", StringComparison.Ordinal)
               || lower.Contains("cmd.exe /c", StringComparison.Ordinal);
    }
}
