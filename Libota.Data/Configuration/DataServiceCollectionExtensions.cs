using System;
using System.Diagnostics;
using Domain.Users;
using Libota.Data.Notifiers;
using Libota.Data.Queries.Members;
using Libota.Data.Repositories;
using Libota.Data.Repositories.Organisations;
using Microsoft.Extensions.DependencyInjection;

namespace Libota.Data.Configuration
{
    public static class DataServiceCollectionExtensions
    {
        public static ServiceCollection RegisterDataServices(this ServiceCollection services)
        {
            services.AddDbContext<LibotaDbContext>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IOrganisationQueryHandler, OrganisationQueryHandler>();
            services.AddScoped<IMembersQueryHandler, MembersQueryHandler>();
            services.AddSingleton<IDataChangeNotifier, DataChangeNotifier>();
            services.AddSingleton<IObserver<DiagnosticListener>, DatabaseEventListener>();

            return services;
        }
    }
}