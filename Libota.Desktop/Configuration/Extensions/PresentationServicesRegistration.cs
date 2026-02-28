using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using Libota.Application.Users.Services;
using Libota.Desktop.Navigation;
using Libota.Desktop.Navigation.Interfaces;
using Libota.Desktop.Navigation.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Libota.Desktop.Configuration.Extensions;

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
            services.AddScoped<WindowNotificationManager>();

            return services;
        }

        // ReSharper disable once UnusedMethodReturnValue.Local
        private IServiceCollection RegisterViewModels()
        {
            services.Scan(scan => scan
                .FromAssemblyOf<App>()
                .AddClasses(c => c.Where(t =>
                    t.IsAssignableTo(typeof(ObservableObject))
                    && !t.IsAssignableTo(typeof(Avalonia.Controls.Control))))
                .AsSelfWithInterfaces()
                .WithTransientLifetime());

            return services;
        }
    }
}