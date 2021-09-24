using Autofac;
using Autofac.Extras.DynamicProxy;
using Libota.Application.Organisation;
using Libota.Application.Security;
using Libota.Application.Setup;
using Libota.Application.Users.Services;

namespace Libota.Application.Configuration
{
    public class ApplicationServicesRegistration : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);

            builder.RegisterType<UserManagementFacade>().AsImplementedInterfaces()
                .EnableInterfaceInterceptors()
                .InterceptedBy(typeof(AuthorizationCheckInterceptor));

            builder.RegisterType<SetupService>().AsImplementedInterfaces()
                .EnableInterfaceInterceptors()
                .InterceptedBy(typeof(AuthorizationCheckInterceptor));

            builder.RegisterType<OrganisationManagementFacade>().AsImplementedInterfaces()
                .EnableInterfaceInterceptors()
                .InterceptedBy(typeof(AuthorizationCheckInterceptor))
                .SingleInstance();

            builder.RegisterType<AuthorizationCheckInterceptor>().AsSelf().AsImplementedInterfaces();
        }
    }
}