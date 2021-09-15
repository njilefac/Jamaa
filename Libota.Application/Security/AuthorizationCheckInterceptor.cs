using System.Linq;
using System.Reflection;
using System.Security;
using Castle.DynamicProxy;
using Libota.Application.Users.Services;
using Microsoft.Extensions.Logging;

namespace Libota.Application.Security
{
    public class AuthorizationCheckInterceptor : IInterceptor
    {
        private readonly ILogger<AuthorizationCheckInterceptor> _logger;
        private readonly IUserSessionService _userSessionService;
        public AuthorizationCheckInterceptor(ILogger<AuthorizationCheckInterceptor> logger, IUserSessionService userSessionService)
        {
            _logger = logger;
            _userSessionService = userSessionService;
        }
        public void Intercept(IInvocation invocation)
        {
            var authorizationAttributes =
                invocation.MethodInvocationTarget.GetCustomAttributes().Where(x => x is AuthorizeAttribute);
            if (!authorizationAttributes.Any())
                invocation.Proceed();
            else
            {
                var currentUserName = _userSessionService?.CurrentUserSession?.UserName;
                if (currentUserName != "admin" )
                {
                    throw new SecurityException($"unauthorized operation [{currentUserName} => {invocation.TargetType}.{invocation.Method.Name}]");
                }
                _logger.LogInformation($"checking authorization before calling {invocation.TargetType}.{invocation.Method.Name}");
                invocation.Proceed();
            }
        }
    }
}