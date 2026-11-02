using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WindowsBox.Core.Presentation;

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

        var applyBtn = new Button { Content = "Apply (elevated batch)…", IsEnabled = false };
        ToolTipService.SetToolTip(applyBtn, "Apply runs through Elevator after explicit confirm. Disabled until a live preview is selected.");

        toolbar.Children.Add(profileBox);
        toolbar.Children.Add(previewBtn);
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
