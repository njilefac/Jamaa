using System;
using System.Diagnostics;
using Domain.Users;
using Libota.Data.Notifiers;
using Libota.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Libota.Data.Configuration
{
    public static class DataServiceCollectionExtensions
    {
        public static ServiceCollection RegisterDataServices(this ServiceCollection services)
        {
            services.AddDbContext<LibotaDbContext>();
            services.AddTransient<IUserRepository, UserRepository>();
            services.AddSingleton<IDataChangeNotifier, DataChangeNotifier>();
            services.AddSingleton<IObserver<DiagnosticListener>, DatabaseEventListener>();

            return services;
        }
    }
}