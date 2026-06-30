using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Users;
using Jamaa.Application.Setup;
using Jamaa.Application.Users.Services;
using Jamaa.Data.Models.Organisation;
using Jamaa.Desktop.Dashboard;
using Jamaa.Desktop.Services.Navigation.Interfaces;
using Jamaa.Desktop.Services.Navigation.Values;
using Jamaa.Desktop.Services.Updater;
using Jamaa.Desktop.Shared;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace UnitTests.Shared;

public class ShellViewModelTests
{
    [Fact]
    public void Constructor_ShouldNotResolveLoginSynchronousy()
    {
        var routeResolver = Substitute.For<IRouteResolver>();
        routeResolver.Resolve(Arg.Any<string>(), Arg.Any<object?>()).Returns(new object());

        var setupService = Substitute.For<ISetupService>();
        var organisations = new TaskCompletionSource<IEnumerable<OrganisationData>>();
        var superUser = new TaskCompletionSource<User?>();
        setupService.ListOrganisations().Returns(organisations.Task);
        setupService.GetSuperUser().Returns(superUser.Task);

        var userSessionService = Substitute.For<IUserSessionService>();
        var updateService = Substitute.For<IApplicationUpdateService>();
        var mainMenu = new MainMenuViewModel(userSessionService, updateService);
        var logger = Substitute.For<ILogger<ShellViewModel>>();

        using var viewModel = new ShellViewModel(setupService, userSessionService, mainMenu, updateService, routeResolver,
            logger);

        routeResolver.DidNotReceive().Resolve(Routes.Login, Arg.Any<object?>());
        viewModel.ActiveContent.ShouldBeNull();
    }
}
