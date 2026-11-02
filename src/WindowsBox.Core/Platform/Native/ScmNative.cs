using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace WindowsBox.Core.Platform.Native;

/// <summary>
/// Minimal advapi32 surface for SCM read/write. Never invoked from unit tests —
/// fakes cover the pipeline; this is for Windows hosts / VM harnesses only.
/// </summary>
[SupportedOSPlatform("windows")]
internal static partial class ScmNative
{
    internal const uint SC_MANAGER_CONNECT = 0x0001;
    internal const uint SC_MANAGER_ENUMERATE_SERVICE = 0x0004;

    internal const uint SERVICE_QUERY_CONFIG = 0x0001;
    internal const uint SERVICE_QUERY_STATUS = 0x0004;
    internal const uint SERVICE_CHANGE_CONFIG = 0x0002;
    internal const uint SERVICE_ENUMERATE_DEPENDENTS = 0x0008;

    internal const uint SERVICE_NO_CHANGE = 0xFFFFFFFF;

    internal const uint SERVICE_CONFIG_DELAYED_AUTO_START_INFO = 3;
    internal const uint SERVICE_CONFIG_TRIGGER_INFO = 8;

    internal const uint SERVICE_AUTO_START = 0x00000002;
    internal const uint SERVICE_DEMAND_START = 0x00000003;
    internal const uint SERVICE_DISABLED = 0x00000004;

    internal const uint SERVICE_WIN32 = 0x00000030;
    internal const uint SERVICE_STATE_ALL = 0x00000003;
    internal const uint ERROR_MORE_DATA = 234;

    [LibraryImport("advapi32", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    internal static partial nint OpenSCManagerW(string? machineName, string? databaseName, uint desiredAccess);

    [LibraryImport("advapi32", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    internal static partial nint OpenServiceW(nint scManager, string serviceName, uint desiredAccess);

    [LibraryImport("advapi32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool CloseServiceHandle(nint handle);

    [LibraryImport("advapi32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool QueryServiceConfigW(
        nint service,
        nint serviceConfig,
        uint bufferSize,
        out uint bytesNeeded);

    [LibraryImport("advapi32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool QueryServiceConfig2W(
        nint service,
        uint infoLevel,
        nint buffer,
        uint bufferSize,
        out uint bytesNeeded);

    [LibraryImport("advapi32", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ChangeServiceConfigW(
        nint service,
        uint serviceType,
        uint startType,
        uint errorControl,
        string? binaryPathName,
        string? loadOrderGroup,
        nint tagId,
        string? dependencies,
        string? serviceStartName,
        string? password,
        string? displayName);

    [LibraryImport("advapi32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ChangeServiceConfig2W(nint service, uint infoLevel, nint info);

    [LibraryImport("advapi32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool QueryServiceStatusEx(
        nint service,
        int infoLevel,
        nint buffer,
        uint bufferSize,
        out uint bytesNeeded);

    [LibraryImport("advapi32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool EnumDependentServicesW(
        nint service,
        uint serviceState,
        nint buffer,
        uint bufferSize,
        out uint bytesNeeded,
        out uint servicesReturned);

    [LibraryImport("advapi32", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool EnumServicesStatusExW(
        nint scManager,
        uint infoLevel,
        uint serviceType,
        uint serviceState,
        nint buffer,
        uint bufferSize,
        out uint bytesNeeded,
        out uint servicesReturned,
        ref uint resumeHandle,
        string? groupName);

    [StructLayout(LayoutKind.Sequential)]
    internal struct QUERY_SERVICE_CONFIG
    {
        public uint dwServiceType;
        public uint dwStartType;
        public uint dwErrorControl;
        public nint lpBinaryPathName;
        public nint lpLoadOrderGroup;
        public uint dwTagId;
        public nint lpDependencies;
        public nint lpServiceStartName;
        public nint lpDisplayName;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SERVICE_STATUS
    {
        public uint dwServiceType;
        public uint dwCurrentState;
        public uint dwControlsAccepted;
        public uint dwWin32ExitCode;
        public uint dwServiceSpecificExitCode;
        public uint dwCheckPoint;
        public uint dwWaitHint;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SERVICE_STATUS_PROCESS
    {
        public uint dwServiceType;
        public uint dwCurrentState;
        public uint dwControlsAccepted;
        public uint dwWin32ExitCode;
        public uint dwServiceSpecificExitCode;
        public uint dwCheckPoint;
        public uint dwWaitHint;
        public uint dwProcessId;
        public uint dwServiceFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ENUM_SERVICE_STATUS
    {
        public nint lpServiceName;
        public nint lpDisplayName;
        public SERVICE_STATUS ServiceStatus;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SERVICE_DELAYED_AUTO_START_INFO
    {
        [MarshalAs(UnmanagedType.Bool)]
        public bool fDelayedAutostart;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SERVICE_TRIGGER_INFO
    {
        public uint cTriggers;
        public nint pTriggers;
        public nint pReserved;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct ENUM_SERVICE_STATUS_PROCESS
    {
        public nint lpServiceName;
        public nint lpDisplayName;
        public SERVICE_STATUS_PROCESS ServiceStatusProcess;
    }
}
