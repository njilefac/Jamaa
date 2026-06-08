using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Domain.Organisation.Requests;
using Domain.Organisation.Values;
using Domain.Shared.Values;
using Jamaa.Application.Users.Services;
using Jamaa.Desktop.Services.Interactions;
using Jamaa.Desktop.Shared;
using JetBrains.Annotations;

namespace Jamaa.Desktop.Members.Components;

[UsedImplicitly]
public partial class MemberRegistrationViewModel : ValidatableFormViewModel, IResultProvider<MemberRegistrationRequest>
{
    private readonly IUserSessionService _userSessionService;

    [ObservableProperty] private DateTime _dateOfBirth;

    [ObservableProperty] [Required] private string? _firstName;

    [ObservableProperty] [Required] private string? _lastName;

    [ObservableProperty] private MembershipType _membershipType;
    [ObservableProperty] private string? _middleName;
    [ObservableProperty] private DateTime _registrationBegin;

    [ObservableProperty] private Gender _selectedGender;

    public MemberRegistrationViewModel(IUserSessionService userSessionService)
    {
        _userSessionService = userSessionService;
        GenderOptions = Enum.GetValues<Gender>().ToList();
        SelectedGender = Gender.Unknown;
        MembershipType = MembershipType.Regular;
        RegistrationBegin = DateTime.Today;
    }

    public List<Gender> GenderOptions { get; }
    public MemberRegistrationRequest Result => GetRegistrationRequest();

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

    public void Reset()
    {
        FirstName = null;
        MiddleName = null;
        LastName = null;
        SelectedGender = Gender.Unknown;
        MembershipType = MembershipType.Regular;
        RegistrationBegin = DateTime.Today;
        ClearErrors();
    }
}