using ReactiveUI;

namespace Club.Station.Desktop.ViewModels.Shared
{
    public class MainWindowViewModel : ReactiveObject, IScreen
    {
        public RoutingState Router { get; }

        public MainWindowViewModel()
        {
            Router = new RoutingState();
        }
    }
}