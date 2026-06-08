using System;
using System.Linq;
using System.Threading.Tasks;
using Domain.Users;
using Jamaa.Application.Users;
using Jamaa.Application.Users.Services;
using Jamaa.Desktop.Dashboard;
using Jamaa.Desktop.Security.Events;
using NSubstitute;
using Xunit;

namespace UnitTests.Dashboard;

public class DashboardViewModelTests
{
    [Fact]
    public async Task Constructor_WhenUserIsAuthenticatedButNoLayoutFound_ShouldApplyDefaultGrid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User("testuser", "password", "test@example.com", "First", null, "Last", id: userId)
        {
            DashboardLayout = null
        };

        var userSessionService = Substitute.For<IUserSessionService>();
        var userSession = new UserSession(true, "testuser", userId, null);
        userSessionService.CurrentUserSession.Returns(userSession);

        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetById(userId).Returns(user);

        // Act
        var viewModel = new DashboardViewModel(userSessionService, userRepository);
        await Task.Delay(100);

        // Assert
        var nonEmptyWidgets = viewModel.ActiveWidgets.Where(w => w is not EmptyCellViewModel).ToList();
        Assert.Equal(6, nonEmptyWidgets.Count);
    }

    [Fact]
    public void Constructor_WhenUserIsNotAuthenticated_ShouldApplyDefaultGrid()
    {
        // Arrange
        var userSessionService = Substitute.For<IUserSessionService>();
        userSessionService.CurrentUserSession.Returns((UserSession?)null);

        var userRepository = Substitute.For<IUserRepository>();

        // Act
        var viewModel = new DashboardViewModel(userSessionService, userRepository);

        // Assert
        var nonEmptyWidgets = viewModel.ActiveWidgets.Where(w => w is not EmptyCellViewModel).ToList();
        Assert.Equal(6, nonEmptyWidgets.Count);
    }

    [Fact]
    public async Task LoadLayout_WhenUserNotFound_ShouldApplyDefaultGrid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userSessionService = Substitute.For<IUserSessionService>();
        var userSession = new UserSession(true, "testuser", userId, null);
        userSessionService.CurrentUserSession.Returns(userSession);

        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetById(userId).Returns((User?)null); // User not found

        var viewModel = new DashboardViewModel(userSessionService, userRepository);

        // Act
        viewModel.Receive(new UserAuthenticated(userSession));
        await Task.Delay(100);

        // Assert
        var nonEmptyWidgets = viewModel.ActiveWidgets.Where(w => w is not EmptyCellViewModel).ToList();
        Assert.Equal(6, nonEmptyWidgets.Count);
    }
}