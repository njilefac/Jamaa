using System;

namespace Jamaa.Desktop.Shared;

public interface IApplicationModule
{
    Guid Id { get; }
    string Title { get; }
    object? HeaderContent { get; }
}