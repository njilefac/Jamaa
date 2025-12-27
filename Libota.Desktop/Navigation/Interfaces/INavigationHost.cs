namespace Libota.Desktop.Navigation.Interfaces;

public interface INavigationHost
{
    void NavigateTo<TViewModel>(object? parameter = null);
    void NavigateTo(string route, object? parameter = null);
    bool CanGoBack();
    void GoBack();
    bool CanGoForward();
    void GoForward();
}