using System;
using System.Globalization;
using Jamaa.Desktop.Services.Converters;
using Shouldly;
using Xunit;

namespace UnitTests.Services.Converters;

public class DecimalToFormattedStringConverterTests
{
    [Fact]
    public void Convert_MultiBinding_ReturnsFormattedString()
    {
        // Arrange
        var converter = new DecimalToFormattedStringConverter();
        var culture = CultureInfo.InvariantCulture;
        var values = new object[] { 123.45m, 2 };

        // Act
        var result = converter.Convert(values, typeof(string), null, culture);

        // Assert
        result.ShouldBe("123.45");
    }

    [Fact]
    public void Convert_MultiBinding_UsesPrecisionFromValues()
    {
        // Arrange
        var converter = new DecimalToFormattedStringConverter();
        var culture = CultureInfo.InvariantCulture;
        var values = new object[] { 123.45m, 3 };

        // Act
        var result = converter.Convert(values, typeof(string), null, culture);

        // Assert
        result.ShouldBe("123.450");
    }

    [Fact]
    public void Convert_MultiBinding_UsesProvidedThousandSeparator()
    {
        var converter = new DecimalToFormattedStringConverter();
        var culture = CultureInfo.InvariantCulture;
        var values = new object[] { 1234567.89m, 2, "'" };

        var result = converter.Convert(values, typeof(string), null, culture);

        result.ShouldBe("1'234'567.89");
    }
}
