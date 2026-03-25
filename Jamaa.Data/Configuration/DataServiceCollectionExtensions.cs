using System;
using System.Diagnostics;
using Domain.Users;
using Microsoft.Extensions.DependencyInjection;
using EntityFrameworkCore.Rx;
using EntityFrameworkCore.Triggers;
using Jamaa.Data.Notifiers;
using Jamaa.Data.Queries.Members;
using Jamaa.Data.Repositories.Organisations;
using Jamaa.Data.Repositories.Users;

namespace Jamaa.Data.Configuration;

public static class DataServiceCollectionExtensions
{
    public static ServiceCollection RegisterDataServices(this ServiceCollection services)
    {
        services.AddDbContext<JamaaDbContext>();
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