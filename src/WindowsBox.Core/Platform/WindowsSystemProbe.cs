using System.Runtime.Versioning;
using WindowsBox.Core.Abstractions;
using MicrosoftWin32 = Microsoft.Win32;

namespace WindowsBox.Core.Platform;

/// <summary>
/// Lightweight role/hardware probe used to gate catalog recommendations.
/// Prefers registry and environment signals so it stays fast on cold start.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsSystemProbe : ISystemProbe
{
    public SystemProbeResult Probe()
    {
        var edition = ReadEdition();
        var build = Environment.OSVersion.Version.Build;
        var hasBattery = HasBattery();
        var isLaptop = hasBattery || IsChassisLaptop();

        return new SystemProbeResult
        {
            Edition = edition,
            Build = build,
            IsLaptop = isLaptop,
            HasBattery = hasBattery,
            HasBluetooth = KeyExists(@"SYSTEM\CurrentControlSet\Services\BTHPORT"),
            HasPrinter = HasAnyPrinter(),
            HasBiometric = KeyExists(@"SYSTEM\CurrentControlSet\Services\WbioSrvc"),
            HasWwan = KeyExists(@"SYSTEM\CurrentControlSet\Services\WwanSvc"),
            HasWebcam = DeviceClassPresent("{e5323777-f976-4f5b-9b55-b94699c46e44}"), // KSCATEGORY_VIDEO_CAMERA
            DomainJoined = IsDomainJoined(),
            MdmEnrolled = IsMdmEnrolled(),
            WindowsInsider = IsWindowsInsider(),
            XboxOrGamePass = KeyExists(@"SOFTWARE\Microsoft\XboxLive", hive: MicrosoftWin32.Registry.CurrentUser)
                             || PackageLikelyInstalled("Microsoft.XboxApp"),
            XboxController = false, // HID enumeration deferred; gated items stay optional.
            EnabledFeatures = ReadEnabledFeatures(),
        };
    }

    private static string ReadEdition()
    {
        using var key = MicrosoftWin32.Registry.LocalMachine.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        var product = key?.GetValue("EditionID") as string
                      ?? key?.GetValue("ProductName") as string
                      ?? "Unknown";
        return product;
    }

    private static bool HasBattery() =>
        // Presence of the Battery class under Enum\BATTERY is a cheap laptop signal
        // without pulling in WinForms SystemInformation.
        KeyExists(@"SYSTEM\CurrentControlSet\Enum\BATTERY");

    private static bool IsChassisLaptop()
    {
        using var key = MicrosoftWin32.Registry.LocalMachine.OpenSubKey(
            @"SYSTEM\CurrentControlSet\Control\SystemInformation");
        var form = key?.GetValue("SystemProductName") as string ?? string.Empty;
        return form.Contains("Laptop", StringComparison.OrdinalIgnoreCase)
               || form.Contains("Notebook", StringComparison.OrdinalIgnoreCase)
               || form.Contains("Surface", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasAnyPrinter()
    {
        try
        {
            using var key = MicrosoftWin32.Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Control\Print\Printers");
            return key?.GetSubKeyNames().Length > 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool DeviceClassPresent(string classGuid)
    {
        try
        {
            using var key = MicrosoftWin32.Registry.LocalMachine.OpenSubKey(
                $@"SYSTEM\CurrentControlSet\Control\DeviceClasses\{classGuid}");
            return key?.GetSubKeyNames().Length > 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsDomainJoined()
    {
        using var key = MicrosoftWin32.Registry.LocalMachine.OpenSubKey(
            @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters");
        var domain = key?.GetValue("Domain") as string;
        return !string.IsNullOrWhiteSpace(domain);
    }

    private static bool IsMdmEnrolled()
    {
        // Enrollment under HKLM\SOFTWARE\Microsoft\Enrollments\* with EnrollmentType != 0.
        try
        {
            using var root = MicrosoftWin32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Enrollments");
            if (root is null)
            {
                return false;
            }

            foreach (var sub in root.GetSubKeyNames())
            {
                using var enrollment = root.OpenSubKey(sub);
                var type = enrollment?.GetValue("EnrollmentType");
                if (type is int i && i != 0)
                {
                    return true;
                }
            }
        }
        catch
        {
            // Advisory only.
        }

        return false;
    }

    private static bool IsWindowsInsider()
    {
        using var key = MicrosoftWin32.Registry.LocalMachine.OpenSubKey(
            @"SOFTWARE\Microsoft\WindowsSelfHost\Applicability");
        var branch = key?.GetValue("BranchName") as string;
        return !string.IsNullOrWhiteSpace(branch);
    }

    private static bool PackageLikelyInstalled(string packageFamilyHint)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var packages = Path.Combine(localAppData, "Packages");
        if (!Directory.Exists(packages))
        {
            return false;
        }

        return Directory.EnumerateDirectories(packages)
            .Any(d => Path.GetFileName(d).StartsWith(packageFamilyHint, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlySet<string> ReadEnabledFeatures()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // Cheap feature hints used by catalog gates; DISM enumeration is slower and deferred.
        if (KeyExists(@"SYSTEM\CurrentControlSet\Services\vmms"))
        {
            set.Add("Microsoft-Hyper-V");
        }

        if (KeyExists(@"SYSTEM\CurrentControlSet\Services\LxssManager")
            || KeyExists(@"SYSTEM\CurrentControlSet\Services\WslService"))
        {
            set.Add("Microsoft-Windows-Subsystem-Linux");
        }

        if (KeyExists(@"SYSTEM\CurrentControlSet\Services\VmCompute"))
        {
            set.Add("Containers-DisposableClientVM"); // Sandbox-ish signal
        }

        return set;
    }

    private static bool KeyExists(string subKey, MicrosoftWin32.RegistryKey? hive = null)
    {
        hive ??= MicrosoftWin32.Registry.LocalMachine;
        try
        {
            using var key = hive.OpenSubKey(subKey);
            return key is not null;
        }
        catch
        {
            return false;
        }
    }
}
