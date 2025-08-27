using Avalonia.Controls;
using Novel_T_Assistant.ViewModels;

namespace Novel_T_Assistant.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private async void ClickHandler(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var owner = this.VisualRoot as Window;

        var vm = new CharacterViewModel();             // <- your VM
        var view = new CharacterView { DataContext = vm };

        var window = new Window
        {
            Title = "Character",
            Content = view,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        if (owner is not null)
            await window.ShowDialog(owner);            // modal, centers on parent
        else
            window.Show();
    }
}
