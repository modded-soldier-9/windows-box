using System.Runtime.Versioning;
using WindowsBox.Core.Abstractions;
using WindowsBox.Core.Catalog.Models;

namespace WindowsBox.Core.Platform;

/// <summary>
/// Factory for platform adapters. Real Windows SCM/registry/probe implementations
/// are returned on Windows; unsupported stubs elsewhere. Unit tests inject fakes.
/// </summary>
public static class PlatformAdapters
{
    public static IServiceController CreateServiceController()
    {
        if (OperatingSystem.IsWindows())
        {
            return CreateWindowsServiceController();
        }

        return new UnsupportedServiceController();
    }

    public static IRegistry CreateRegistry()
    {
        if (OperatingSystem.IsWindows())
        {
            return CreateWindowsRegistry();
        }

        return new UnsupportedRegistry();
    }

    public static ISystemProbe CreateSystemProbe()
    {
        if (OperatingSystem.IsWindows())
        {
            return CreateWindowsSystemProbe();
        }

        return new UnsupportedSystemProbe();
    }

    [SupportedOSPlatform("windows")]
    private static IServiceController CreateWindowsServiceController() => new WindowsServiceController();

    [SupportedOSPlatform("windows")]
    private static IRegistry CreateWindowsRegistry() => new WindowsRegistryAdapter();

    [SupportedOSPlatform("windows")]
    private static ISystemProbe CreateWindowsSystemProbe() => new WindowsSystemProbe();
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
