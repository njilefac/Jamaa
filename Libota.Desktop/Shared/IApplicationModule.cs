using System;

namespace Libota.Desktop.Shared;

public interface IApplicationModule
{
    Guid Id { get; }
    string Title { get; }
    object? HeaderContent { get; }
}