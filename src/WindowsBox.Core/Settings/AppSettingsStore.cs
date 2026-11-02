using System.Text.Json;
using System.Text.Json.Serialization;
using WindowsBox.Core.Logging;

namespace WindowsBox.Core.Settings;

public interface IAppSettingsStore
{
    AppSettings Load();

    void Save(AppSettings settings);
}

/// <summary>
/// Local JSON settings under the given root. Never phones home.
/// </summary>
public sealed class FileAppSettingsStore : IAppSettingsStore
{
    private readonly string _path;

    public FileAppSettingsStore(string settingsDirectory)
    {
        var root = Path.GetFullPath(settingsDirectory);
        Directory.CreateDirectory(root);
        _path = Path.Combine(root, "settings.json");
    }

    public string PathOnDisk => _path;

    public AppSettings Load()
    {
        if (!File.Exists(_path))
        {
            return new AppSettings();
        }

        try
        {
            using var stream = File.OpenRead(_path);
            return JsonSerializer.Deserialize(stream, SettingsJsonContext.Default.AppSettings)
                   ?? new AppSettings();
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.SchemaVersionValue = AppSettings.SchemaVersion;
        var temp = _path + ".tmp";
        using (var stream = File.Create(temp))
        {
            JsonSerializer.Serialize(stream, settings, SettingsJsonContext.Default.AppSettings);
            stream.Flush();
        }

        File.Move(temp, _path, overwrite: true);
    }

    public static string DefaultDirectory()
    {
        if (OperatingSystem.IsWindows())
        {
            var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            return Path.Combine(programData, "WindowsBox", "config");
        }

        return Path.Combine(Path.GetTempPath(), "WindowsBox", "config");
    }
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(UpdatePreference))]
[JsonSerializable(typeof(UpdateChannel))]
[JsonSerializable(typeof(LogLevel))]
internal partial class SettingsJsonContext : JsonSerializerContext;
