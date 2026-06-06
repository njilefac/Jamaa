namespace Jamaa.Application.Security.Authorization;

[AttributeUsage(AttributeTargets.Method)]
public class AuthorizeAttribute : Attribute
{
    public string? Operation { get; set; }
}