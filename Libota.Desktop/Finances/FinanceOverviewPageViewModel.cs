using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Libota.Application.Users.Services;
using Libota.Desktop.Shared;

namespace Libota.Desktop.Finances;

public class FinanceOverviewPageViewModel(IUserSessionService userSessionService) : ObservableObject, IApplicationModule
{
    public Guid Id => Guid.Parse("d1c8e5b0-9f3a-4c8b-9e2a-1f2b3c4d5e6f");
    public string Title => "Finances";
    public object? HeaderContent { get; } = null;
}