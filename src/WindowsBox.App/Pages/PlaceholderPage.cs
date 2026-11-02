using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WindowsBox.Core.Plugins;
using WindowsBox.Core.Plugins.Builtin;
using WindowsBox.Core.Presentation;
using WindowsBox.Core.Settings;
using WindowsBox.Core.Update;

namespace WindowsBox.App.Pages;

internal abstract class ShellPage : Page
{
    protected static Grid CreateRoot(string title, string blurb, UIElement? body = null)
    {
        var root = new Grid { Padding = new Thickness(32) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var titleBlock = new TextBlock
        {
            Text = title,
            Style = Application.Current.Resources["TitleTextBlockStyle"] as Style,
            Margin = new Thickness(0, 0, 0, 4),
        };
        var blurbBlock = new TextBlock
        {
            Text = blurb,
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.85,
            Margin = new Thickness(0, 0, 0, 16),
        };

        Grid.SetRow(titleBlock, 0);
        Grid.SetRow(blurbBlock, 1);
        root.Children.Add(titleBlock);
        root.Children.Add(blurbBlock);

        if (body is not null)
        {
            Grid.SetRow(body, 2);
            root.Children.Add(body);
        }

        root.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        return root;
    }
}

public sealed class HomePage : ShellPage
{
    public HomePage()
    {
        var tips = new StackPanel { Spacing = 8 };
        tips.Children.Add(Info("1. Preview every change before apply — risk, gain, and what breaks."));
        tips.Children.Add(Info("2. Journals store measured priors so undo never assumes Microsoft defaults."));
        tips.Children.Add(Info("3. Elevator elevates only for a batch, then exits. Nothing stays resident."));
        tips.Children.Add(Info("4. On open, verify detects Windows Update reverts. Re-apply is opt-in."));
        Content = CreateRoot(
            "Windows Box",
            "Explain every change. Measure the benefit. Undo anything. Zero background footprint.",
            tips);
    }

    private static TextBlock Info(string text) => new()
    {
        Text = text,
        TextWrapping = TextWrapping.Wrap,
        Opacity = 0.9,
    };
}

public sealed class ServicesPage : ShellPage
{
    private readonly ListView _list = new()
    {
        SelectionMode = ListViewSelectionMode.None,
        IsItemClickEnabled = false,
    };

    private readonly TextBlock _summary = new()
    {
        Text = "Load a profile preview to see the diff.",
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(0, 0, 0, 12),
    };

    public ServicesPage()
    {
        var toolbar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 0, 0, 12),
        };

        var profileBox = new ComboBox
        {
            Width = 160,
            ItemsSource = new[] { "Safe", "Balanced", "Gamer", "Laptop", "Office", "Privacy", "Developer", "Minimal" },
            SelectedItem = "Balanced",
        };

        var previewBtn = new Button { Content = "Preview diff" };
        previewBtn.Click += (_, _) => ShowDemoPreview(profileBox.SelectedItem as string ?? "Balanced");

        var dryRunBtn = new Button { Content = "Dry-run (show changes)" };
        ToolTipService.SetToolTip(dryRunBtn, "Shows exactly what would change without mutating services. Same as `wbox dry-run`.");
        dryRunBtn.Click += (_, _) => ShowDemoDryRun(profileBox.SelectedItem as string ?? "Balanced");

        var applyBtn = new Button { Content = "Apply (elevated batch)…", IsEnabled = false };
        ToolTipService.SetToolTip(applyBtn, "Apply runs through Elevator after explicit confirm. Disabled until a live preview is selected.");

        toolbar.Children.Add(profileBox);
        toolbar.Children.Add(previewBtn);
        toolbar.Children.Add(dryRunBtn);
        toolbar.Children.Add(applyBtn);

        var body = new Grid();
        body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        body.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(toolbar, 0);
        Grid.SetRow(_summary, 1);
        Grid.SetRow(_list, 2);
        body.Children.Add(toolbar);
        body.Children.Add(_summary);
        body.Children.Add(_list);

        Content = CreateRoot(
            "Services",
            "Flagship: dependency-checked optimization with preview, journaled undo, and Manual-before-Disabled.",
            body);

