using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Domain.Shared.Values;
using JetBrains.Annotations;
using Libota.Data.Models.Members;
using Libota.Desktop.Services.Navigation.Interfaces;
using Libota.Desktop.Services.Navigation.Messages;

namespace Libota.Desktop.Members.Pages;

[UsedImplicitly]
public partial class MemberProfileViewModel: ObservableObject, IRouteableViewModel
{
    [ObservableProperty] private string _firstName  = string.Empty;
    [ObservableProperty] private string? _middleName;
    [ObservableProperty] private string _lastName = string.Empty;
    [ObservableProperty] private Gender _gender;
    [ObservableProperty] private DateTime _birthDate;
    [ObservableProperty] private RegistrationData? _registration;

    [RelayCommand]
    private Task GoToPreviousPage()
    {
        WeakReferenceMessenger.Default.Send(new NavigateBackRequested());
        return Task.CompletedTask;
    }

    public string Title => $"Details [{FirstName} {LastName}]";
}