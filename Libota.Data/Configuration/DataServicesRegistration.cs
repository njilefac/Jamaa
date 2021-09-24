using Autofac;
using Libota.Data.Notifiers;
using Libota.Data.Repositories;

namespace Libota.Data.Configuration
{
    public class DataServicesRegistration : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);
            builder.RegisterType<LibotaDbContext>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<UserRepository>().AsImplementedInterfaces();
            builder.RegisterType<DataChangeNotifier>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<DatabaseEventListener>().AsImplementedInterfaces().SingleInstance();
        }
    }
}