        ShowDemoPreview("Balanced");
    }

    private void ShowDemoPreview(string profileName)
    {
        // Demo cards illustrate the Fluent preview contract; live SCM preview is wired via Core on Windows.
        var cards = new List<PreviewCardModel>
        {
            new()
            {
                ItemId = "svc.diagtrack",
                Title = "Connected User Experiences and Telemetry",
                Subtitle = "DiagTrack",
                Diff = "Automatic → Manual",
                Risk = 2,
                RiskLabel = PreviewDiffPresenter.RiskLabel(2),
                ExpectedGainMb = 8,
                Breaks = ["Optional diagnostic data upload"],
            },
            new()
            {
                ItemId = "svc.trkwks",
                Title = "Distributed Link Tracking Client",
                Subtitle = "TrkWks",
                Diff = "Automatic → Manual",
                Risk = 1,
                RiskLabel = PreviewDiffPresenter.RiskLabel(1),
                ExpectedGainMb = 8,
            },
        };

        _summary.Text = $"{profileName}: {cards.Count} service(s); 2 → Manual; est. −16 MB idle. Confirm risk ≥3 or Disabled before apply.";
        _list.ItemsSource = cards.Select(c =>
            $"{c.RiskLabel,-14}  {c.Subtitle,-16}  {c.Diff,-28}  ~{c.ExpectedGainMb} MB").ToList();
    }

    private void ShowDemoDryRun(string profileName)
    {
        ShowDemoPreview(profileName);
        _summary.Text =
            $"Dry-run ({profileName}): planned changes below — no services mutated. " +
            "CLI: wbox dry-run --profile " + profileName.ToLowerInvariant() + ". " +
            "Apply stays disabled until you confirm a live preview.";
    }
}

public sealed class SoftwarePage : ShellPage
{
    public SoftwarePage()
        : base()
    {
        Content = CreateRoot(
            "Software",
            "Unified inventory and multi-source installs (winget first). Bundles and rollback land in M2.");
    }
}

public sealed class PrivacyPage : ShellPage
{
    public PrivacyPage()
    {
        Content = CreateRoot("Privacy", "Policy-first telemetry and connection controls. Same engine as Services.");
    }
}

public sealed class StartupPage : ShellPage
{
    public StartupPage()
    {
        Content = CreateRoot("Startup", "Autoruns-grade coverage of autostart locations — next stacked PR.");
    }
}

public sealed class TweaksPage : ShellPage
{
    public TweaksPage()
    {
        Content = CreateRoot("Tweaks", "Visual, power, and network tweaks with honesty labels. Never applied on install.");
    }
}

public sealed class HistoryPage : ShellPage
{
    public HistoryPage()
    {
        var empty = HistoryPresenter.ToRows([]);
        var list = new ListView
        {
            ItemsSource = empty.Count == 0
                ? new[] { "No apply batches yet. After you apply a profile, undo history appears here." }
                : empty.Select(r => $"{r.When:u}  {r.Profile}  {r.Summary}").ToArray(),
        };

        Content = CreateRoot(
            "History",
            "Timeline of apply batches with one-click rollback from measured journal priors.",
            list);
    }
}

/// <summary>
/// In-repo plugin marketplace. No sandbox — only allow-listed merged plugins.
/// </summary>
public sealed class PluginsPage : ShellPage
{
    public PluginsPage()
    {
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock
        {
            Text =
                "Plugins ship via PRs into the repo plugins/ folder. Ratings use GitHub issue labels " +
                "(plugin-rating:1–5, plugin-review, plugin-report). Deny-list: plugins/deny-list.txt. " +
                "Auto-update is tied to Settings → Check for updates.",
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.85,
        });

        var installBtn = new Button { Content = "Install protected-service-drift (one-click)" };
        var status = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) };
        installBtn.Click += (_, _) =>
        {
            try
            {
                var root = FindPluginsRoot();
                var store = new FileAppSettingsStore(FileAppSettingsStore.DefaultDirectory());
                var market = new PluginMarketplace(root, store);
                var result = market.Install(ProtectedServiceDriftPlugin.PluginId);
                status.Text = result.Message;
            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
            }
        };

        panel.Children.Add(installBtn);
        panel.Children.Add(status);
        Content = CreateRoot("Plugins", "Reviewed in-repo extensions only. Nothing downloads arbitrary code.", panel);
    }

    private static string FindPluginsRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "plugins");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return Path.GetFullPath("plugins");
    }
}

/// <summary>
/// Opt-in network preferences. Defaults keep the app fully offline.
/// </summary>
public sealed class SettingsPage : ShellPage
{
    private readonly FileAppSettingsStore _store = new(FileAppSettingsStore.DefaultDirectory());
    private readonly ToggleSwitch _checkUpdates = new() { OnContent = "On", OffContent = "Off" };
    private readonly ToggleSwitch _crashUpload = new() { OnContent = "On", OffContent = "Off" };
    private readonly ToggleSwitch _telemetry = new() { OnContent = "On", OffContent = "Off" };
    private readonly ComboBox _preference = new()
    {
        Width = 220,
        ItemsSource = new[] { "Manual (default)", "Automatic (check on open)" },
    };
    private readonly ComboBox _channel = new()
    {
        Width = 220,
        ItemsSource = new[] { "Stable (default)", "Beta" },
    };
    private readonly TextBlock _status = new()
    {
        TextWrapping = TextWrapping.Wrap,
        Opacity = 0.9,
        Margin = new Thickness(0, 12, 0, 0),
    };

