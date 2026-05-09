namespace Jamaa.Application.Security.Authorization;

public class AuthorizeAttribute : Attribute
{
    public string? Operation { get; set; }
}