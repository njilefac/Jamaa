using System;
using System.Reactive;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Organisation.Requests;
using Domain.Shared.Values;
using JetBrains.Annotations;
using Libota.Application.Organisation;
using Libota.Desktop.Infrastructure;

namespace Libota.Desktop.ViewModels.Members;

[UsedImplicitly]
public partial class MembersOverviewPageViewModel : ObservableValidator
{
    public MembersOverviewPageViewModel(IOrganisationManagementFacade organisationManagementFacade, MemberListViewModel memberList)
    {
        _organisationManagementFacade = organisationManagementFacade;
        _memberList = memberList;
        _organisationManagementFacade.CurrentMembers.Subscribe(m =>
        {
            TotalMembersCount++;
            if (m.Gender == Gender.Male) { MaleMembersCount++; }
            else { FemaleMembersCount++; }
        });

        _organisationManagementFacade.MemberDeleted.Subscribe(m =>
        {
            TotalMembersCount--;
            if (m.Gender == Gender.Male)
            {
                MaleMembersCount--;
            }
            else
            {
                FemaleMembersCount--;
            }
        });
    }

    public Interaction<Unit, MemberRegistrationRequest> ShowRegistrationPrompt { get; } = new();

    [RelayCommand]
    private async Task RegisterMember()
    {
        var request = await ShowRegistrationPrompt.Handle(Unit.Default);
        await _organisationManagementFacade.RegisterMember(request);
    }

    private readonly IOrganisationManagementFacade _organisationManagementFacade;
    [ObservableProperty] private MemberListViewModel _memberList;
    [ObservableProperty] private int _totalMembersCount;
    [ObservableProperty] private int _maleMembersCount;
    [ObservableProperty] private int _femaleMembersCount;
}