using Avalonia.Controls.Notifications;
using FluentValidation;
using Libota.Application.Users.Services;
using Libota.Data.Mapping;
using Libota.Desktop.Services;
using Libota.Desktop.Validators;
using Libota.Desktop.ViewModels.Events;
using Libota.Desktop.ViewModels.Finances;
using Libota.Desktop.ViewModels.Groups;
using Libota.Desktop.ViewModels.Members;
using Libota.Desktop.ViewModels.Security;
using Libota.Desktop.ViewModels.Setup;
using Libota.Desktop.ViewModels.Shared;
using Libota.Desktop.ViewModels.Users;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Splat;
using Splat.Microsoft.Extensions.DependencyInjection;

namespace Libota.Desktop.Configuration.Extensions;

public static class PresentationServicesRegistration
{
    public static IServiceCollection RegisterPresentationServices(this IServiceCollection services)
    {
        services.UseMicrosoftDependencyResolver();
        var resolver = Locator.CurrentMutable;
        resolver.InitializeSplat();
        resolver.InitializeReactiveUI();

        services.RegisterMappers()
            .RegisterServices()
            .RegisterValidators()
            .RegisterViewModels()
            .RegisterViews();


        return services;
    }

    private static IServiceCollection RegisterMappers(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(EntityMappingProfile).Assembly);
        return services;
    }

    private static IServiceCollection RegisterServices(this IServiceCollection services)
    {
        services.AddSingleton<IUserSessionService, UserSessionService>();
        services.AddScoped<WindowNotificationManager>();
        services.AddScoped<IViewLocator, LibotaViewLocator>();

        return services;
    }

    private static IServiceCollection RegisterViewModels(this IServiceCollection services)
    {
        services.AddSingleton<MainWindowViewModel>();

        services.AddSingleton<MembersManagementScreenViewModel>();

            
        services.AddTransient<MainMenuViewModel>();
        services.AddTransient<MemberRegistrationDialogViewModel>();
        services.AddTransient<LoginScreenViewModel>();
        services.AddTransient<CreateSuperUserViewModel>();
        services.AddTransient<CreateOrganisationViewModel>();
        services.AddTransient<OrganisationContactDetailsViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<UserManagementViewModel>();
        services.AddTransient<GroupManagementViewModel>();
        services.AddTransient<EventManagementViewModel>();
        services.AddTransient<FinanceManagementViewModel>();
            
        services.AddTransient<MembersOverviewPageViewModel>();
        services.AddTransient<MemberProfileViewModel>();
        services.AddTransient<MembersListViewModel>();

        return services;
    }

    private static IServiceCollection RegisterViews(this IServiceCollection services)
    {
        Locator.CurrentMutable.RegisterViewsForViewModels(typeof(MainWindowViewModel).Assembly);
        return services;
    }

    private static IServiceCollection RegisterValidators(this IServiceCollection services)
    {
        services.AddScoped<IValidator<LoginScreenViewModel>, LoginScreenViewModelValidator>();
        return services;
    }
}