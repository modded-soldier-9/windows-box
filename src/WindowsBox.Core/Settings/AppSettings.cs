namespace WindowsBox.Core.Settings;

/// <summary>
/// User preferences. All network features default OFF so a signed offline build
/// works with zero phone-home. See <c>docs/network-safeguards.md</c>.
/// </summary>
public sealed class AppSettings
{
    public const int SchemaVersion = 1;

    public int SchemaVersionValue { get; set; } = SchemaVersion;

    /// <summary>
    /// When false (default), the app never contacts GitHub Releases or any update endpoint.
    /// Opt-in only; disclosed in Settings.
    /// </summary>
    public bool CheckForUpdatesEnabled { get; set; }

    /// <summary>
    /// Manual (default): user must click Check. Automatic: check on app open only when
    /// <see cref="CheckForUpdatesEnabled"/> is also true. Never installs without consent.
    /// </summary>
    public UpdatePreference UpdatePreference { get; set; } = UpdatePreference.Manual;

    public DateTimeOffset? LastUpdateCheckUtc { get; set; }

    public string? LastSeenReleaseTag { get; set; }
}

public enum UpdatePreference
{
    /// <summary>User clicks Check for updates. Default.</summary>
    Manual = 0,

    /// <summary>Check on app open when update checks are opted in. Still no silent install.</summary>
    Automatic = 1,
}
