namespace Libota.Application.Security.Authorization
{
    public class AuthorizeAttribute : Attribute
    {
        public string? Operation { get; set; }
    }
}