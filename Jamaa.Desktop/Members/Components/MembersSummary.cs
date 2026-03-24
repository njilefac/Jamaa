using CommunityToolkit.Mvvm.ComponentModel;
using Domain.Shared.Values;
using System;

using System.Reactive.Disposables;
using Jamaa.Application.Organisation;
using Jamaa.Application.Users.Services;

namespace Jamaa.Desktop.Members.Components;

public partial class MembersSummary : ObservableObject, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    [ObservableProperty]
    private uint _totalMembersCount;
    [ObservableProperty]
    private uint _maleMembersCount;
    [ObservableProperty]
    private uint _femaleMembersCount;

    public MembersSummary(IOrganisationManagementFacade organisationManagementFacade,
        IUserSessionService userSessionService)
    {
        _disposables.Add(organisationManagementFacade.CurrentMembers.Subscribe(m =>
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
        }));

        _disposables.Add(organisationManagementFacade.MemberDeleted.Subscribe(m =>
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
        }));

        _disposables.Add(userSessionService.UserSessions.Subscribe( userSession =>
        {
            if ( userSession is null or { IsAuthenticated: false })
            {
                TotalMembersCount = 0;
                MaleMembersCount = 0;
                FemaleMembersCount = 0;
            }
        }));
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}