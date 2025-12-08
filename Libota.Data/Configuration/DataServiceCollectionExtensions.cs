using System;
using System.Diagnostics;
using Domain.Users;
using Libota.Data.Notifiers;
using Libota.Data.Queries.Members;
using Libota.Data.Repositories;
using Libota.Data.Repositories.Organisations;
using Microsoft.Extensions.DependencyInjection;
using EntityFrameworkCore.Rx;
using EntityFrameworkCore.Triggers;
using Libota.Data.Repositories.Users;

namespace Libota.Data.Configuration;

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
            
        services.AddTriggers();
        services.AddDbObservables();

        return services;
    }
}