using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Domain.Organisation.Values;
using Jamaa.Data.Models.Members;

namespace Jamaa.Desktop.Members.ViewModels;

public partial class RegistrationViewModel : ObservableObject
{
    [ObservableProperty] private string _id;
    [ObservableProperty] private DateTime _startDate;
    [ObservableProperty] private DateTime? _endDate;
    [ObservableProperty] private MembershipType _membershipType;
    [ObservableProperty] private RegistrationStatus _status;

    public bool IsActive => EndDate == null;

    public void UpdateFrom(RegistrationData registration)
    {
        StartDate = registration.StartDate;
        EndDate = registration.EndDate;
        MembershipType = registration.MembershipType;
        Status = registration.Status;
    }
}
