using System;
using ReactiveUI;
using Splat;

namespace Libota.Desktop.ViewModels.Members
{
    public class MembersManagementScreenViewModel : ReactiveObject, IRoutableViewModel
    {
        public string UrlPathSegment => "members.home";
        public IScreen HostScreen  => Locator.Current.GetService<IScreen>() ?? throw new InvalidOperationException();
    }
}