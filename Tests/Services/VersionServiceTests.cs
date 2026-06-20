using System;
using Jamaa.Desktop.Services;
using Shouldly;
using Xunit;

namespace UnitTests.Services;

public class VersionServiceTests
{
    [Theory]
    [InlineData("1.2.3+abc123", "1.2.3")]
    [InlineData("1.2.3-beta.1", "1.2.3")]
    [InlineData("v1.2.3", "1.2.3")]
    [InlineData("V1.2.3", "1.2.3")]
    public void GetComparableVersion_ShouldNormalizeSemVerDecorations(string input, string expected)
    {
        var comparable = VersionService.GetComparableVersion(input);

        comparable.ShouldBe(Version.Parse(expected));
    }

    [Fact]
    public void GetComparableVersion_ShouldFallbackToZeroVersion_WhenInvalid()
    {
        var comparable = VersionService.GetComparableVersion("not-a-version");

        comparable.ShouldBe(new Version(0, 0, 0));
    }

    [Theory]
    [InlineData("1.2.3+abc123", "1.2.3")]
    [InlineData("1.2.3-beta.1", "1.2.3-beta.1")]
    [InlineData("v1.2.3-beta+sha", "1.2.3-beta")]
    public void GetDisplayVersion_ShouldKeepPrereleaseSuffix_AndDropBuildMetadata(string input, string expected)
    {
        var normalized = VersionService.GetDisplayVersion(input);

        normalized.ShouldBe(expected);
    }
}
