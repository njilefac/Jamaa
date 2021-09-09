using Autofac;
using Libota.Data.Repositories;

namespace Libota.Data.Configuration
{
    public class DataServicesRegistration : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);
            builder.RegisterType<LibotaDbContext>().InstancePerLifetimeScope();
            builder.RegisterType<UserRepository>().AsImplementedInterfaces().InstancePerLifetimeScope();
        }
    }
}
