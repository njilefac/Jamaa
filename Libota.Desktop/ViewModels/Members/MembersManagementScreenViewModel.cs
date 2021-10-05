using ReactiveUI;

namespace Libota.Desktop.ViewModels.Members
{
    public class MembersManagementScreenViewModel : ReactiveObject, IScreen
    {
        public RoutingState Router { get; } = new RoutingState();

        public MembersManagementScreenViewModel()
        {
            
        }
    }
}