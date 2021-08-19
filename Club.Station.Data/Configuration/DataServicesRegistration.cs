using Autofac;
using Club.Station.Data.Repositories;
using Domain.Repositories;

namespace Club.Station.Data.Configuration
{
    public class DataServicesRegistration : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);
            builder.RegisterType<DefaultDbContext>().InstancePerLifetimeScope();
            builder.RegisterType<UsersRepository>().As<IUserRepository>().InstancePerLifetimeScope();
        }
    }
}
