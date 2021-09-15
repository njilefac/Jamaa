using Autofac;
using Libota.Application.Members.Queries.Models;
using Libota.Application.Shared.Providers;
using Libota.Data.Providers;
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
            builder.RegisterType<MembersInsertedProvider>().As<IProvideObservableData<Member>>().SingleInstance();
        }
    }
}
