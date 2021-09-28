using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Domain.Values;
using Libota.Application.Organisation;
using Libota.Application.Organisation.Requests;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Helpers;

namespace Libota.Desktop.ViewModels.Members
{
    public class MembersOverviewPageViewModel : ReactiveValidationObject
    {
        private readonly IOrganisationManagementFacade _organisationManagementFacade;

        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public ReactiveCommand<object?, Unit> RegisterMember { get; }

        [Reactive] public int TotalMembersCount { get; set; }
        [Reactive] public int MaleMembersCount { get; set; }
        [Reactive] public int FemaleMembersCount { get; set; }

        public Interaction<Window?, MemberRegistrationRequest?> ShowRegistrationPrompt { get; }


        public MembersOverviewPageViewModel(IOrganisationManagementFacade organisationManagementFacade)
        {
            _organisationManagementFacade = organisationManagementFacade;

            organisationManagementFacade.MemberAdded.Subscribe(m =>
            {
                TotalMembersCount++;
                if (m.Gender == Gender.Male)
                    MaleMembersCount++;
                else FemaleMembersCount++;
            });

            organisationManagementFacade.MemberDeleted.Subscribe(m =>
            {
                TotalMembersCount--;
                if (m.Gender == Gender.Male)
                    MaleMembersCount--;
                else FemaleMembersCount--;
            });

            RegisterMember = ReactiveCommand.CreateFromTask<object?>(OnRegisterMember);

            ShowRegistrationPrompt = new Interaction<Window?, MemberRegistrationRequest?>();
        }

        private Task OnRegisterMember(object? sender)
        {
            ShowRegistrationPrompt.Handle(sender as Window)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(request =>
                {
                    if(request != null)
                        _organisationManagementFacade.RegisterMember(request);
                });
            return Task.CompletedTask;
        }
    }
}