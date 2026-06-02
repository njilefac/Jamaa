using Microsoft.Extensions.Logging;

namespace Jamaa.Application.Security.Authorization;

public partial class AuthorizationCheckInterceptor
{
    [LoggerMessage(LogLevel.Information, "checking authorization before calling {InvocationTargetType}.{MethodName}")]
    partial void LogCheckingAuthorization(Type? invocationTargetType, string methodName);
}