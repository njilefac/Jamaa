using System;
using System.Reactive;
using System.Threading.Tasks;
using Domain.Shared.Values;
using Libota.Data.Models.Members;
using Libota.Desktop.ViewModels.Shared;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;
using ReactiveCommand = ReactiveUI.ReactiveCommand;

namespace Libota.Desktop.ViewModels.Members
{
    public class MemberProfileViewModel: ReactiveObject, IRoutableViewModel
    {
        public MemberProfileViewModel()
        {
            HostScreen = Locator.Current.GetService<MainWindowViewModel>() ?? throw new InvalidOperationException();;
            GoBack = ReactiveCommand.CreateFromTask(GoToPreviousPage);
        }
        
        public string? UrlPathSegment => "members.profile";
        public IScreen HostScreen { get; }
        [Reactive] public string FirstName { get; set; } = string.Empty;
        [Reactive] public string? MiddleName { get; set; }
        [Reactive] public string LastName { get; set; } = string.Empty;
        
        [Reactive] public Gender Gender { get; set; }
        
        [Reactive] public DateTime BirthDate { get; set; }
        
        [Reactive] public required RegistrationData Registration { get; set; }

        public ReactiveCommand<Unit, Unit> GoBack { get; set; }

        private async Task GoToPreviousPage()
        {
            HostScreen.Router.NavigateBack.Execute();
            await Task.FromResult(Unit.Default);
        }
    }
}