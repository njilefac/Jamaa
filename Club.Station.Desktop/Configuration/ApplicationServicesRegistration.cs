using Autofac;
using AutoMapper.Contrib.Autofac.DependencyInjection;
using Club.Station.Data.Mapping;
using Club.Station.Desktop.ViewModels.Events;
using Club.Station.Desktop.ViewModels.Finances;
using Club.Station.Desktop.ViewModels.Groups;
using Club.Station.Desktop.ViewModels.Members;
using Club.Station.Desktop.ViewModels.Security;
using Club.Station.Desktop.ViewModels.Shared;
using Club.Station.Desktop.ViewModels.Users;
using Club.Station.Desktop.Views.Events;
using Club.Station.Desktop.Views.Finances;
using Club.Station.Desktop.Views.Groups;
using Club.Station.Desktop.Views.Members;
using Club.Station.Desktop.Views.Security;
using Club.Station.Desktop.Views.Shared;
using Club.Station.Desktop.Views.Users;
using Domain.Services;
using ReactiveUI;

namespace Club.Station.Desktop.Configuration
{
    public class ApplicationServicesRegistration : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);

            RegisterMappers(builder);
            RegisterViews(builder);
            RegisterViewModels(builder);
            RegisterServices(builder);
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
            builder.RegisterType<LoginScreenView>().AsSelf().AsImplementedInterfaces();
            builder.RegisterType<UserManagementScreen>().AsSelf().AsImplementedInterfaces();
            builder.RegisterType<MemberManagementScreen>().AsSelf().AsImplementedInterfaces();
            builder.RegisterType<GroupManagementScreen>().AsSelf().AsImplementedInterfaces();
            builder.RegisterType<EventManagementScreen>().AsSelf().AsImplementedInterfaces();
            builder.RegisterType<FinanceManagementScreen>().AsSelf().AsImplementedInterfaces();
        }
    }
}