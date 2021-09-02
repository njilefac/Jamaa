using Autofac;

namespace Libota.Application.Configuration
{
    public class ApplicationServicesRegistration : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);
            builder.RegisterType<UserManagementFacade>().AsImplementedInterfaces();
        }
    }
}