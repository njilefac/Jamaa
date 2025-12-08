using System.Linq;
using Castle.DynamicProxy;
using Libota.Application.Organisation;
using Libota.Application.Security;
using Libota.Application.Setup;
using Libota.Application.Shared;
using Libota.Application.Users.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Libota.Application.Configuration;

public static class ApplicationServicesRegistration
{
        
    public static ServiceCollection RegisterApplicationServices(this ServiceCollection services)
    {
        services.AddSingleton(new ProxyGenerator());
            
        services.AddProxiedScoped<IUserManagementFacade, UserManagementFacade>();

        services.AddProxiedScoped<ISetupService, SetupService>();

        services.AddProxiedSingleton<IOrganisationManagementFacade, OrganisationManagementFacade>();

        services.AddSingleton<IQueryProcessor, QueryProcessor>();

        services.AddScoped<IInterceptor, AuthorizationCheckInterceptor>();

        return services;
    }
    extension(IServiceCollection services)
    {
        private void AddProxiedScoped<TInterface, TImplementation>
            ()
            where TInterface : class
            where TImplementation : class, TInterface
        {
            services.AddScoped<TImplementation>();
            services.AddScoped(typeof(TInterface), serviceProvider =>
            {
                var proxyGenerator = serviceProvider
                    .GetRequiredService<ProxyGenerator>();
                var actual = serviceProvider
                    .GetRequiredService<TImplementation>();
                var interceptors = serviceProvider
                    .GetServices<IInterceptor>().ToArray();
                return proxyGenerator.CreateInterfaceProxyWithTarget<TInterface>(actual, interceptors);
            });
        }

        private void AddProxiedSingleton<TInterface, TImplementation>
            ()
            where TInterface : class
            where TImplementation : class, TInterface
        {
            services.AddSingleton<TImplementation>();
            services.AddSingleton(typeof(TInterface), serviceProvider =>
            {
                var proxyGenerator = serviceProvider
                    .GetRequiredService<ProxyGenerator>();
                var actual = serviceProvider
                    .GetRequiredService<TImplementation>();
                var interceptors = serviceProvider
                    .GetServices<IInterceptor>().ToArray();
                return proxyGenerator.CreateInterfaceProxyWithTarget<TInterface>(actual, interceptors);
            });
        }
    }
}