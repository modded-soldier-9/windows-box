using WindowsBox.Core.Abstractions;

namespace WindowsBox.Tests.Fakes;

public sealed class FakeSystemProbe : ISystemProbe
{
    public SystemProbeResult Result { get; set; } = new()
    {
        Edition = "Professional",
        Build = 26100,
        IsLaptop = false,
        HasBattery = false,
        HasBluetooth = false,
        HasPrinter = false,
        HasBiometric = false,
        HasWwan = false,
        HasWebcam = true,
        DomainJoined = false,
        MdmEnrolled = false,
        WindowsInsider = false,
        XboxOrGamePass = false,
        XboxController = false,
        EnabledFeatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
    };

    public SystemProbeResult Probe() => Result;
}
