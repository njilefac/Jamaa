using CommunityToolkit.Mvvm.ComponentModel;
using Domain.Shared.Values;
using Jamaa.Data.Models.Members;

namespace Jamaa.Desktop.Members.ViewModels;

public partial class MemberViewModel : ObservableObject
{
    [ObservableProperty] private string _id;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullName))]
    private string _lastName;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullName))]
    private string? _middleName;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullName))]
    private string _firstName;
    [ObservableProperty] private Gender _gender;
    [ObservableProperty] private string _organisationId;
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
        if (member.Registration == null)
        {
            Registration = null;
        }
        else
        {
            if (Registration == null || Registration.Id != member.Registration.Id)
            {
                Registration = new RegistrationViewModel
                {
                    Id = member.Registration.Id,
                    StartDate = member.Registration.StartDate,
                    EndDate = member.Registration.EndDate,
                    MembershipType = member.Registration.MembershipType,
                    Status = member.Registration.Status
                };
            }
            else
            {
                Registration.UpdateFrom(member.Registration);
            }
        }
    }
}
