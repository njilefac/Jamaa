using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Domain.Organisation.Values;
using Domain.Shared.Values;
using JetBrains.Annotations;
using Libota.Data.Models.Members;
using Libota.Desktop.Services.Navigation.Interfaces;

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

    public Gender[] Genders => Enum.GetValues<Gender>();
    public RegistrationStatus[] RegistrationStatuses => Enum.GetValues<RegistrationStatus>();
    public MembershipType[] MembershipTypes => Enum.GetValues<MembershipType>();

    public string Title => $"Member Profile";
}