using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using Domain.Organisation.Values;
using Jamaa.Desktop.Members.ViewModels;
using Jamaa.Desktop.Services.Interactions;
using Jamaa.Desktop.Shared;
using JetBrains.Annotations;

namespace Jamaa.Desktop.Members.Components;

[UsedImplicitly]
public partial class MemberEndRegistrationViewModel : ValidatableFormViewModel, IResultProvider<RegistrationStatus>
{
    [ObservableProperty] private MemberViewModel? _member;

    [ObservableProperty] [Required] private RegistrationStatus _selectedReason;

    public MemberEndRegistrationViewModel()
    {
        ReasonOptions =
        [
            RegistrationStatus.Left, RegistrationStatus.Rejected, RegistrationStatus.Suspended,
            RegistrationStatus.Expelled
        ];
        _selectedReason = RegistrationStatus.Left;
    }

    public List<RegistrationStatus> ReasonOptions { get; }

    public RegistrationStatus Result => SelectedReason;

    public void Reset(MemberViewModel member)
    {
        Member = member;
        SelectedReason = RegistrationStatus.Left;
        ClearErrors();
        ResetValidationState();
    }
}