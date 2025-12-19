using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Domain.Shared.Values;
using JetBrains.Annotations;
using Libota.Application.Organisation;

namespace Libota.Desktop.ViewModels.Members;

[UsedImplicitly]
public partial class MembersOverviewPageViewModel : ObservableValidator
{
    public MembersOverviewPageViewModel(IOrganisationManagementFacade organisationManagementFacade,
        MemberListViewModel memberList)
    {
        _memberList = memberList;
        organisationManagementFacade.CurrentMembers.Subscribe(m =>
        {
            TotalMembersCount++;
            if (m.Gender == Gender.Male)
            {
                MaleMembersCount++;
            }
            else
            {
                FemaleMembersCount++;
            }
        });

        organisationManagementFacade.MemberDeleted.Subscribe(m =>
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


    [ObservableProperty] private MemberListViewModel _memberList;
    [ObservableProperty] private int _totalMembersCount;
    [ObservableProperty] private int _maleMembersCount;
    [ObservableProperty] private int _femaleMembersCount;
}