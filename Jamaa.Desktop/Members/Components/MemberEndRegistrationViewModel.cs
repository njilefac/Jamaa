using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Domain.Organisation.Values;
using Jamaa.Desktop.Services.Interactions;
using Jamaa.Desktop.Shared;
using JetBrains.Annotations;
using Jamaa.Desktop.Members.ViewModels;

namespace Jamaa.Desktop.Members.Components;

[UsedImplicitly]
public partial class MemberEndRegistrationViewModel : ValidatableFormViewModel, IResultProvider<RegistrationStatus>
{
    public MemberEndRegistrationViewModel()
    {
        ReasonOptions = [RegistrationStatus.Left, RegistrationStatus.Rejected, RegistrationStatus.Suspended, RegistrationStatus.Expelled];
        _selectedReason = RegistrationStatus.Left;
    }

    [ObservableProperty]
    private MemberViewModel? _member;

    public List<RegistrationStatus> ReasonOptions { get; }

    [ObservableProperty]
    [Required]
    private RegistrationStatus _selectedReason;

    public RegistrationStatus Result => SelectedReason;

    public void Reset(MemberViewModel member)
    {
        Member = member;
        SelectedReason = RegistrationStatus.Left;
        ClearErrors();
        ResetValidationState();
    }
}
