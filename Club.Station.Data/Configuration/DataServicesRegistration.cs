using Autofac;

namespace Club.Station.Data.Configuration
{
    public class DataServicesRegistration : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);
            builder.RegisterType<DefaultDbContext>().InstancePerLifetimeScope();
        }
    }
}
