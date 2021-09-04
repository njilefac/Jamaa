using EventFlow;
using EventFlow.Configuration;
using EventFlow.Configuration.Cancellation;
using EventFlow.Extensions;
using EventFlow.Hangfire.Extensions;

namespace Libota.Application.Configuration
{
    public class ApplicationEventingConfigurationModule : IModule
    {
        public void Register(IEventFlowOptions eventFlowOptions)
        {
            var fromAssembly = GetType().Assembly;
            
            eventFlowOptions
                .UseHangfireJobScheduler()
                .Configure(c =>
                {
                    c.IsAsynchronousSubscribersEnabled = true;
                    c.ThrowSubscriberExceptions = true;
                    c.CancellationBoundary = CancellationBoundary.BeforeNotifyingSubscribers;
                })
                .RegisterServices(sr =>
                {
                    sr.Register(ctx =>
                    {
                        var log = ctx.Resolver.Resolve<LibotaEventLog>();
                        return log;
                    });
                })
                .AddDefaults(fromAssembly);
        }
    }
}