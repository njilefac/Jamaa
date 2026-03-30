using System;
using System.Linq;
using System.Reactive;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Huskui.Avalonia.Controls;
using Jamaa.Desktop.Members.ViewModels;
using Jamaa.Desktop.Members.Messages;

namespace Jamaa.Desktop.Members.Components;

public partial class MemberCard : UserControl
{
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<MemberCard, bool>(nameof(IsSelected));

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    private MemberListViewModel? GetViewModel() => this.FindAncestorOfType<MembersList>()?.DataContext as MemberListViewModel;

    public MemberCard()
    {
        InitializeComponent();
        this.PointerPressed += (s, e) =>
        {
            var vm = GetViewModel();
            var selectionModel = vm?.Selection;
            if (selectionModel != null && this.DataContext is MemberViewModel member)
            {
                var index = vm.Members.IndexOf(member);
                if (index != -1)
                {
                    if (e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Control))
                    {
                        if (selectionModel.IsSelected(index)) selectionModel.Deselect(index);
                        else selectionModel.Select(index);
                    }
                    else
                    {
                        selectionModel.SelectedIndex = index;
                    }
                    e.Handled = true;
                }
            }
        };

        this.KeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter)
            {
                var vm = GetViewModel();
                if (vm != null && this.DataContext is MemberViewModel member)
                {
                    if (vm.ShowMemberProfileCommand.CanExecute(member))
                    {
                        vm.ShowMemberProfileCommand.Execute(member);
                        e.Handled = true;
                    }
                }
            }
        };

        this.GetObservable(IsSelectedProperty).Subscribe(new AnonymousObserver<bool>(_ => UpdateSelectionInModel()));

        this.DataContextChanged += (s, e) =>
        {
            UpdateSelection();
        };

        this.Loaded += (s, e) =>
        {
            if (GetViewModel() is { } vm)
            {
                vm.Selection.SelectionChanged += (sender, args) => UpdateSelection();
            }
        };

        this.Unloaded += (s, e) =>
        {
        };
    }

    private bool _isUpdatingSelection;

    private void UpdateSelection()
    {
        if (_isUpdatingSelection) return;
        if (GetViewModel() is { } vm && this.DataContext is MemberViewModel member)
        {
            var index = vm.Members.IndexOf(member);
            if (index != -1)
            {
                _isUpdatingSelection = true;
                IsSelected = vm.Selection.IsSelected(index);
                _isUpdatingSelection = false;
            }
        }
    }

    private void UpdateSelectionInModel()
    {
        if (_isUpdatingSelection) return;
        if (GetViewModel() is { } vm && this.DataContext is MemberViewModel member)
        {
            var index = vm.Members.IndexOf(member);
            if (index != -1)
            {
                _isUpdatingSelection = true;
                if (IsSelected && !vm.Selection.IsSelected(index))
                {
                    vm.Selection.Select(index);
                }
                else if (!IsSelected && vm.Selection.IsSelected(index))
                {
                    vm.Selection.Deselect(index);
                }
                _isUpdatingSelection = false;
            }
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}