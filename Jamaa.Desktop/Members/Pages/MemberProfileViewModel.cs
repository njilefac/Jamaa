using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Organisation.Requests;
using Domain.Organisation.Values;
using Domain.Shared.Values;
using Jamaa.Application.Organisation;
using Jamaa.Data.Models.Members;
using Jamaa.Desktop.Members.Messages;
using Jamaa.Desktop.Services.Navigation.Interfaces;
using Jamaa.Desktop.Services.Notifications;
using JetBrains.Annotations;

namespace Jamaa.Desktop.Members.Pages;

[UsedImplicitly]
public partial class MemberProfileViewModel : ObservableObject, IRouteableViewModel
{
    private readonly INotificationService _notificationService;
    private readonly IOrganisationFacade _organisationFacade;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private DateTime _birthDate;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _firstName = string.Empty;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private Gender _gender;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isOperationInFlight;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _lastName = string.Empty;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private MembershipType _membershipType;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string? _middleName;

    private MemberData? _originalMember;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteAvatarCommand))]
    private byte[]? _picture;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private RegistrationData? _registration;

    // Expose registration fields as top-level observable properties so Save CanExecute updates when they change
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private DateTime _registrationStartDate;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private RegistrationStatus _registrationStatus;

    [ObservableProperty] private int _selectedTabIndex;

    public MemberProfileViewModel(IOrganisationFacade organisationFacade,
        INotificationService notificationService)
    {
        _organisationFacade = organisationFacade;
        _notificationService = notificationService;
    }

    public Func<Task<byte[]?>>? AvatarPicker { get; set; }

    public static Gender[] GenderOptions => Enum.GetValues<Gender>();
    public static RegistrationStatus[] RegistrationStatusOptions => Enum.GetValues<RegistrationStatus>();
    public static MembershipType[] MembershipTypeOptions => Enum.GetValues<MembershipType>();

    public string Title => "Member Profile";

    public void Initialize(MemberProfileNavigationArgs args)
    {
        var member = args.Member;
        _originalMember = member;
        FirstName = member.FirstName;
        MiddleName = member.MiddleName;
        LastName = member.LastName;
        Gender = member.Gender;
        Picture = member.PictureData;

        RegistrationStartDate = member.Registration.StartDate;
        RegistrationStatus = member.Registration.Status;
        MembershipType = member.Registration.MembershipType;

        Registration = new RegistrationData
        {
            Id = member.Registration.Id,
            StartDate = member.Registration.StartDate,
            MembershipType = member.Registration.MembershipType,
            Status = member.Registration.Status,
            MemberId = member.Registration.MemberId,
            Organisation = member.Registration.Organisation,
            Member = member
        };

        if (args.TargetTab != null)
            SelectedTabIndex = args.TargetTab switch
            {
                "General" => 0,
                "Finances" => 1,
                "Attendance" => 2,
                "Groups" => 3,
                _ => 0
            };
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        // INTEGRATION: Saves member and waits for event confirmation
        if (_originalMember == null || Registration == null) return;

        var request = new MemberUpdateRequest
        {
            MemberId = _originalMember.Id,
            FirstName = FirstName,
            MiddleName = MiddleName,
            LastName = LastName,
            Gender = Gender,
            RegistrationBegin = RegistrationStartDate,
            MembershipType = MembershipType,
            Status = RegistrationStatus,
            OrganisationId = OrganisationId.With(_originalMember.OrganisationId),
            Avatar = Picture
        };

        var subject = $"{request.FirstName} {request.LastName}";
        var isConfirmed = await _notificationService.TrackOperationAsync(
            () => _organisationFacade.UpdateMember(request),
            _organisationFacade.MemberUpdated,
            m => m.Id == _originalMember.Id,
            TimeSpan.FromSeconds(10),
            "Member",
            "Saved",
            subject,
            SetOperationInFlight);

        if (isConfirmed)
        {
            // Update original to disable save button again (only after event confirmed)
            _originalMember.FirstName = FirstName;
            _originalMember.MiddleName = MiddleName;
            _originalMember.LastName = LastName;
            _originalMember.Gender = Gender;
            _originalMember.PictureData = Picture;
            _originalMember.Registration.StartDate = RegistrationStartDate;
            _originalMember.Registration.MembershipType = MembershipType;
            _originalMember.Registration.Status = RegistrationStatus;
            SaveCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanSave()
    {
        if (_originalMember == null || IsOperationInFlight) return false;

        var changed = FirstName != _originalMember.FirstName ||
                      MiddleName != _originalMember.MiddleName ||
                      LastName != _originalMember.LastName ||
                      Gender != _originalMember.Gender;

        // Compare registration-related fields
        changed |= RegistrationStartDate != _originalMember.Registration.StartDate ||
                   MembershipType != _originalMember.Registration.MembershipType ||
                   RegistrationStatus != _originalMember.Registration.Status;

        changed |= Picture != _originalMember.PictureData;

        return changed;
    }

    private void SetOperationInFlight(bool isInFlight)
    {
        IsOperationInFlight = isInFlight;
        SaveCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private async Task ChangeAvatar()
    {
        if (AvatarPicker != null)
        {
            var newAvatar = await AvatarPicker();
            if (newAvatar != null) Picture = newAvatar;
        }
    }

    [RelayCommand(CanExecute = nameof(CanDeleteAvatar))]
    private void DeleteAvatar()
    {
        Picture = null;
    }

    private bool CanDeleteAvatar()
    {
        return Picture != null;
    }
}