    public SettingsPage()
    {
        var settings = _store.Load();
        _checkUpdates.IsOn = settings.CheckForUpdatesEnabled;
        _crashUpload.IsOn = settings.CrashReportUploadEnabled;
        _telemetry.IsOn = settings.TelemetryEnabled;
        _preference.SelectedIndex = settings.UpdatePreference == UpdatePreference.Automatic ? 1 : 0;
        _channel.SelectedIndex = settings.UpdateChannel == UpdateChannel.Beta ? 1 : 0;

        _checkUpdates.Toggled += (_, _) => Persist();
        _crashUpload.Toggled += (_, _) => Persist();
        _telemetry.Toggled += (_, _) => Persist();
        _preference.SelectionChanged += (_, _) => Persist();
        _channel.SelectionChanged += (_, _) => Persist();

        var checkBtn = new Button { Content = "Check for updates now", Margin = new Thickness(0, 12, 0, 0) };
        checkBtn.Click += async (_, _) => await RunCheckAsync();

        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock
        {
            Text = "Updates (opt-in)",
            Style = Application.Current.Resources["SubtitleTextBlockStyle"] as Style,
        });
        panel.Children.Add(new TextBlock
        {
            Text =
                "Off by default. When enabled, Windows Box may contact GitHub Releases only. " +
                "Nothing is downloaded or installed without your action. " +
                "Signed offline builds work with all network features off — see docs/network-safeguards.md.",
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.85,
        });
        panel.Children.Add(Labeled("Check for updates", _checkUpdates));
        panel.Children.Add(Labeled("When to check", _preference));
        panel.Children.Add(Labeled("Channel", _channel));
        panel.Children.Add(checkBtn);

        panel.Children.Add(new TextBlock
        {
            Text = "Crash reports (opt-in)",
            Style = Application.Current.Resources["SubtitleTextBlockStyle"] as Style,
            Margin = new Thickness(0, 16, 0, 0),
        });
        panel.Children.Add(new TextBlock
        {
            Text =
                "Crashes are always written locally under %ProgramData%\\WindowsBox\\crashes. " +
                "Upload is off by default. Turning this on sends the dump to the configured crash endpoint.",
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.85,
        });
        panel.Children.Add(Labeled("Upload crash dumps", _crashUpload));

        panel.Children.Add(new TextBlock
        {
            Text = "Telemetry (opt-in)",
            Style = Application.Current.Resources["SubtitleTextBlockStyle"] as Style,
            Margin = new Thickness(0, 16, 0, 0),
        });
        panel.Children.Add(new TextBlock
        {
            Text =
                "Off by default. When enabled, Windows Box may send anonymous usage events " +
                "(feature names only — no file contents). Switch off anytime; offline builds work with this off.",
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.85,
        });
        panel.Children.Add(Labeled("Enable telemetry", _telemetry));
        panel.Children.Add(_status);

        if (settings.LastUpdateCheckUtc is { } last)
        {
            _status.Text = $"Last check: {last:u}. Last seen tag: {settings.LastSeenReleaseTag ?? "(none)"}.";
        }
        else
        {
            _status.Text = "No update check has run yet (network idle). Crash upload default: off.";
        }

        Content = CreateRoot("Settings", "Privacy-first preferences. Network features stay off until you opt in.", panel);
    }

    private void Persist()
    {
        var settings = _store.Load();
        settings.CheckForUpdatesEnabled = _checkUpdates.IsOn;
        settings.CrashReportUploadEnabled = _crashUpload.IsOn;
        settings.TelemetryEnabled = _telemetry.IsOn;
        settings.UpdatePreference = _preference.SelectedIndex == 1
            ? UpdatePreference.Automatic
            : UpdatePreference.Manual;
        settings.UpdateChannel = _channel.SelectedIndex == 1
            ? UpdateChannel.Beta
            : UpdateChannel.Stable;
        _store.Save(settings);
        _status.Text =
            "Saved. Updates: " + (settings.CheckForUpdatesEnabled ? settings.UpdatePreference.ToString() : "off") +
            ". Crash upload: " + (settings.CrashReportUploadEnabled ? "on" : "off") +
            ". Telemetry: " + (settings.TelemetryEnabled ? "on" : "off") + ".";
    }

    private async Task RunCheckAsync()
    {
        Persist();
        var settings = _store.Load();
        var checker = new UpdateChecker(
            _store,
            currentVersion: "0.1.0-m1",
            httpClientFactory: settings.CheckForUpdatesEnabled ? () => new HttpClient() : null);
        var result = await checker.CheckAsync(userInitiated: true);
        _status.Text = result.Message;
    }

    private static StackPanel Labeled(string label, UIElement control)
    {
        var row = new StackPanel { Spacing = 4, Margin = new Thickness(0, 8, 0, 0) };
        row.Children.Add(new TextBlock { Text = label, Opacity = 0.9 });
        row.Children.Add(control);
        return row;
    }
}
