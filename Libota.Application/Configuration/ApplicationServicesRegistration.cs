using System.Linq;
using Castle.DynamicProxy;
using Libota.Application.Organisation;
using Libota.Application.Security;
using Libota.Application.Setup;
using Libota.Application.Users.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Libota.Application.Configuration
{
    public static class ApplicationServicesRegistration
    {
        
        public static ServiceCollection RegisterApplicationServices(this ServiceCollection services)
        {
            services.AddProxiedScoped<IUserManagementFacade, UserManagementFacade>();

            services.AddProxiedScoped<ISetupService, SetupService>();

            services.AddProxiedSingleton<IOrganisationManagementFacade, OrganisationManagementFacade>();

            services.AddScoped<IInterceptor, AuthorizationCheckInterceptor>();

            return services;
        }
        private static void AddProxiedScoped<TInterface, TImplementation>
            (this IServiceCollection services)
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
                return proxyGenerator.CreateInterfaceProxyWithTarget(
                    typeof(TInterface), actual, interceptors);
            });
        }

        private static void AddProxiedSingleton<TInterface, TImplementation>
            (this IServiceCollection services)
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
                return proxyGenerator.CreateInterfaceProxyWithTarget(
                    typeof(TInterface), actual, interceptors);
            });
        }
    }
}