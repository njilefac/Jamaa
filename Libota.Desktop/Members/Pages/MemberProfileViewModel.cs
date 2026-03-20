using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Organisation.Requests;
using Domain.Organisation.Values;
using Domain.Shared.Values;
using JetBrains.Annotations;
using Libota.Application.Organisation;
using Libota.Data.Models.Members;
using Libota.Desktop.Services.Navigation.Interfaces;

namespace Libota.Desktop.Members.Pages;

[UsedImplicitly]
public partial class MemberProfileViewModel: ObservableObject, IRouteableViewModel
{
    private readonly IOrganisationManagementFacade _organisationManagementFacade;
    private MemberData? _originalMember;

    public MemberProfileViewModel(IOrganisationManagementFacade organisationManagementFacade)
    {
        _organisationManagementFacade = organisationManagementFacade;
    }

    public void Initialize(MemberData member)
    {
        _originalMember = member;
        FirstName = member.FirstName;
        MiddleName = member.MiddleName;
        LastName = member.LastName;
        Gender = member.Gender;
        
        if (member.Registration != null)
        {
            Registration = new RegistrationData
            {
                Id = member.Registration.Id,
                StartDate = member.Registration.StartDate,
                MembershipType = member.Registration.MembershipType,
                Status = member.Registration.Status,
                MemberId = member.Registration.MemberId,
                Organisation = member.Registration.Organisation
            };
        }
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _firstName  = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string? _middleName;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _lastName = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private Gender _gender;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private DateTime _birthDate;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private RegistrationData? _registration;

    public Gender[] Genders => Enum.GetValues<Gender>();
    public RegistrationStatus[] RegistrationStatuses => Enum.GetValues<RegistrationStatus>();
    public MembershipType[] MembershipTypes => Enum.GetValues<MembershipType>();
    
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async void Save()
    {
        if (_originalMember == null || Registration == null) return;

        var request = new MemberUpdateRequest
        {
            MemberId = _originalMember.Id,
            FirstName = FirstName,
            MiddleName = MiddleName,
            LastName = LastName,
            Gender = Gender,
            RegistrationBegin = Registration.StartDate,
            MembershipType = Registration.MembershipType,
            OrganisationId = OrganisationId.With(_originalMember.OrganisationId)
        };

        await _organisationManagementFacade.UpdateMember(request);
        
        // Update original to disable save button again
        _originalMember.FirstName = FirstName;
        _originalMember.MiddleName = MiddleName;
        _originalMember.LastName = LastName;
        _originalMember.Gender = Gender;
        if (_originalMember.Registration != null)
        {
            _originalMember.Registration.StartDate = Registration.StartDate;
            _originalMember.Registration.MembershipType = Registration.MembershipType;
        }
        SaveCommand.NotifyCanExecuteChanged();
    }

    private bool CanSave()
    {
        if (_originalMember == null) return false;

        var changed = FirstName != _originalMember.FirstName ||
                      MiddleName != _originalMember.MiddleName ||
                      LastName != _originalMember.LastName ||
                      Gender != _originalMember.Gender;

        if (Registration != null && _originalMember.Registration != null)
        {
            changed |= Registration.StartDate != _originalMember.Registration.StartDate ||
                       Registration.MembershipType != _originalMember.Registration.MembershipType;
        }

        return changed;
    }

    public string Title => $"Member Profile";
}