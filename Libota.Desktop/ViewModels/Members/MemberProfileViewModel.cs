using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Shared.Values;
using JetBrains.Annotations;
using Libota.Data.Models.Members;
using Libota.Desktop.Navigation;

namespace Libota.Desktop.ViewModels.Members;

[UsedImplicitly]
public partial class MemberProfileViewModel(INavigationService navigationService): ObservableObject
{
    [ObservableProperty] private string _firstName  = string.Empty;
    [ObservableProperty] private string? _middleName;
    [ObservableProperty] private string _lastName = string.Empty;
    [ObservableProperty] private Gender _gender;
    [ObservableProperty] private DateTime _birthDate;
    [ObservableProperty] private RegistrationData? _registration;

    [RelayCommand]
    private async Task GoToPreviousPage()
    {
        await navigationService.GoBack();
    }
}