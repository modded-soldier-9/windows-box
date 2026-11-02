using System.Text.Json;
using System.Text.Json.Serialization;
using WindowsBox.Core.Settings;

namespace WindowsBox.Core.Crash;

/// <summary>
/// Writes crash dumps locally. Upload is opt-in via Settings and off by default.
/// Offline signed builds never upload when the toggle is off.
/// </summary>
public sealed class CrashReporter
{
    private readonly string _dumpDirectory;
    private readonly IAppSettingsStore _settings;
    private readonly Func<HttpClient>? _httpClientFactory;
    private readonly string? _uploadEndpoint;

    public CrashReporter(
        string dumpDirectory,
        IAppSettingsStore settings,
        Func<HttpClient>? httpClientFactory = null,
        string? uploadEndpoint = null)
    {
        _dumpDirectory = Path.GetFullPath(dumpDirectory);
        Directory.CreateDirectory(_dumpDirectory);
        _settings = settings;
        _httpClientFactory = httpClientFactory;
        _uploadEndpoint = uploadEndpoint;
    }

    public string DumpDirectory => _dumpDirectory;

    public static string DefaultDumpDirectory()
    {
        if (OperatingSystem.IsWindows())
        {
            var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            return Path.Combine(programData, "WindowsBox", "crashes");
        }

        return Path.Combine(Path.GetTempPath(), "WindowsBox", "crashes");
    }

    public CrashDumpWriteResult WriteLocalDump(Exception exception, string? appVersion = null)
    {
        ArgumentNullException.ThrowIfNull(exception);
        var report = new CrashReport
        {
            Id = Guid.NewGuid().ToString("N"),
            OccurredAtUtc = DateTimeOffset.UtcNow,
            ExceptionType = exception.GetType().FullName ?? exception.GetType().Name,
            Message = exception.Message,
            StackTrace = exception.StackTrace,
            AppVersion = appVersion,
            OsDescription = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
        };

        var path = Path.Combine(_dumpDirectory, $"{report.Id}.json");
        using (var stream = File.Create(path))
        {
            JsonSerializer.Serialize(stream, report, CrashJsonContext.Default.CrashReport);
        }

        return new CrashDumpWriteResult { DumpPath = path, Report = report };
    }

    /// <summary>
    /// Uploads only when <see cref="AppSettings.CrashReportUploadEnabled"/> is true.
    /// Default path performs zero network I/O.
    /// </summary>
    public async Task<CrashUploadResult> TryUploadAsync(
        string dumpPath,
        CancellationToken cancellationToken = default)
    {
        var settings = _settings.Load();
        if (!settings.CrashReportUploadEnabled)
        {
            return new CrashUploadResult
            {
                Attempted = false,
                Uploaded = false,
                SkippedBecauseDisabled = true,
                Message =
                    "Crash report upload is off (default). Local dump kept on disk. " +
                    "Enable upload in Settings → Privacy only if you consent to send the dump.",
            };
        }

        if (_httpClientFactory is null || string.IsNullOrWhiteSpace(_uploadEndpoint))
        {
            return new CrashUploadResult
            {
                Attempted = false,
                Uploaded = false,
                Message = "Upload opted in, but no upload transport/endpoint is configured.",
            };
        }

        if (!File.Exists(dumpPath))
        {
            return new CrashUploadResult
            {
                Attempted = true,
                Uploaded = false,
                Message = $"Dump not found: {dumpPath}",
            };
        }

        using var client = _httpClientFactory();
        await using var stream = File.OpenRead(dumpPath);
        using var content = new StreamContent(stream);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        using var response = await client.PostAsync(_uploadEndpoint, content, cancellationToken);
        return new CrashUploadResult
        {
            Attempted = true,
            Uploaded = response.IsSuccessStatusCode,
            Message = response.IsSuccessStatusCode
                ? $"Uploaded crash dump ({(int)response.StatusCode})."
                : $"Upload failed: {(int)response.StatusCode} {response.ReasonPhrase}",
        };
    }

    public IReadOnlyList<string> ListLocalDumps() =>
        Directory.Exists(_dumpDirectory)
            ? Directory.EnumerateFiles(_dumpDirectory, "*.json").OrderByDescending(f => f).ToList()
            : [];
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true)]
[JsonSerializable(typeof(CrashReport))]
internal partial class CrashJsonContext : JsonSerializerContext;
