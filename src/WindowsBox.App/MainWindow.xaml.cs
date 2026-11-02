using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WindowsBox.App.Pages;

namespace WindowsBox.App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        RootNav.SelectedItem = RootNav.MenuItems[0];
        ContentFrame.Navigate(typeof(HomePage));
    }

    private void RootNav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ContentFrame.Navigate(typeof(SettingsPage));
            return;
        }

        if (args.SelectedItem is not NavigationViewItem item || item.Tag is not string tag)
        {
            return;
        }

        var pageType = tag switch
        {
            "home" => typeof(HomePage),
            "services" => typeof(ServicesPage),
            "software" => typeof(SoftwarePage),
            "privacy" => typeof(PrivacyPage),
            "startup" => typeof(StartupPage),
            "tweaks" => typeof(TweaksPage),
            "history" => typeof(HistoryPage),
            "settings" => typeof(SettingsPage),
            _ => typeof(HomePage),
        };

        ContentFrame.Navigate(pageType);
    }
}
