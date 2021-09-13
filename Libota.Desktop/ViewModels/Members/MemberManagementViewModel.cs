using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Libota.Application.Organisation;
using Libota.Application.Organisation.Requests;
using ReactiveUI;
using ReactiveUI.Validation.Helpers;

namespace Libota.Desktop.ViewModels.Members
{
    public class MemberManagementViewModel : ReactiveValidationObject
    {
        private readonly IOrganisationManagementFacade _organisationManagementFacade;
        public ReactiveCommand<object?, Unit> RegisterMember { get; }

        public Interaction<Window, MemberRegistrationRequest> ShowRegistrationPrompt { get; }

        public MemberManagementViewModel(IOrganisationManagementFacade organisationManagementFacade)
        {
            _organisationManagementFacade = organisationManagementFacade;
            RegisterMember = ReactiveCommand.CreateFromTask<object?>(OnRegisterMember);
            ShowRegistrationPrompt = new Interaction<Window, MemberRegistrationRequest>();
            
        }

        private Task OnRegisterMember(object? sender)
        {
            ShowRegistrationPrompt.Handle(sender as Window)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(request =>
                {
                    if (request == null)
                    {
                        return;
                    }
                    _organisationManagementFacade.RegisterMember(request);
                });
            return Task.CompletedTask;
        }
    }
}