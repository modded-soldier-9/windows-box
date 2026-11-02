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
    public const string DefaultStableReleasesApi =
        "https://api.github.com/repos/modded-soldier-9/windows-box/releases/latest";

    public const string DefaultBetaReleasesApi =
        "https://api.github.com/repos/modded-soldier-9/windows-box/releases";

    private readonly IAppSettingsStore _settings;
    private readonly Func<HttpClient>? _httpClientFactory;
    private readonly string? _releasesApiUrlOverride;
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
        _releasesApiUrlOverride = releasesApiUrl;
    }

    public static string ResolveApiUrl(UpdateChannel channel) =>
        channel == UpdateChannel.Beta ? DefaultBetaReleasesApi : DefaultStableReleasesApi;

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

        var apiUrl = _releasesApiUrlOverride ?? ResolveApiUrl(settings.UpdateChannel);
        using var client = _httpClientFactory();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("WindowsBox", _currentVersion));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        using var response = await client.GetAsync(apiUrl, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        GitHubReleaseDto? release;
        if (settings.UpdateChannel == UpdateChannel.Beta && _releasesApiUrlOverride is null)
        {
            var releases = await JsonSerializer.DeserializeAsync(
                stream,
                UpdateJsonContext.Default.GitHubReleaseDtoArray,
                cancellationToken) ?? [];
            // Prefer prerelease when on Beta; fall back to newest.
            release = releases.FirstOrDefault(r => r.Prerelease) ?? releases.FirstOrDefault();
        }
        else
        {
            release = await JsonSerializer.DeserializeAsync(
                stream,
                UpdateJsonContext.Default.GitHubReleaseDto,
                cancellationToken);
        }

        var tag = release?.TagName?.Trim();
        settings.LastUpdateCheckUtc = DateTimeOffset.UtcNow;
        settings.LastSeenReleaseTag = tag;
        _settings.Save(settings);

        var available = !string.IsNullOrWhiteSpace(tag) &&
                        !string.Equals(tag, _currentVersion, StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(tag, "v" + _currentVersion, StringComparison.OrdinalIgnoreCase);

        var channelLabel = settings.UpdateChannel.ToString().ToLowerInvariant();
        return new UpdateCheckResult
        {
            Performed = true,
            LatestTag = tag,
            ReleaseUrl = release?.HtmlUrl,
            UpdateAvailable = available,
            Message = available
                ? $"Update available on {channelLabel}: {tag} (you have {_currentVersion}). Review release notes before installing."
                : $"Channel {channelLabel}: you are on {_currentVersion}; latest reported tag is {tag ?? "(none)"}."
        };
    }
}

internal sealed class GitHubReleaseDto
{
    [JsonPropertyName("tag_name")]
    public string? TagName { get; set; }

    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; set; }

    [JsonPropertyName("prerelease")]
    public bool Prerelease { get; set; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(GitHubReleaseDto))]
[JsonSerializable(typeof(GitHubReleaseDto[]))]
internal partial class UpdateJsonContext : JsonSerializerContext;
