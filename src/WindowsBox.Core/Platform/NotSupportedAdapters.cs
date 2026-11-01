using WindowsBox.Core.Abstractions;
using WindowsBox.Core.Catalog.Models;

namespace WindowsBox.Core.Platform;

/// <summary>
/// Placeholders returned when running off Windows. Real SCM/registry adapters ship in M1.
/// </summary>
public static class PlatformAdapters
{
    public static IServiceController CreateServiceController() =>
        OperatingSystem.IsWindows()
            ? throw new NotImplementedException("Windows SCM adapter lands in M1; use fakes in tests.")
            : new UnsupportedServiceController();

    public static IRegistry CreateRegistry() =>
        OperatingSystem.IsWindows()
            ? throw new NotImplementedException("Windows registry adapter lands in M1; use fakes in tests.")
            : new UnsupportedRegistry();

    public static ISystemProbe CreateSystemProbe() =>
        OperatingSystem.IsWindows()
            ? throw new NotImplementedException("Windows system probe lands in M1; use fakes in tests.")
            : new UnsupportedSystemProbe();
}

file sealed class UnsupportedServiceController : IServiceController
{
    public bool Exists(string serviceName) => false;

    public ServiceConfig? GetConfig(string serviceName) => null;

    public IReadOnlyList<string> EnumerateServices() => [];

    public void SetStartType(string serviceName, ServiceStartType startType, bool delayedAutostart = false) =>
        throw new PlatformNotSupportedException("Service control requires Windows.");
}

file sealed class UnsupportedRegistry : IRegistry
{
    public bool KeyExists(RegistryHive hive, string subKey) => false;

    public object? GetValue(RegistryHive hive, string subKey, string valueName) => null;

    public void SetValue(
        RegistryHive hive,
        string subKey,
        string valueName,
        object value,
        RegistryValueKind kind) =>
        throw new PlatformNotSupportedException("Registry access requires Windows.");

    public string ExportKey(RegistryHive hive, string subKey) =>
        throw new PlatformNotSupportedException("Registry export requires Windows.");
}

file sealed class UnsupportedSystemProbe : ISystemProbe
{
    public SystemProbeResult Probe() =>
        throw new PlatformNotSupportedException("System probing requires Windows.");
}
