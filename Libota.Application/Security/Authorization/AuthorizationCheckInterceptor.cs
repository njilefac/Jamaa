using System.Reflection;
using System.Security;
using Castle.DynamicProxy;
using Libota.Application.Users.Services;
using Microsoft.Extensions.Logging;

namespace Libota.Application.Security.Authorization;

public class AuthorizationCheckInterceptor(ILogger<AuthorizationCheckInterceptor> logger, IUserSessionService userSessionService) : IInterceptor
{
    public void Intercept(IInvocation invocation)
    {
        if (!invocation.MethodInvocationTarget.GetCustomAttributes<AuthorizeAttribute>().Any())
            invocation.Proceed();
        else
        {
            var currentUserName = userSessionService.CurrentUserSession?.UserName;
            if (currentUserName != "admin" )
            {
                throw new SecurityException($"unauthorized operation [{currentUserName} => {invocation.TargetType}.{invocation.Method.Name}]");
            }
            logger.LogInformation($"checking authorization before calling {invocation.TargetType}.{invocation.Method.Name}");
            invocation.Proceed();
        }
    }
}