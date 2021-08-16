using Autofac;
using Club.Station.Desktop.ViewModels;
using Club.Station.Desktop.Views;
using Domain.Services;
using ReactiveUI;

namespace Club.Station.Desktop.Configuration
{
    public class ApplicationServicesRegistration : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);
            
            RegisterViews(builder);
            RegisterViewModels(builder);
            RegisterServices(builder);
        }

        private static void RegisterServices(ContainerBuilder builder)
        {
            builder.RegisterType<UserSessionService>().As<IUserSessionService>().SingleInstance();
        }

        private static void RegisterViewModels(ContainerBuilder builder)
        {
            builder.RegisterType<MainWindowViewModel>().AsSelf().As<IScreen>().SingleInstance();
            builder.RegisterType<LoginScreenViewModel>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<MainNavigationViewModel>().AsSelf().AsImplementedInterfaces().SingleInstance();
            
            builder.RegisterType<UserManagementViewModel>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<MemberManagementViewModel>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<GroupManagementViewModel>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<EventManagementViewModel>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<FinanceManagementViewModel>().AsSelf().AsImplementedInterfaces().SingleInstance();
        }

        private static void RegisterViews(ContainerBuilder builder)
        {
            builder.RegisterType<MainWindow>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<LoginScreenView>().AsSelf().AsImplementedInterfaces();
            builder.RegisterType<MainNavigation>().AsSelf().AsImplementedInterfaces();
            builder.RegisterType<UserManagementScreen>().AsSelf().AsImplementedInterfaces();
            builder.RegisterType<MemberManagementScreen>().AsSelf().AsImplementedInterfaces();
            builder.RegisterType<GroupManagementScreen>().AsSelf().AsImplementedInterfaces();
            builder.RegisterType<EventManagementScreen>().AsSelf().AsImplementedInterfaces();
            builder.RegisterType<FinanceManagementScreen>().AsSelf().AsImplementedInterfaces();
        }
    }
}