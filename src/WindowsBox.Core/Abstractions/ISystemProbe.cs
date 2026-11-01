namespace WindowsBox.Core.Abstractions;

/// <summary>
/// Hardware/role probes that gate catalog recommendations.
/// </summary>
public interface ISystemProbe
{
    SystemProbeResult Probe();
}

public sealed class SystemProbeResult
{
    public required string Edition { get; init; }

    public required int Build { get; init; }

    public bool IsLaptop { get; init; }

    public bool HasBattery { get; init; }

    public bool HasBluetooth { get; init; }

    public bool HasPrinter { get; init; }

    public bool HasBiometric { get; init; }

    public bool HasWwan { get; init; }

    public bool HasWebcam { get; init; }

    public bool DomainJoined { get; init; }

    public bool MdmEnrolled { get; init; }

    public bool WindowsInsider { get; init; }

    public bool XboxOrGamePass { get; init; }

    public bool XboxController { get; init; }

    public IReadOnlySet<string> EnabledFeatures { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public bool HasRole(string role) => role.ToLowerInvariant() switch
    {
        "mdm_enrolled" => MdmEnrolled,
        "domain_joined" => DomainJoined,
        "windows_insider" => WindowsInsider,
        "xbox_or_gamepass" => XboxOrGamePass,
        "xbox_controller" => XboxController,
        "printer_installed" => HasPrinter,
        "bluetooth_radio" => HasBluetooth,
        "biometric" => HasBiometric,
        "wwan" => HasWwan,
        "laptop_or_tablet" => IsLaptop || HasBattery,
        "desktop" => !IsLaptop && !HasBattery,
        _ => false,
    };
}
