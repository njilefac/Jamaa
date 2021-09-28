using System;

namespace Libota.Application.Security
{
    public class AuthorizeAttribute : Attribute
    {
        public string? Operation { get; set; }
    }
}