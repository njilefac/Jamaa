using Domain.Users;
using Jamaa.Application.Setup;
using Jamaa.Application.Users.Services;
using Jamaa.Desktop;
using Jamaa.Desktop.Configuration.Extensions;
using Jamaa.Desktop.Dashboard;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace UnitTests.Configuration;

public class PresentationServicesRegistrationTests
{
    [Fact]
    public void DashboardViewModel_ShouldBeRegistered()
    {
        // Arrange
        var services = new ServiceCollection();

        // Mock dependencies for other services registered in RegisterPresentationServices
        services.AddSingleton(Substitute.For<ISetupService>());
        services.AddSingleton(Substitute.For<ILogger<Program>>());
        services.AddSingleton(Substitute.For<ILogger<UserSessionService>>());
        services.AddSingleton(Substitute.For<IUserRepository>());

        // Register presentation services
        services.RegisterPresentationServices();

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var dashboardViewModel = serviceProvider.GetService<DashboardViewModel>();

        // Assert
        dashboardViewModel.ShouldNotBeNull();
        dashboardViewModel.ShouldBeOfType<DashboardViewModel>();
    }
}