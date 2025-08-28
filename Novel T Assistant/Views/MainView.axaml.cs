using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Novel_T_Assistant.ViewModels;

namespace Novel_T_Assistant.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    // Navigation to Text Editor
    private async void OnTextEditorClick(object sender, PointerPressedEventArgs e)
    {
        var owner = this.VisualRoot as Window;

        // Create the text editor view
        var view = new TextEditorView();

        var window = new Window
        {
            Title = "Novel T Assistant - Text Editor",
            Content = view,
            Width = 1000,
            Height = 700,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        if (owner is not null)
            window.Show(); // Use Show() instead of ShowDialog() so it's not modal
        else
            window.Show();
    }

    // Navigation to Chapter Planner
    private async void OnChapterPlannerClick(object sender, PointerPressedEventArgs e)
    {
        // TODO: Navigate to chapter planner view
        ShowNotImplementedDialog("Chapter Planner");

        // When implemented:
        // var window = new ChapterPlannerWindow();
        // window.Show();
    }

    // Navigation to Characters Codex
    private async void OnCharactersClick(object sender, PointerPressedEventArgs e)
    {
        var owner = this.VisualRoot as Window;

        // For now, show the character creation dialog
        var vm = new CharacterViewModel();
        var view = new CharacterView { DataContext = vm };

        var window = new Window
        {
            Title = "Character Management",
            Content = view,
            Width = 800,
            Height = 600,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        if (owner is not null)
            await window.ShowDialog(owner);
        else
            window.Show();

        // When fully implemented, this should open a character list/management view:
        // var window = new CharacterManagementWindow();
        // window.Show();
    }

    // Navigation to Locations Codex
    private async void OnLocationsClick(object sender, PointerPressedEventArgs e)
    {
        // TODO: Navigate to locations management view
        ShowNotImplementedDialog("Locations Codex");

        // When implemented:
        // var window = new LocationsWindow();
        // window.Show();
    }

    // Navigation to Events Timeline
    private async void OnEventsClick(object sender, PointerPressedEventArgs e)
    {
        // TODO: Navigate to events/timeline view
        ShowNotImplementedDialog("Events Timeline");

        // When implemented:
        // var window = new EventsTimelineWindow();
        // window.Show();
    }

    // Navigation to Items Catalog
    private async void OnItemsClick(object sender, PointerPressedEventArgs e)
    {
        // TODO: Navigate to items/objects catalog view
        ShowNotImplementedDialog("Items Catalog");

        // When implemented:
        // var window = new ItemsCatalogWindow();
        // window.Show();
    }

    // Temporary method to show "not implemented" dialogs
    private async void ShowNotImplementedDialog(string featureName)
    {
        var owner = this.VisualRoot as Window;
        if (owner == null) return;

        Window dialog = new Window
        {
            Title = "Coming Soon",
            Width = 400,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new Border
            {
                Padding = new Avalonia.Thickness(30),
                Child = new StackPanel
                {
                    Spacing = 20,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = $"🚧 {featureName}",
                            FontSize = 20,
                            FontWeight = Avalonia.Media.FontWeight.SemiBold,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                        },
                        new TextBlock
                        {
                            Text = "This feature is under development and will be available soon!",
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                            Foreground = Avalonia.Media.Brushes.Gray
                        }
                    }
                }
            }
        };

        await dialog.ShowDialog(owner);
    }

    // Handle the old button click (you can remove this if you want)
    private async void ClickHandler(object sender, RoutedEventArgs e)
    {
        OnCharactersClick(sender, null);
    }
}