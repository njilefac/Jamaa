using CommunityToolkit.Mvvm.ComponentModel;
using Domain.Shared.Values;
using Jamaa.Data.Models.Members;

namespace Jamaa.Desktop.Members.ViewModels;

public partial class MemberViewModel : ObservableObject
{
    [ObservableProperty] private string _id = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullName))]
    private string _lastName = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullName))]
    private string? _middleName;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullName))]
    private string _firstName = string.Empty;
    [ObservableProperty] private Gender _gender;
    [ObservableProperty] private string _organisationId = string.Empty;
    [ObservableProperty] private RegistrationViewModel? _registration;
    [ObservableProperty] private byte[]? _pictureData;

    public string FullName => string.IsNullOrWhiteSpace(MiddleName) 
        ? $"{FirstName} {LastName}" 
        : $"{FirstName} {MiddleName} {LastName}";

    public void UpdateFrom(MemberData member)
    {
        FirstName = member.FirstName;
        MiddleName = member.MiddleName;
        LastName = member.LastName;
        Gender = member.Gender;
        OrganisationId = member.OrganisationId;
        PictureData = member.PictureData;
        var registration = member.Registration;
        if (Registration == null || Registration.Id != registration.Id)
        {
            Registration = new RegistrationViewModel
            {
                Id = registration.Id,
                StartDate = registration.StartDate,
                EndDate = registration.EndDate,
                MembershipType = registration.MembershipType,
                Status = registration.Status
            };
        }
        else
        {
            Registration.UpdateFrom(registration);
        }
    }
}
