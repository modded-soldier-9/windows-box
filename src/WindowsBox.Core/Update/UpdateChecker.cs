using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using WindowsBox.Core.Settings;

namespace WindowsBox.Core.Update;

public sealed class UpdateCheckResult
{
    public required bool Performed { get; init; }

    public required string Message { get; init; }

    public string? LatestTag { get; init; }

    public string? ReleaseUrl { get; init; }

    public bool UpdateAvailable { get; init; }

    public bool SkippedBecauseDisabled { get; init; }
}

/// <summary>
/// Contacts GitHub Releases only when the user has opted in via Settings.
/// Default path performs zero network I/O. Offline signed builds work with checks off.
/// </summary>
public sealed class UpdateChecker
{
    public const string DefaultReleasesApi =
        "https://api.github.com/repos/modded-soldier-9/windows-box/releases/latest";

    private readonly IAppSettingsStore _settings;
    private readonly Func<HttpClient>? _httpClientFactory;
    private readonly string _releasesApiUrl;
    private readonly string _currentVersion;

    public UpdateChecker(
        IAppSettingsStore settings,
        string currentVersion,
        Func<HttpClient>? httpClientFactory = null,
        string? releasesApiUrl = null)
    {
        _settings = settings;
        _currentVersion = currentVersion;
        _httpClientFactory = httpClientFactory;
        _releasesApiUrl = releasesApiUrl ?? DefaultReleasesApi;
    }

    /// <summary>
    /// Returns immediately without network when update checks are disabled (default)
    /// or when preference is Manual and <paramref name="userInitiated"/> is false.
    /// </summary>
    public async Task<UpdateCheckResult> CheckAsync(
        bool userInitiated = false,
        CancellationToken cancellationToken = default)
    {
        var settings = _settings.Load();
        if (!settings.CheckForUpdatesEnabled)
        {
            return new UpdateCheckResult
            {
                Performed = false,
                SkippedBecauseDisabled = true,
                Message =
                    "Update checks are off (default). Enable “Check for updates” in Settings to opt in. " +
                    "Offline signed builds work with this off.",
            };
        }

        if (!userInitiated && settings.UpdatePreference != UpdatePreference.Automatic)
        {
            return new UpdateCheckResult
            {
                Performed = false,
                SkippedBecauseDisabled = false,
                Message =
                    "Update preference is Manual. Use Check for updates in Settings, or switch to Automatic.",
            };
        }

        if (_httpClientFactory is null)
        {
            return new UpdateCheckResult
            {
                Performed = false,
                Message = "No HTTP transport configured; check skipped (tests / offline host).",
            };
        }

        using var client = _httpClientFactory();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("WindowsBox", _currentVersion));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        using var response = await client.GetAsync(_releasesApiUrl, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var release = await JsonSerializer.DeserializeAsync(
            stream,
            UpdateJsonContext.Default.GitHubReleaseDto,
            cancellationToken);

        var tag = release?.TagName?.Trim();
        settings.LastUpdateCheckUtc = DateTimeOffset.UtcNow;
        settings.LastSeenReleaseTag = tag;
        _settings.Save(settings);

        var available = !string.IsNullOrWhiteSpace(tag) &&
                        !string.Equals(tag, _currentVersion, StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(tag, "v" + _currentVersion, StringComparison.OrdinalIgnoreCase);

        return new UpdateCheckResult
        {
            Performed = true,
            LatestTag = tag,
            ReleaseUrl = release?.HtmlUrl,
            UpdateAvailable = available,
            Message = available
                ? $"Update available: {tag} (you have {_currentVersion}). Review the release notes before installing."
                : $"You are on {_currentVersion}; latest reported tag is {tag ?? "(none)"}."
        };
    }
}

internal sealed class GitHubReleaseDto
{
    [JsonPropertyName("tag_name")]
    public string? TagName { get; set; }

    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; set; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(GitHubReleaseDto))]
internal partial class UpdateJsonContext : JsonSerializerContext;
