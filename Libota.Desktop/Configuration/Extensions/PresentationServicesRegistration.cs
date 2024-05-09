using Avalonia.Controls.Notifications;
using FluentValidation;
using Libota.Application.Organisation;
using Libota.Application.Setup;
using Libota.Application.Users.Services;
using Libota.Data.Mapping;
using Libota.Desktop.Validators;
using Libota.Desktop.ViewModels.Events;
using Libota.Desktop.ViewModels.Finances;
using Libota.Desktop.ViewModels.Groups;
using Libota.Desktop.ViewModels.Members;
using Libota.Desktop.ViewModels.Security;
using Libota.Desktop.ViewModels.Setup;
using Libota.Desktop.ViewModels.Shared;
using Libota.Desktop.ViewModels.Users;
using Libota.Desktop.Views.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace Libota.Desktop.Configuration.Extensions
{
    public static class PresentationServicesRegistration
    {
        private const string MEMBERS_MANAGEMENT_SCREEN = "MembersManagementScreen";
        private const string MAIN_WINDOW = "MainWindow";

        public static IServiceCollection RegisterPresentationServices(this IServiceCollection services)
        {
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

            return services;
        }

        private static IServiceCollection RegisterViewModels(this IServiceCollection services)
        {
            services.AddKeyedSingleton<MainWindowViewModel>(MAIN_WINDOW);
            services.AddKeyedSingleton<MembersManagementScreenViewModel>(MEMBERS_MANAGEMENT_SCREEN);

            services.AddSingleton<MainMenuViewModel>(r =>
                new MainMenuViewModel(r.GetRequiredService<IUserSessionService>(),
                    r.GetRequiredKeyedService<MainWindowViewModel>(MAIN_WINDOW),
                    r.GetRequiredService<LoginScreenViewModel>()));

            services.AddSingleton<MemberRegistrationDialogViewModel>();

            services.AddSingleton<LoginScreenViewModel>(r => new LoginScreenViewModel(
                r.GetRequiredKeyedService<MainWindowViewModel>(MAIN_WINDOW),
                r.GetRequiredService<IUserSessionService>(),
                r.GetRequiredService<ISetupService>(),
                r.GetRequiredService<ILogger<LoginScreenViewModel>>()));

            services.AddSingleton<CreateSuperUserViewModel>(r =>
                new CreateSuperUserViewModel(r.GetRequiredKeyedService<MainWindowViewModel>(MAIN_WINDOW),
                    r.GetRequiredService<ISetupService>(),
                    r.GetRequiredService<IUserSessionService>()));

            services.AddSingleton<CreateOrganisationViewModel>(r =>
                new CreateOrganisationViewModel(r.GetRequiredService<ISetupService>(), r.GetRequiredKeyedService<MainWindowViewModel>(MAIN_WINDOW)));

            services.AddSingleton<OrganisationContactDetailsViewModel>(r =>
                new OrganisationContactDetailsViewModel(r.GetRequiredKeyedService<MainWindowViewModel>(MAIN_WINDOW)));

            services.AddSingleton<DashboardViewModel>(r =>
                new DashboardViewModel(r.GetRequiredService<IUserSessionService>(), r.GetRequiredKeyedService<MainWindowViewModel>(MAIN_WINDOW)));

            services.AddSingleton<UserManagementViewModel>(r =>
                new UserManagementViewModel(r.GetRequiredKeyedService<MainWindowViewModel>(MAIN_WINDOW)));

            services.AddSingleton<GroupManagementViewModel>(r =>
                new GroupManagementViewModel(r.GetRequiredService<IUserSessionService>(), r.GetRequiredKeyedService<MainWindowViewModel>(MAIN_WINDOW)));

            services.AddSingleton<EventManagementViewModel>(r =>
                new EventManagementViewModel(r.GetRequiredKeyedService<MainWindowViewModel>(MAIN_WINDOW), r.GetRequiredService<IUserSessionService>()));

            services.AddSingleton<FinanceManagementViewModel>(r =>
                new FinanceManagementViewModel(r.GetRequiredService<IUserSessionService>(), r.GetRequiredKeyedService<MainWindowViewModel>(MAIN_WINDOW)));

            services.AddSingleton<MembersOverviewPageViewModel>(r =>
                new MembersOverviewPageViewModel(r.GetRequiredService<IOrganisationManagementFacade>(), r.GetRequiredKeyedService<MembersManagementScreenViewModel>(MEMBERS_MANAGEMENT_SCREEN)));

            services.AddSingleton<MemberProfileViewModel>(r => new MemberProfileViewModel(r.GetRequiredKeyedService<MembersManagementScreenViewModel>(MEMBERS_MANAGEMENT_SCREEN)));

            services.AddSingleton<MembersListViewModel>(r =>
                new MembersListViewModel(r.GetRequiredService<IOrganisationManagementFacade>(), r.GetRequiredKeyedService<MembersManagementScreenViewModel>(MEMBERS_MANAGEMENT_SCREEN)));

            return services;
        }

        private static IServiceCollection RegisterViews(this IServiceCollection services)
        {
            services.Scan(scan => scan.FromAssemblyOf<MainWindow>()
                .AddClasses(x => x.AssignableTo(typeof(IViewFor<>)))
                .AsSelfWithInterfaces()
                .WithScopedLifetime());

            return services;
        }

        private static IServiceCollection RegisterValidators(this IServiceCollection services)
        {
            services.AddScoped<IValidator<LoginScreenViewModel>, LoginScreenViewModelValidator>();
            return services;
        }
    }
}