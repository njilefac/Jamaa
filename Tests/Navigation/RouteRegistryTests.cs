using FluentAssertions;
using Libota.Desktop.Services.Navigation.Models;
using Libota.Desktop.Services.Navigation.Services;
using Xunit;

namespace UnitTests.Navigation;

public class RouteRegistryTests
{
    [Fact]
    public void Resolve_ShouldFindNestedRoute()
    {
        // Arrange
        var registry = new RouteRegistry();
        var nestedRoute = new RouteMap("/parent/child", typeof(object));
        var parentRoute = new RouteMap("/parent", typeof(object), Nested: [nestedRoute]);
        
        registry.Register(parentRoute);

        // Act
        var resolved = registry.Resolve("/parent/child");

        // Assert
        resolved.Should().NotBeNull();
        resolved!.Path.Should().Be("/parent/child");
    }

    [Fact]
    public void Resolve_ShouldFindTopLevelRoute()
    {
        // Arrange
        var registry = new RouteRegistry();
        var route = new RouteMap("/home", typeof(object));
        registry.Register(route);

        // Act
        var resolved = registry.Resolve("/home");

        // Assert
        resolved.Should().NotBeNull();
        resolved!.Path.Should().Be("/home");
    }

    [Fact]
    public void Resolve_ShouldFindDeeplyNestedRoute()
    {
        // Arrange
        var registry = new RouteRegistry();
        var grandchild = new RouteMap("/a/b/c", typeof(object));
        var child = new RouteMap("/a/b", typeof(object), Nested: [grandchild]);
        var parent = new RouteMap("/a", typeof(object), Nested: [child]);
        
        registry.Register(parent);

        // Act
        var resolved = registry.Resolve("/a/b/c");

        // Assert
        resolved.Should().NotBeNull();
        resolved!.Path.Should().Be("/a/b/c");
    }
}
