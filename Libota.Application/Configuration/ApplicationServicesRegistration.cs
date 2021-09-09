using Autofac;
using EventFlow.Logs;
using Libota.Application.Setup;
using Libota.Application.Users;

namespace Libota.Application.Configuration
{
    public class ApplicationServicesRegistration : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);
            builder.RegisterType<UserManagementFacade>().AsImplementedInterfaces();
            builder.RegisterType<SetupService>().AsImplementedInterfaces();
        }
    }
}