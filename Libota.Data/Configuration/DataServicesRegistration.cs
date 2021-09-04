using Autofac;
using Domain.Repositories;
using Libota.Data.Repositories;

namespace Libota.Data.Configuration
{
    public class DataServicesRegistration : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);
            builder.RegisterType<LibotaDbContext>().InstancePerLifetimeScope();
            builder.RegisterType<UsersRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<OrganisationRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
        }
    }
}
