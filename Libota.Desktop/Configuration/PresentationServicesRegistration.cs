using Autofac;
using AutoMapper.Contrib.Autofac.DependencyInjection;
using Libota.Data.Mapping;
using FluentValidation;
using Libota.Application.Users.Services;
using Libota.Desktop.Validators;
using Libota.Desktop.ViewModels.Events;
using Libota.Desktop.ViewModels.Finances;
using Libota.Desktop.ViewModels.Groups;
using Libota.Desktop.ViewModels.Members;
using Libota.Desktop.ViewModels.Security;
using Libota.Desktop.ViewModels.Setup;
using Libota.Desktop.ViewModels.Shared;
using Libota.Desktop.ViewModels.Users;
using Libota.Desktop.Views.Events;
using Libota.Desktop.Views.Finances;
using Libota.Desktop.Views.Groups;
using Libota.Desktop.Views.Members;
using Libota.Desktop.Views.Security;
using Libota.Desktop.Views.Setup;
using Libota.Desktop.Views.Shared;
using Libota.Desktop.Views.Users;
using ReactiveUI;

namespace Libota.Desktop.Configuration
{
    public class PresentationServicesRegistration : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);

            RegisterMappers(builder);
            RegisterViews(builder);
            RegisterViewModels(builder);
            RegisterServices(builder);
            RegisterValidators(builder);
        }

        private void RegisterMappers(ContainerBuilder builder)
        {
            builder.RegisterAutoMapper(
                GetType().Assembly,
                typeof(EntityMappingProfile).Assembly);
        }

        private static void RegisterServices(ContainerBuilder builder)
        {
            builder.RegisterType<UserSessionService>().As<IUserSessionService>().SingleInstance();
        }

        private static void RegisterViewModels(ContainerBuilder builder)
        {
            builder.RegisterType<MainWindowViewModel>().AsSelf().As<IScreen>().SingleInstance();
            builder.RegisterType<LoginScreenViewModel>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<CreateSuperUserViewModel>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<MainMenuViewModel>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<DashboardViewModel>().AsSelf().AsImplementedInterfaces().SingleInstance();
            
            builder.RegisterType<UserManagementViewModel>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<MemberManagementViewModel>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<GroupManagementViewModel>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<EventManagementViewModel>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<FinanceManagementViewModel>().AsSelf().AsImplementedInterfaces().SingleInstance();
        }

        private static void RegisterViews(ContainerBuilder builder)
        {
            builder.RegisterType<MainWindow>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<MainMenu>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<Dashboard>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<CreateSuperUserScreen>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<LoginScreenView>().AsSelf().AsImplementedInterfaces();
            builder.RegisterType<UserManagementScreen>().AsSelf().AsImplementedInterfaces();
            builder.RegisterType<MemberManagementScreen>().AsSelf().AsImplementedInterfaces();
            builder.RegisterType<GroupManagementScreen>().AsSelf().AsImplementedInterfaces();
            builder.RegisterType<EventManagementScreen>().AsSelf().AsImplementedInterfaces();
            builder.RegisterType<FinanceManagementScreen>().AsSelf().AsImplementedInterfaces();
        }
        
        private static void RegisterValidators(ContainerBuilder builder)
        {
            builder.RegisterType<LoginScreenViewModelValidator>().As<IValidator<LoginScreenViewModel>>();
        }
    }
}