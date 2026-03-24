using System.Linq;
using System.Reactive;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Huskui.Avalonia.Controls;

namespace Jamaa.Desktop.Members.Components;

public partial class MemberCard : Card
{
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<MemberCard, bool>(nameof(IsSelected));

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public MemberCard()
    {
        InitializeComponent();
        this.PointerPressed += (s, e) =>
        {
            var vm = (this.Parent as ItemsControl)?.DataContext as MemberListViewModel;
            var selectionModel = vm?.Selection;
            if (selectionModel != null && this.DataContext is Jamaa.Data.Models.Members.MemberData member)
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

        this.GetObservable(IsSelectedProperty).Subscribe(new AnonymousObserver<bool>(_ => UpdateSelectionInModel()));

        this.DataContextChanged += (s, e) =>
        {
            UpdateSelection();
        };

        this.Loaded += (s, e) =>
        {
            if ((this.Parent as ItemsControl)?.DataContext is MemberListViewModel vm)
            {
                vm.Selection.SelectionChanged += (sender, args) => UpdateSelection();
            }
        };
    }

    private bool _isUpdatingSelection;

    private void UpdateSelection()
    {
        if (_isUpdatingSelection) return;
        if ((this.Parent as ItemsControl)?.DataContext is MemberListViewModel vm && this.DataContext is Jamaa.Data.Models.Members.MemberData member)
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
        if ((this.Parent as ItemsControl)?.DataContext is MemberListViewModel vm && this.DataContext is Jamaa.Data.Models.Members.MemberData member)
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