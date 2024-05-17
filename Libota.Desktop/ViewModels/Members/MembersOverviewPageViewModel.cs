using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Domain.Organisation.Requests;
using Domain.Shared.Values;
using Libota.Application.Organisation;
using Reactive.Bindings;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Helpers;
using Splat;
using ReactiveCommand = ReactiveUI.ReactiveCommand;

namespace Libota.Desktop.ViewModels.Members
{
    public class MembersOverviewPageViewModel : ReactiveValidationObject, IRoutableViewModel
    {
        private readonly IOrganisationManagementFacade _organisationManagementFacade;

        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public ReactiveCommand<Unit, Unit> RegisterMember { get; }

        [Reactive] public int TotalMembersCount { get; set; }
        [Reactive] public int MaleMembersCount { get; set; }
        [Reactive] public int FemaleMembersCount { get; set; }

        public Interaction<Unit, MemberRegistrationRequest?> ShowRegistrationPrompt { get; }


        public MembersOverviewPageViewModel()
        {
            HostScreen = Locator.Current.GetService<MembersManagementScreenViewModel>() ?? throw new InvalidOperationException();;
            _organisationManagementFacade = Locator.Current.GetService<IOrganisationManagementFacade>() ?? throw new InvalidOperationException();

            _organisationManagementFacade.MemberAdded.Subscribe(m =>
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

            RegisterMember = ReactiveCommand.CreateFromTask(OnRegisterMember);

            ShowRegistrationPrompt = new Interaction<Unit, MemberRegistrationRequest?>();
        }

        private Task OnRegisterMember()
        {
            ShowRegistrationPrompt.Handle(Unit.Default)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(request =>
                {
                    if(request != null)
                        _organisationManagementFacade.RegisterMember(request);
                });
            return Task.CompletedTask;
        }

        public string UrlPathSegment => "members.overview";
        public IScreen HostScreen { get; }
    }
}