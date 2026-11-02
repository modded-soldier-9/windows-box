using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using WindowsBox.Core.Abstractions;
using WindowsBox.Core.Catalog.Models;
using WindowsBox.Core.Platform.Native;

namespace WindowsBox.Core.Platform;

/// <summary>
/// Live SCM adapter. Reads and writes start type / delayed autostart / triggers / dependents.
/// Must never be exercised against a developer host by unit tests — use fakes.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsServiceController : IServiceController
{
    public bool Exists(string serviceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        using var manager = ServiceManagerHandle.Open(ScmNative.SC_MANAGER_CONNECT);
        using var service = ServiceHandle.TryOpen(manager, serviceName, ScmNative.SERVICE_QUERY_CONFIG);
        return service is not null;
    }

    public ServiceConfig? GetConfig(string serviceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        using var manager = ServiceManagerHandle.Open(ScmNative.SC_MANAGER_CONNECT);
        using var service = ServiceHandle.TryOpen(
            manager,
            serviceName,
            ScmNative.SERVICE_QUERY_CONFIG | ScmNative.SERVICE_QUERY_STATUS | ScmNative.SERVICE_ENUMERATE_DEPENDENTS);

        if (service is null)
        {
            return null;
        }

        var (startType, displayName, dependOn) = QueryBasicConfig(service.Handle);
        var delayed = QueryDelayed(service.Handle);
        var hasTriggers = QueryHasTriggers(service.Handle);
        var running = QueryIsRunning(service.Handle);
        var dependents = QueryDependents(service.Handle);

        var normalized = startType;
        if (normalized == ServiceStartType.Automatic && delayed)
        {
            normalized = ServiceStartType.AutomaticDelayed;
        }
        else if (normalized == ServiceStartType.Manual && hasTriggers)
        {
            normalized = ServiceStartType.ManualTrigger;
        }

        return new ServiceConfig
        {
            Name = serviceName,
            StartType = normalized is ServiceStartType.AutomaticDelayed or ServiceStartType.ManualTrigger
                ? (normalized == ServiceStartType.AutomaticDelayed ? ServiceStartType.Automatic : ServiceStartType.Manual)
                : normalized,
            DelayedAutostart = delayed,
            HasTriggers = hasTriggers,
            IsRunning = running,
            DependOnService = dependOn,
            DependentServices = dependents,
            DisplayName = displayName,
            Exists = true,
        };
    }

    public IReadOnlyList<string> EnumerateServices()
    {
        using var manager = ServiceManagerHandle.Open(
            ScmNative.SC_MANAGER_CONNECT | ScmNative.SC_MANAGER_ENUMERATE_SERVICE);

        uint resume = 0;
        var names = new List<string>();

        // First call to learn buffer size.
        _ = ScmNative.EnumServicesStatusExW(
            manager.Handle,
            infoLevel: 0,
            ScmNative.SERVICE_WIN32,
            ScmNative.SERVICE_STATE_ALL,
            nint.Zero,
            0,
            out var needed,
            out _,
            ref resume,
            null);

        var buffer = Marshal.AllocHGlobal((int)needed);
        try
        {
            resume = 0;
            if (!ScmNative.EnumServicesStatusExW(
                    manager.Handle,
                    infoLevel: 0,
                    ScmNative.SERVICE_WIN32,
                    ScmNative.SERVICE_STATE_ALL,
                    buffer,
                    needed,
                    out _,
                    out var returned,
                    ref resume,
                    null))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            var stride = Marshal.SizeOf<ScmNative.ENUM_SERVICE_STATUS_PROCESS>();
            for (var i = 0; i < returned; i++)
            {
                var entry = Marshal.PtrToStructure<ScmNative.ENUM_SERVICE_STATUS_PROCESS>(buffer + (i * stride));
                if (entry.lpServiceName != nint.Zero)
                {
                    names.Add(Marshal.PtrToStringUni(entry.lpServiceName)!);
                }
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }

        return names;
    }

    public void SetStartType(string serviceName, ServiceStartType startType, bool delayedAutostart = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        var scmStart = startType switch
        {
            ServiceStartType.Automatic or ServiceStartType.AutomaticDelayed => ScmNative.SERVICE_AUTO_START,
            ServiceStartType.Manual or ServiceStartType.ManualTrigger => ScmNative.SERVICE_DEMAND_START,
            ServiceStartType.Disabled => ScmNative.SERVICE_DISABLED,
            _ => throw new ArgumentOutOfRangeException(nameof(startType), startType, "Unknown start type."),
        };

        var wantDelayed = delayedAutostart || startType == ServiceStartType.AutomaticDelayed;

        using var manager = ServiceManagerHandle.Open(ScmNative.SC_MANAGER_CONNECT);
        using var service = ServiceHandle.Open(
            manager,
            serviceName,
            ScmNative.SERVICE_CHANGE_CONFIG | ScmNative.SERVICE_QUERY_CONFIG);

        if (!ScmNative.ChangeServiceConfigW(
                service.Handle,
                ScmNative.SERVICE_NO_CHANGE,
                scmStart,
                ScmNative.SERVICE_NO_CHANGE,
                null,
                null,
                nint.Zero,
                null,
                null,
                null,
                null))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), $"ChangeServiceConfig failed for {serviceName}");
        }

        // Delayed autostart only applies to Automatic.
        if (scmStart == ScmNative.SERVICE_AUTO_START)
        {
            var info = new ScmNative.SERVICE_DELAYED_AUTO_START_INFO { fDelayedAutostart = wantDelayed };
            var ptr = Marshal.AllocHGlobal(Marshal.SizeOf<ScmNative.SERVICE_DELAYED_AUTO_START_INFO>());
            try
            {
                Marshal.StructureToPtr(info, ptr, fDeleteOld: false);
                if (!ScmNative.ChangeServiceConfig2W(
                        service.Handle,
                        ScmNative.SERVICE_CONFIG_DELAYED_AUTO_START_INFO,
                        ptr))
                {
                    throw new Win32Exception(
                        Marshal.GetLastWin32Error(),
                        $"ChangeServiceConfig2 (delayed) failed for {serviceName}");
                }
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
    }

    private static (ServiceStartType StartType, string? DisplayName, IReadOnlyList<string> DependOn)
        QueryBasicConfig(nint service)
    {
        _ = ScmNative.QueryServiceConfigW(service, nint.Zero, 0, out var needed);
        var buffer = Marshal.AllocHGlobal((int)needed);
        try
        {
            if (!ScmNative.QueryServiceConfigW(service, buffer, needed, out _))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            var cfg = Marshal.PtrToStructure<ScmNative.QUERY_SERVICE_CONFIG>(buffer);
            var start = cfg.dwStartType switch
            {
                ScmNative.SERVICE_AUTO_START => ServiceStartType.Automatic,
                ScmNative.SERVICE_DEMAND_START => ServiceStartType.Manual,
                ScmNative.SERVICE_DISABLED => ServiceStartType.Disabled,
                _ => ServiceStartType.Manual,
            };

            string? display = cfg.lpDisplayName == nint.Zero
                ? null
                : Marshal.PtrToStringUni(cfg.lpDisplayName);

            var dependOn = ParseMultiString(cfg.lpDependencies);
            return (start, display, dependOn);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static bool QueryDelayed(nint service)
    {
        var size = (uint)Marshal.SizeOf<ScmNative.SERVICE_DELAYED_AUTO_START_INFO>();
        var buffer = Marshal.AllocHGlobal((int)size);
        try
        {
            if (!ScmNative.QueryServiceConfig2W(
                    service,
                    ScmNative.SERVICE_CONFIG_DELAYED_AUTO_START_INFO,
                    buffer,
                    size,
                    out _))
            {
                // Not all services expose delayed info; treat as false.
                return false;
            }

            var info = Marshal.PtrToStructure<ScmNative.SERVICE_DELAYED_AUTO_START_INFO>(buffer);
            return info.fDelayedAutostart;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static bool QueryHasTriggers(nint service)
    {
        _ = ScmNative.QueryServiceConfig2W(
            service,
            ScmNative.SERVICE_CONFIG_TRIGGER_INFO,
            nint.Zero,
            0,
            out var needed);

        if (needed == 0)
        {
            return false;
        }

        var buffer = Marshal.AllocHGlobal((int)needed);
        try
        {
            if (!ScmNative.QueryServiceConfig2W(
                    service,
                    ScmNative.SERVICE_CONFIG_TRIGGER_INFO,
                    buffer,
                    needed,
                    out _))
            {
                return false;
            }

            var info = Marshal.PtrToStructure<ScmNative.SERVICE_TRIGGER_INFO>(buffer);
            return info.cTriggers > 0;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static bool QueryIsRunning(nint service)
    {
        const int SC_STATUS_PROCESS_INFO = 0;
        var size = (uint)Marshal.SizeOf<ScmNative.SERVICE_STATUS_PROCESS>();
        var buffer = Marshal.AllocHGlobal((int)size);
        try
        {
            if (!ScmNative.QueryServiceStatusEx(service, SC_STATUS_PROCESS_INFO, buffer, size, out _))
            {
                return false;
            }

            var status = Marshal.PtrToStructure<ScmNative.SERVICE_STATUS_PROCESS>(buffer);
            // SERVICE_RUNNING = 4
            return status.dwCurrentState == 4;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static IReadOnlyList<string> QueryDependents(nint service)
    {
        _ = ScmNative.EnumDependentServicesW(
            service,
            ScmNative.SERVICE_STATE_ALL,
            nint.Zero,
            0,
            out var needed,
            out _);

        if (needed == 0)
        {
            return [];
        }

        var buffer = Marshal.AllocHGlobal((int)needed);
        try
        {
            if (!ScmNative.EnumDependentServicesW(
                    service,
                    ScmNative.SERVICE_STATE_ALL,
                    buffer,
                    needed,
                    out _,
                    out var returned))
            {
                return [];
            }

            var names = new List<string>((int)returned);
            var stride = Marshal.SizeOf<ScmNative.ENUM_SERVICE_STATUS>();
            for (var i = 0; i < returned; i++)
            {
                var entry = Marshal.PtrToStructure<ScmNative.ENUM_SERVICE_STATUS>(buffer + (i * stride));
                if (entry.lpServiceName != nint.Zero)
                {
                    names.Add(Marshal.PtrToStringUni(entry.lpServiceName)!);
                }
            }

            return names;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static IReadOnlyList<string> ParseMultiString(nint ptr)
    {
        if (ptr == nint.Zero)
        {
            return [];
        }

        var list = new List<string>();
        var offset = 0;
        while (true)
        {
            var s = Marshal.PtrToStringUni(ptr + offset);
            if (string.IsNullOrEmpty(s))
            {
                break;
            }

            list.Add(s);
            offset += (s.Length + 1) * 2;
        }

        return list;
    }

    private sealed class ServiceManagerHandle : IDisposable
    {
        public nint Handle { get; }

        private ServiceManagerHandle(nint handle) => Handle = handle;

        public static ServiceManagerHandle Open(uint access)
        {
            var handle = ScmNative.OpenSCManagerW(null, null, access);
            if (handle == nint.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "OpenSCManager failed");
            }

            return new ServiceManagerHandle(handle);
        }

        public void Dispose()
        {
            if (Handle != nint.Zero)
            {
                _ = ScmNative.CloseServiceHandle(Handle);
            }
        }
    }

    private sealed class ServiceHandle : IDisposable
    {
        public nint Handle { get; }

        private ServiceHandle(nint handle) => Handle = handle;

        public static ServiceHandle? TryOpen(ServiceManagerHandle manager, string name, uint access)
        {
            var handle = ScmNative.OpenServiceW(manager.Handle, name, access);
            return handle == nint.Zero ? null : new ServiceHandle(handle);
        }

        public static ServiceHandle Open(ServiceManagerHandle manager, string name, uint access)
        {
            var handle = ScmNative.OpenServiceW(manager.Handle, name, access);
            if (handle == nint.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"OpenService failed for {name}");
            }

            return new ServiceHandle(handle);
        }

        public void Dispose()
        {
            if (Handle != nint.Zero)
            {
                _ = ScmNative.CloseServiceHandle(Handle);
            }
        }
    }
}
