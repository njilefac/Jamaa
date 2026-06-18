using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;

namespace Jamaa.Desktop.Shared;

public partial class MainWindow : UserControl
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnNavigationSelectionChanged(object? sender, FANavigationViewSelectionChangedEventArgs args)
    {
        if (!args.IsSettingsSelected) return;

        if (DataContext is MainWindowViewModel viewModel) viewModel.NavigateToSettings();
    }
}