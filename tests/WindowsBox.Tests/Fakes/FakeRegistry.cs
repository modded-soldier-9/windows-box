using WindowsBox.Core.Abstractions;

namespace WindowsBox.Tests.Fakes;

public sealed class FakeRegistry : IRegistry
{
    private readonly Dictionary<string, Dictionary<string, object>> _keys =
        new(StringComparer.OrdinalIgnoreCase);

    private static string Key(RegistryHive hive, string subKey) => $"{hive}:{subKey}";

    public void Seed(RegistryHive hive, string subKey, string valueName, object value)
    {
        var k = Key(hive, subKey);
        if (!_keys.TryGetValue(k, out var values))
        {
            values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            _keys[k] = values;
        }

        values[valueName] = value;
    }

    public bool KeyExists(RegistryHive hive, string subKey) => _keys.ContainsKey(Key(hive, subKey));

    public object? GetValue(RegistryHive hive, string subKey, string valueName)
    {
        if (_keys.TryGetValue(Key(hive, subKey), out var values) &&
            values.TryGetValue(valueName, out var value))
        {
            return value;
        }

        return null;
    }

    public void SetValue(
        RegistryHive hive,
        string subKey,
        string valueName,
        object value,
        RegistryValueKind kind)
    {
        Seed(hive, subKey, valueName, value);
    }

    public string ExportKey(RegistryHive hive, string subKey)
    {
        if (!_keys.TryGetValue(Key(hive, subKey), out var values))
        {
            return $"Windows Registry Editor Version 5.00\r\n\r\n[{hive}\\{subKey}]\r\n";
        }

        var lines = new List<string>
        {
            "Windows Registry Editor Version 5.00",
            string.Empty,
            $"[{hive}\\{subKey}]",
        };

        foreach (var (name, value) in values)
        {
            lines.Add(value is int i ? $"\"{name}\"=dword:{i:x8}" : $"\"{name}\"=\"{value}\"");
        }

        return string.Join("\r\n", lines) + "\r\n";
    }
}
