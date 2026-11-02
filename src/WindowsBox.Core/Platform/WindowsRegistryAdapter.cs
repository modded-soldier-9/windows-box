using System.Runtime.Versioning;
using System.Text;
using WindowsBox.Core.Abstractions;
using MicrosoftWin32 = Microsoft.Win32;

namespace WindowsBox.Core.Platform;

/// <summary>
/// Live registry adapter used to snapshot service keys before apply.
/// Unit tests must use <c>FakeRegistry</c>; this never runs in CI Linux.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsRegistryAdapter : IRegistry
{
    public bool KeyExists(RegistryHive hive, string subKey)
    {
        using var key = Open(hive, subKey, writable: false);
        return key is not null;
    }

    public object? GetValue(RegistryHive hive, string subKey, string valueName)
    {
        using var key = Open(hive, subKey, writable: false);
        return key?.GetValue(valueName);
    }

    public void SetValue(
        RegistryHive hive,
        string subKey,
        string valueName,
        object value,
        RegistryValueKind kind)
    {
        using var key = Open(hive, subKey, writable: true)
                        ?? throw new InvalidOperationException($"Registry key not found: {hive}\\{subKey}");

        key.SetValue(valueName, value, ToNativeKind(kind));
    }

    public string ExportKey(RegistryHive hive, string subKey)
    {
        using var key = Open(hive, subKey, writable: false);
        var sb = new StringBuilder();
        sb.AppendLine("Windows Registry Editor Version 5.00");
        sb.AppendLine();
        sb.Append('[').Append(ToHiveName(hive)).Append('\\').Append(subKey).AppendLine("]");

        if (key is null)
        {
            return sb.ToString();
        }

        foreach (var name in key.GetValueNames().OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
        {
            var value = key.GetValue(name, null, MicrosoftWin32.RegistryValueOptions.DoNotExpandEnvironmentNames);
            var kind = key.GetValueKind(name);
            sb.AppendLine(FormatValue(name, value, kind));
        }

        return sb.ToString();
    }

    private static MicrosoftWin32.RegistryKey? Open(RegistryHive hive, string subKey, bool writable)
    {
        var root = hive switch
        {
            RegistryHive.LocalMachine => MicrosoftWin32.Registry.LocalMachine,
            RegistryHive.CurrentUser => MicrosoftWin32.Registry.CurrentUser,
            _ => throw new ArgumentOutOfRangeException(nameof(hive), hive, null),
        };

        return root.OpenSubKey(subKey, writable);
    }

    private static string ToHiveName(RegistryHive hive) => hive switch
    {
        RegistryHive.LocalMachine => "HKEY_LOCAL_MACHINE",
        RegistryHive.CurrentUser => "HKEY_CURRENT_USER",
        _ => throw new ArgumentOutOfRangeException(nameof(hive), hive, null),
    };

    private static MicrosoftWin32.RegistryValueKind ToNativeKind(RegistryValueKind kind) => kind switch
    {
        RegistryValueKind.String => MicrosoftWin32.RegistryValueKind.String,
        RegistryValueKind.DWord => MicrosoftWin32.RegistryValueKind.DWord,
        RegistryValueKind.QWord => MicrosoftWin32.RegistryValueKind.QWord,
        RegistryValueKind.MultiString => MicrosoftWin32.RegistryValueKind.MultiString,
        RegistryValueKind.Binary => MicrosoftWin32.RegistryValueKind.Binary,
        RegistryValueKind.ExpandString => MicrosoftWin32.RegistryValueKind.ExpandString,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    private static string FormatValue(string name, object? value, MicrosoftWin32.RegistryValueKind kind)
    {
        var displayName = string.IsNullOrEmpty(name) ? "@" : $"\"{Escape(name)}\"";
        return kind switch
        {
            MicrosoftWin32.RegistryValueKind.DWord when value is int i =>
                $"{displayName}=dword:{i:x8}",
            MicrosoftWin32.RegistryValueKind.DWord when value is uint u =>
                $"{displayName}=dword:{u:x8}",
            MicrosoftWin32.RegistryValueKind.QWord when value is long l =>
                $"{displayName}=hex(b):{FormatHex(BitConverter.GetBytes(l))}",
            MicrosoftWin32.RegistryValueKind.Binary when value is byte[] bytes =>
                $"{displayName}=hex:{FormatHex(bytes)}",
            MicrosoftWin32.RegistryValueKind.ExpandString =>
                $"{displayName}=hex(2):{FormatHex(Encoding.Unicode.GetBytes(Convert.ToString(value) ?? string.Empty + "\0"))}",
            MicrosoftWin32.RegistryValueKind.MultiString when value is string[] parts =>
                $"{displayName}=hex(7):{FormatHex(Encoding.Unicode.GetBytes(string.Join("\0", parts) + "\0\0"))}",
            _ => $"{displayName}=\"{Escape(Convert.ToString(value) ?? string.Empty)}\"",
        };
    }

    private static string FormatHex(byte[] bytes) =>
        string.Join(",", bytes.Select(b => b.ToString("x2")));

    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
