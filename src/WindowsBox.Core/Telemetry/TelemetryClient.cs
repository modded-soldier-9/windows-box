using WindowsBox.Core.Settings;

namespace WindowsBox.Core.Telemetry;

public sealed class TelemetryEvent
{
    public required string Name { get; init; }

    public IReadOnlyDictionary<string, string>? Properties { get; init; }

    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class TelemetrySendResult
{
    public required bool Sent { get; init; }

    public required string Message { get; init; }

    public bool SkippedBecauseDisabled { get; init; }
}

/// <summary>
/// Optional product telemetry. Off by default, switchable in Settings.
/// When disabled, <see cref="TrackAsync"/> is a no-op with zero network I/O.
/// </summary>
public sealed class TelemetryClient
{
    private readonly IAppSettingsStore _settings;
    private readonly Func<HttpClient>? _httpClientFactory;
    private readonly string? _endpoint;
    private readonly List<TelemetryEvent> _localBuffer = [];

    public TelemetryClient(
        IAppSettingsStore settings,
        Func<HttpClient>? httpClientFactory = null,
        string? endpoint = null)
    {
        _settings = settings;
        _httpClientFactory = httpClientFactory;
        _endpoint = endpoint;
    }

    public IReadOnlyList<TelemetryEvent> LocalBuffer => _localBuffer;

    public async Task<TelemetrySendResult> TrackAsync(
        TelemetryEvent telemetryEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(telemetryEvent);
        var settings = _settings.Load();
        if (!settings.TelemetryEnabled)
        {
            return new TelemetrySendResult
            {
                Sent = false,
                SkippedBecauseDisabled = true,
                Message =
                    "Telemetry is off (default). Enable in Settings → Privacy to opt in. " +
                    "Offline signed builds work with telemetry off.",
            };
        }

        _localBuffer.Add(telemetryEvent);

        if (_httpClientFactory is null || string.IsNullOrWhiteSpace(_endpoint))
        {
            return new TelemetrySendResult
            {
                Sent = false,
                Message = "Telemetry opted in; event buffered locally (no endpoint configured).",
            };
        }

        using var client = _httpClientFactory();
        using var content = new StringContent(
            $"{{\"name\":\"{telemetryEvent.Name}\",\"ts\":\"{telemetryEvent.TimestampUtc:o}\"}}",
            System.Text.Encoding.UTF8,
            "application/json");
        using var response = await client.PostAsync(_endpoint, content, cancellationToken);
        return new TelemetrySendResult
        {
            Sent = response.IsSuccessStatusCode,
            Message = response.IsSuccessStatusCode
                ? "Telemetry event sent."
                : $"Telemetry send failed: {(int)response.StatusCode}",
        };
    }
}
