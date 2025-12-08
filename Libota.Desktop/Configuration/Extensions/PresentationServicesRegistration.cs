using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using Libota.Application.Users.Services;
using Libota.Desktop.Infrastructure.Attributes;
using Libota.Desktop.Navigation;
using Microsoft.Extensions.DependencyInjection;

namespace Libota.Desktop.Configuration.Extensions;

public static class PresentationServicesRegistration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection RegisterPresentationServices()
        {
            services.RegisterServices()
                .RegisterViewModels()
                .RegisterSingleInstanceViews()
                .RegisterMultipleInstanceViews();

            return services;
        }

        private IServiceCollection RegisterServices()
        {
            services.AddSingleton<IUserSessionService, UserSessionService>();
            services.AddSingleton<NavigationStore>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddScoped<WindowNotificationManager>();

            return services;
        }

        private IServiceCollection RegisterViewModels()
        {
            services.Scan(scan => scan
                .FromAssemblyOf<App>()
                .AddClasses(c => c.Where(t =>
                    t.IsAssignableTo(typeof(ObservableObject)) && !t.IsAssignableTo(typeof(Avalonia.Controls.Control))))
                .AsSelf()
                .WithTransientLifetime());

            return services;
        }

        private IServiceCollection RegisterSingleInstanceViews()
        {
            services.Scan(scan => scan
                .FromAssemblyOf<App>()
                .AddClasses(c =>
                    c.AssignableTo<Avalonia.Controls.Control>()
                        .WithAttribute<SingleInstanceViewAttribute>())
                .AsSelfWithInterfaces()
                .WithSingletonLifetime());

            return services;
        }

        private IServiceCollection RegisterMultipleInstanceViews()
        {
            services.Scan(scan => scan
                .FromAssemblyOf<App>()
                .AddClasses(c =>
                    c.AssignableTo<Avalonia.Controls.Control>()
                        .WithoutAttribute<SingleInstanceViewAttribute>())
                .AsSelfWithInterfaces()
                .WithTransientLifetime());

            return services;
        }
    }
}