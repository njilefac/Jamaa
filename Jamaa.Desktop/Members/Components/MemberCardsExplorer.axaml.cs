using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Domain.Organisation.Requests;
using FluentAvalonia.UI.Controls;
using Jamaa.Desktop.Members.ViewModels;
using Jamaa.Desktop.Services.Interactions;

namespace Jamaa.Desktop.Members.Components;

public partial class MemberCardsExplorer : UserControl, IDisposable
{
    private int? _selectionAnchorIndex;

    public MemberCardsExplorer()
    {
        InitializeComponent();
    }
    
     private int GetColumnCount(int vmMembersCount)
    {
        if (MembersRepeater == null || vmMembersCount == 0)
            return 1;

        // Try to find the first few elements to see how many share the same Y coordinate
        // We look for the first realized element to establish a baseline
        Visual? firstElement = null;
        int firstIndex = -1;
        for (int i = 0; i < vmMembersCount; i++)
        {
            firstElement = MembersRepeater.TryGetElement(i);
            if (firstElement != null)
            {
                firstIndex = i;
                break;
            }
        }

        if (firstElement == null) return 1;

        double baselineY = firstElement.Bounds.Y;
        int countInFirstVisibleRow = 0;

        // Check subsequent elements on the same row
        for (int i = firstIndex; i < vmMembersCount; i++)
        {
            var element = MembersRepeater.TryGetElement(i);
            if (element == null) break;
            if (Math.Abs(element.Bounds.Y - baselineY) > 1) break;
            countInFirstVisibleRow++;
        }

        // Check preceding elements on the same row (if we didn't start at index 0)
        for (int i = firstIndex - 1; i >= 0; i--)
        {
            var element = MembersRepeater.TryGetElement(i);
            if (element == null) break;
            if (Math.Abs(element.Bounds.Y - baselineY) > 1) break;
            countInFirstVisibleRow++;
        }

        if (countInFirstVisibleRow > 0) return countInFirstVisibleRow;

        // Fallback: estimate based on width if we can't find enough elements
        if (firstElement.Bounds.Width > 0)
        {
            return (int)Math.Max(1, Math.Floor(MembersRepeater.Bounds.Width / firstElement.Bounds.Width));
        }

        return 1;
    }

    private void OnMembersListKeyDown(object? sender, KeyEventArgs e)
    {
        // Do not handle keys if a TextBox (e.g., search field) has focus
        if (e.Source is TextBox)
            return;

        if (DataContext is not MemberListViewModel vm)
            return;

        var count = vm.Members.Count;
        if (count == 0)
            return;

        var hasShift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        var hasCtrlOrCmd = e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta);

        int current = vm.Selection.SelectedIndex >= 0 ? vm.Selection.SelectedIndex : 0;
        int newIndex = current;
        bool handled = true;

        switch (e.Key)
        {
            case Key.Up:
                if (hasCtrlOrCmd && !hasShift)
                {
                    newIndex = 0;
                }
                else
                {
                    int columns = GetColumnCount(vm.Members.Count);
                    newIndex = current >= columns ? current - columns : current;
                }
                break;
            case Key.Down:
                if (hasCtrlOrCmd && !hasShift)
                {
                    newIndex = count - 1;
                }
                else
                {
                    int columns = GetColumnCount(vm.Members.Count);
                    newIndex = current + columns < count ? current + columns : current;
                }
                break;
            case Key.Left:
                newIndex = current > 0 ? current - 1 : 0;
                if (hasCtrlOrCmd && !hasShift)
                    newIndex = 0;
                break;
            case Key.Right:
                newIndex = current < count - 1 ? current + 1 : count - 1;
                if (hasCtrlOrCmd && !hasShift)
                    newIndex = count - 1;
                break;
            case Key.Home:
                newIndex = 0;
                break;
            case Key.End:
                newIndex = count - 1;
                break;
            case Key.Enter:
                if (vm.Selection.SelectedItem is MemberViewModel selectedMember)
                {
                    vm.ShowMemberProfileCommand.Execute(selectedMember);
                    handled = true;
                }
                break;
            default:
                handled = false;
                break;
        }

        if (!handled)
            return;

        if (hasShift)
        {
            // Initialize anchor if needed
            if (_selectionAnchorIndex is null || _selectionAnchorIndex < 0 || _selectionAnchorIndex >= count)
                _selectionAnchorIndex = current;

            vm.Selection.Clear();
            int start = Math.Min(_selectionAnchorIndex.Value, newIndex);
            int end = Math.Max(_selectionAnchorIndex.Value, newIndex);
            vm.Selection.SelectRange(start, end + 1); // end is exclusive
            vm.Selection.SelectedIndex = newIndex;
        }
        else
        {
            // Move selection/focus
            vm.Selection.Clear();
            vm.Selection.SelectedIndex = newIndex;
            _selectionAnchorIndex = newIndex;
        }

        FocusAndBringIntoView(vm, newIndex);
        e.Handled = true;
    }

    private void FocusAndBringIntoView(MemberListViewModel vm, int index)
    {
        if (index < 0 || index >= vm.Members.Count)
            return;

        var member = vm.Members[index];
        var card = this.GetVisualDescendants().OfType<MemberCard>()
            .FirstOrDefault(c => ReferenceEquals(c.DataContext, member));
        if (card != null)
        {
            card.BringIntoView();
            card.Focus();
        }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}