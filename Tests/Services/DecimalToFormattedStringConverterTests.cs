using System.Globalization;
using Jamaa.Desktop.Services.Converters;
using Shouldly;
using Xunit;

namespace UnitTests.Services;

public class DecimalToFormattedStringConverterTests
{
    private readonly DecimalToFormattedStringConverter _converter = new();

    [Fact]
    public void Convert_MultiBinding_FormatsUsingProvidedSeparator()
    {
        var result = _converter.Convert(
            [1234567.89m, 2, "'"],
            typeof(string),
            null,
            CultureInfo.InvariantCulture);

        result.ShouldBe("1'234'567.89");
    }

    [Fact]
    public void Convert_MultiBinding_UsesPrecisionFromValues()
    {
        var result = _converter.Convert(
            [123.45m, 3],
            typeof(string),
            null,
            CultureInfo.InvariantCulture);

        result.ShouldBe("123.450");
    }
}
