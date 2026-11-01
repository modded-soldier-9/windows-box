namespace WindowsBox.Core.Abstractions;

/// <summary>
/// Minimal registry abstraction for journaling prior values.
/// </summary>
public interface IRegistry
{
    bool KeyExists(RegistryHive hive, string subKey);

    object? GetValue(RegistryHive hive, string subKey, string valueName);

    void SetValue(RegistryHive hive, string subKey, string valueName, object value, RegistryValueKind kind);

    string ExportKey(RegistryHive hive, string subKey);
}

public enum RegistryHive
{
    LocalMachine,
    CurrentUser,
}

public enum RegistryValueKind
{
    String,
    DWord,
    QWord,
    MultiString,
    Binary,
    ExpandString,
}
