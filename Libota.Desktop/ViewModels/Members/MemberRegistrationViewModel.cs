using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Domain.Organisation.Requests;
using Domain.Organisation.Values;
using Domain.Shared.Values;
using JetBrains.Annotations;
using Libota.Application.Users.Services;

namespace Libota.Desktop.ViewModels.Members;

[UsedImplicitly]
public partial class MemberRegistrationViewModel : ObservableValidator
{
    private readonly IUserSessionService _userSessionService;

    public MemberRegistrationViewModel(IUserSessionService userSessionService)
    {
        _userSessionService = userSessionService;
        GenderChoices = Enum.GetValues<Gender>().ToList();
        SelectedGender = Gender.Unknown;
        MembershipType = MembershipType.Regular;
        RegistrationBegin = DateTime.Today;

        // this.ValidationRule(x => x.FirstName, v => !string.IsNullOrWhiteSpace(v) && v.Length > 2,
        //     "First name must be at least three characters");
        //
        // this.ValidationRule(x => x.LastName, v => !string.IsNullOrWhiteSpace(v) && v.Length > 2,
        //     "Last name must be at least three characters");
        //
        // this.ValidationRule(x => x.SelectedGender, v => v != Gender.Unknown,
        //     "Please select a gender");
        //
        // RegisterMember =
        //     ReactiveCommand.CreateFromTask<Unit, MemberRegistrationRequest>(GetRegistrationData, this.IsValid());
    }

    private async Task<MemberRegistrationRequest> GetRegistrationData(Unit arg)
    {
        var organisationId = _userSessionService!.CurrentUserSession!.Organisation?.Id;

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

        return await Task.FromResult(request);
    }

    public List<Gender> GenderChoices { get; }
    [ObservableProperty] private string? _firstName;
    [ObservableProperty] private string? _middleName;
    [ObservableProperty] private string? _lastName;
    [ObservableProperty] private Gender _selectedGender;

    [ObservableProperty] private DateTime _dateOfBirth;
    [ObservableProperty] private DateTime _registrationBegin;
    [ObservableProperty] private MembershipType _membershipType;
}