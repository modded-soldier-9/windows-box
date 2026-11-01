using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace WindowsBox.App.Pages;

internal abstract class PlaceholderPage : Page
{
    protected PlaceholderPage(string title, string blurb)
    {
        var root = new Grid { Padding = new Thickness(32) };
        var stack = new StackPanel { Spacing = 12 };
        stack.Children.Add(new TextBlock
        {
            Text = title,
            Style = Application.Current.Resources["TitleTextBlockStyle"] as Style,
        });
        stack.Children.Add(new TextBlock
        {
            Text = blurb,
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.85,
        });
        stack.Children.Add(new TextBlock
        {
            Text = "Placeholder — wired in a later milestone. No changes run on install; nothing phones home.",
            FontSize = 12,
            Opacity = 0.6,
            Margin = new Thickness(0, 12, 0, 0),
        });
        root.Children.Add(stack);
        Content = root;
        Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
    }
}

public sealed class HomePage : PlaceholderPage
{
    public HomePage()
        : base(
            "Windows Box",
            "Explain every change. Measure the benefit. Undo anything. Zero background footprint.")
    {
    }
}

public sealed class ServicesPage : PlaceholderPage
{
    public ServicesPage()
        : base(
            "Services",
            "Flagship: dependency-checked service optimization with preview, journaled undo, and Manual-before-Disabled defaults.")
    {
    }
}

public sealed class SoftwarePage : PlaceholderPage
{
    public SoftwarePage()
        : base(
            "Software",
            "Unified inventory and multi-source installs (winget first). Bundles and rollback land in M2.")
    {
    }
}

public sealed class PrivacyPage : PlaceholderPage
{
    public PrivacyPage()
        : base("Privacy", "Policy-first telemetry and connection controls. Same engine as Services.")
    {
    }
}

public sealed class StartupPage : PlaceholderPage
{
    public StartupPage()
        : base("Startup", "Autoruns-grade coverage of autostart locations — coming after Services MVP.")
    {
    }
}

public sealed class TweaksPage : PlaceholderPage
{
    public TweaksPage()
        : base("Tweaks", "Visual, power, and network tweaks with honesty labels. Never applied on install.")
    {
    }
}

public sealed class HistoryPage : PlaceholderPage
{
    public HistoryPage()
        : base("History", "Timeline of apply batches with one-click rollback from the journal.")
    {
    }
}
