using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Domain.Shared.Values;
using JetBrains.Annotations;
using Libota.Application.Organisation;
using Libota.Desktop.Navigation;

namespace Libota.Desktop.ViewModels.Members;

[UsedImplicitly]
public partial class MembersOverviewViewModel : ObservableValidator
{
    public MembersOverviewViewModel(
        IOrganisationManagementFacade organisationManagementFacade,
        IRouteResolver routeResolver)
    {
        ActiveContent = routeResolver.Resolve(Routes.MembersList);
        
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

    [ObservableProperty] private int _totalMembersCount;
    [ObservableProperty] private int _maleMembersCount;
    [ObservableProperty] private int _femaleMembersCount;
    [ObservableProperty] private object? _activeContent;
}