using System;
using System.Globalization;
using Jamaa.Desktop.Services.Converters;
using Shouldly;
using Xunit;
using Avalonia.Data;

namespace UnitTests.Services.Converters;

public class DecimalToFormattedStringConverterTests
{
    [Fact]
    public void ConvertBack_MultiBinding_ReturnsCorrectValues()
    {
        // Arrange
        var converter = new DecimalToFormattedStringConverter();
        var culture = CultureInfo.InvariantCulture;
        var targetTypes = new[] { typeof(decimal), typeof(int) };

        // Act
        var result = converter.ConvertBack("123.45", targetTypes, null, culture);

        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBe(2);
        result[0].ShouldBe(123.45m);
        result[1].ShouldBe(BindingOperations.DoNothing);
    }

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
