using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Domain.Organisation.Requests;
using Domain.Organisation.Values;
using Domain.Shared.Values;
using JetBrains.Annotations;
using Libota.Application.Users.Services;
using Libota.Desktop.Infrastructure.Interactions;

namespace Libota.Desktop.ViewModels.Members;

[UsedImplicitly]
public partial class MemberRegistrationViewModel : ObservableValidator, IResultProvider<MemberRegistrationRequest>
{
    private readonly IUserSessionService _userSessionService;

    public MemberRegistrationViewModel(IUserSessionService userSessionService)
    {
        _userSessionService = userSessionService;
        GenderChoices = Enum.GetValues<Gender>().ToList();
        SelectedGender = Gender.Unknown;
        MembershipType = MembershipType.Regular;
        RegistrationBegin = DateTime.Today;
    }

    private MemberRegistrationRequest GetRegistrationRequest()
    {
        var organisationId = _userSessionService.CurrentUserSession!.Organisation?.Id;

        var request = new MemberRegistrationRequest
        {
            FirstName = FirstName ?? throw new InvalidOperationException(),
            MiddleName = MiddleName,
            LastName = LastName ?? throw new InvalidOperationException(),
            Gender = SelectedGender,
            RegistrationBegin = RegistrationBegin,
            MembershipType = MembershipType,
            OrganisationId = OrganisationId.With(organisationId ?? throw new InvalidOperationException())
        };

        return request;
    }
    
    public List<Gender> GenderChoices { get; }
    [ObservableProperty] private string? _firstName;
    [ObservableProperty] private string? _middleName;
    [ObservableProperty] private string? _lastName;
    [ObservableProperty] private Gender _selectedGender;

    [ObservableProperty] private DateTime _dateOfBirth;
    [ObservableProperty] private DateTime _registrationBegin;
    [ObservableProperty] private MembershipType _membershipType;
    public MemberRegistrationRequest Result => GetRegistrationRequest();
}