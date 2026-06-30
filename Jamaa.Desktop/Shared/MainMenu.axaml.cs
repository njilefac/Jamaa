using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using Jamaa.Desktop.Services;

namespace Jamaa.Desktop.Shared;

public partial class MainMenu : UserControl
{
    public MainMenu()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async void ShowAboutDialog(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var dialog = new FAContentDialog
        {
            Title = "About Jamaa",
            Content = CreateAboutContent(),
            CloseButtonText = "Close",
            DefaultButton = FAContentDialogButton.Close
        };

        if (TopLevel.GetTopLevel(this) is { } owner)
        {
            await dialog.ShowAsync(owner);
            return;
        }

        await dialog.ShowAsync();
    }

    private static Control CreateAboutContent()
    {
        return new StackPanel
        {
            Spacing = 8,
            Children =
            {
                new TextBlock
                {
                    Text = "Jamaa Desktop",
                    FontSize = 18,
                    FontWeight = Avalonia.Media.FontWeight.SemiBold
                },
                new TextBlock
                {
                    Text = $"Version {VersionService.GetDisplayVersion()}"
                },
                new TextBlock
                {
                    Text = "Nubia Systems",
                    Opacity = 0.75
                },
                new TextBlock
                {
                    Text = $"Copyright (c) {DateTime.Now.Year}",
                    Opacity = 0.75
                }
            }
        };
    }
}
