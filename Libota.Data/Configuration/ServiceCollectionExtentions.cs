using EntityFrameworkCore.Rx;
using EntityFrameworkCore.Triggers;
using Microsoft.Extensions.DependencyInjection;

namespace Libota.Data.Configuration
{
    public static class ServiceCollectionExtentions
    {
        public static IServiceCollection AddObservableDataLayer(this IServiceCollection services)
        {
            services.AddTriggers();
            services.AddDbObservables();
            return services;
        }
    }
}