using Microsoft.Extensions.DependencyInjection;
using Jamaa.Desktop.Configuration.Extensions;
using Jamaa.Desktop.Dashboard;
using Shouldly;
using Xunit;
using CommunityToolkit.Mvvm.ComponentModel;
using Jamaa.Application.Users.Services;
using Jamaa.Application.Setup;
using NSubstitute;
using Microsoft.Extensions.Logging;
using Jamaa.Desktop.Services.Notifications;

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
        services.AddSingleton(Substitute.For<ILogger<Jamaa.Desktop.Program>>());
        
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
