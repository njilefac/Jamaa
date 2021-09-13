using EventFlow;
using EventFlow.Configuration;
using EventFlow.EntityFramework;
using EventFlow.EntityFramework.Extensions;
using EventFlow.Extensions;
using Libota.Application.Organisation.Queries.Models;

namespace Libota.Data.Configuration
{
    public class DataEventingConfigurationModule : IModule
    {
        public void Register(IEventFlowOptions eventFlowOptions)
        {
            eventFlowOptions.AddDefaults(GetType().Assembly);
            eventFlowOptions.ConfigureEntityFramework(EntityFrameworkConfiguration.New);
            eventFlowOptions.UseEntityFrameworkEventStore<LibotaDbContext>();
            eventFlowOptions.UseEntityFrameworkReadModel<OrganisationReadModel, LibotaDbContext>();
            eventFlowOptions.UseEntityFrameworkSnapshotStore<LibotaDbContext>();
            eventFlowOptions.AddDbContextProvider<LibotaDbContext, LibotaDbContextProvider>(Lifetime.Singleton);
        }
    }
}