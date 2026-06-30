using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Jamaa.Application.Users.Services;
using Jamaa.Desktop.Dashboard;
using Jamaa.Desktop.Services;
using Jamaa.Desktop.Services.Navigation.Interfaces;
using Jamaa.Desktop.Services.Navigation.Services;
using Jamaa.Desktop.Services.Hosting;
using Jamaa.Desktop.Services.Notifications;
using Jamaa.Desktop.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Jamaa.Desktop.Configuration.Extensions;

public static class PresentationServicesRegistration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection RegisterPresentationServices()
        {
            services.RegisterServices()
                .RegisterViewModels();

            return services;
        }

        private IServiceCollection RegisterServices()
        {
            services.AddSingleton<IUserSessionService, UserSessionService>();
            services.AddSingleton<IRouteResolver, RouteResolver>();
            services.AddSingleton<IRouteRegistry, RouteRegistry>();
            services.AddSingleton<INavigationItemsProvider, NavigationItemsProvider>();
            services.AddSingleton<IEmbeddedWebServer, EmbeddedWebServer>();
            services.AddSingleton<AvaloniaNotificationService>();
            services.AddSingleton<INotificationService>(sp => sp.GetRequiredService<AvaloniaNotificationService>());
            services.AddSingleton<Shell>();

            // Ensure DashboardViewModel is a singleton to persist its state and handle shutdown/logout correctly
            services.AddSingleton<DashboardViewModel>();

            return services;
        }

        // ReSharper disable once UnusedMethodReturnValue.Local
        private IServiceCollection RegisterViewModels()
        {
            services.Scan(scan => scan
                .FromAssemblyOf<App>()
                .AddClasses(c => c.Where(t =>
                    t.IsAssignableTo(typeof(ObservableObject))
                    && !t.IsAssignableTo(typeof(Control))
                    && t != typeof(DashboardViewModel))) // Exclude DashboardViewModel as it's registered as singleton
                .AsSelfWithInterfaces()
                .WithTransientLifetime());

            // Ensure view models that do not inherit from ObservableObject are still resolvable via DI.
            // Nodify editor view model base may not derive from ObservableObject, so register it explicitly.
            services.AddTransient<Jamaa.Desktop.Accounting.AutomationRulesViewModel>();

            return services;
        }
    }
}
