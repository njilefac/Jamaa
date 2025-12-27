using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Domain.Shared.Values;
using JetBrains.Annotations;
using Libota.Application.Organisation;
using Libota.Desktop.Navigation;
using Libota.Desktop.ViewModels.Members.Messages;

namespace Libota.Desktop.ViewModels.Members;

[UsedImplicitly]
public partial class MembersOverviewViewModel : ObservableValidator, IRecipient<MemberDetailsRequested>
{
    public MembersOverviewViewModel(
        IOrganisationManagementFacade organisationManagementFacade, 
        IRouteResolver routeResolver)
    {
        ActiveContent = routeResolver.Resolve(Routes.MembersList);
        
        organisationManagementFacade.CurrentMembers.Subscribe(m =>
        {
            MembersSummary.TotalMembersCount++;
            if (m.Gender == Gender.Male)
            {
                MembersSummary.MaleMembersCount++;
            }
            else
            {
                MembersSummary.FemaleMembersCount++;
            }
        });

        organisationManagementFacade.MemberDeleted.Subscribe(m =>
        {
            MembersSummary.TotalMembersCount--;
            if (m.Gender == Gender.Male)
            {
                MembersSummary.MaleMembersCount--;
            }
            else
            {
                MembersSummary.FemaleMembersCount--;
            }
        });
    }

    [ObservableProperty] private object? _activeContent;
    [ObservableProperty] private MembersSummary _membersSummary = new();
    public void Receive(MemberDetailsRequested message)
    {
        ActiveContent = new MemberProfileViewModel
            {
                FirstName = message.Member.FirstName,
                LastName = message.Member.LastName,
                MiddleName = message.Member.MiddleName,
                Gender = message.Member.Gender,
                Registration = message.Member.Registration.Member.Registration
            };
    }
}