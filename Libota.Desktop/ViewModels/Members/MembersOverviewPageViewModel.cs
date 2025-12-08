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
    public MembersOverviewPageViewModel(MembersManagementScreenViewModel hostScreen,
        IOrganisationManagementFacade organisationManagementFacade)
    {
        _organisationManagementFacade = organisationManagementFacade;


        ShowRegistrationPrompt = new Interaction<Unit, MemberRegistrationRequest?>();

        _organisationManagementFacade.CurrentMembers.Subscribe(m =>
        {
            TotalMembersCount++;
            if (m.Gender == Gender.Male)
                MaleMembersCount++;
            else FemaleMembersCount++;
        });

        _organisationManagementFacade.MemberDeleted.Subscribe(m =>
        {
            TotalMembersCount--;
            if (m.Gender == Gender.Male)
                MaleMembersCount--;
            else FemaleMembersCount--;
        });
    }

    public string UrlPathSegment => "members.overview";

    public Interaction<Unit, MemberRegistrationRequest> ShowRegistrationPrompt { get; }

    [RelayCommand]
    private async Task RegisterMember()
    {
        var request = await ShowRegistrationPrompt.Handle(Unit.Default);
        await _organisationManagementFacade.RegisterMember(request);
    }

    private readonly IOrganisationManagementFacade _organisationManagementFacade;
    [ObservableProperty] private int _totalMembersCount;
    [ObservableProperty] private int _maleMembersCount;
    [ObservableProperty] private int _femaleMembersCount;
}