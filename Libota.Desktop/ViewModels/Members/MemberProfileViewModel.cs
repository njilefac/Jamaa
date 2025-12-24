using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Domain.Shared.Values;
using JetBrains.Annotations;
using Libota.Data.Models.Members;
using Libota.Desktop.ViewModels.Members.Messages;

namespace Libota.Desktop.ViewModels.Members;

[UsedImplicitly]
public partial class MemberProfileViewModel: ObservableObject, IRecipient<MemberProfileSelected>, IDisposable
{
    [ObservableProperty] private string _firstName  = string.Empty;
    [ObservableProperty] private string? _middleName;
    [ObservableProperty] private string _lastName = string.Empty;
    [ObservableProperty] private Gender _gender;
    [ObservableProperty] private DateTime _birthDate;
    [ObservableProperty] private RegistrationData? _registration;
    
    public MemberProfileViewModel()
    {
        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    [RelayCommand]
    private Task GoToPreviousPage()
    {
        //TODO: await Navigation?.GoBack()!;
        return Task.CompletedTask;
    }

    public void Receive(MemberProfileSelected message)
    {
        FirstName = message.Member.FirstName;
        MiddleName = message.Member.MiddleName;
        LastName = message.Member.LastName;
        Gender = message.Member.Gender;
        //TODO: BirthDate = message.Member.BirthDate;
        Registration = message.Member.Registration;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }
}