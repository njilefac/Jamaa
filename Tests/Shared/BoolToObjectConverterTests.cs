using System;
using System.Globalization;
using Jamaa.Desktop.Services.Converters;
using Xunit;

namespace UnitTests.Shared;

public class BoolToObjectConverterTests
{
    [Theory]
    [InlineData(true, "Yes", "No", "Yes")]
    [InlineData(false, "Yes", "No", "No")]
    [InlineData(null, "Yes", "No", "No")]
    public void Convert_ReturnsCorrectValue(bool? input, object trueVal, object falseVal, object expected)
    {
        var converter = new BoolToObjectConverter
        {
            TrueValue = trueVal,
            FalseValue = falseVal
        };

        var result = converter.Convert(input, typeof(object), null, CultureInfo.InvariantCulture);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ConvertBack_ReturnsTrue_WhenValueMatchesTrueValue()
    {
        var converter = new BoolToObjectConverter { TrueValue = "Visible", FalseValue = "Collapsed" };

        var result = converter.ConvertBack("Visible", typeof(bool), null, CultureInfo.InvariantCulture);

        Assert.Equal(true, result);
    }

    [Fact]
    public void ConvertBack_ReturnsFalse_WhenValueMatchesFalseValue()
    {
        var converter = new BoolToObjectConverter { TrueValue = "Visible", FalseValue = "Collapsed" };

        var result = converter.ConvertBack("Collapsed", typeof(bool), null, CultureInfo.InvariantCulture);

        Assert.Equal(false, result);
    }

    [Fact]
    public void ConvertBack_ReturnsTrue_WhenValueIsBooleanTrueAndTrueValueIsBooleanTrue()
    {
        var converter = new BoolToObjectConverter { TrueValue = true, FalseValue = false };

        var result = converter.ConvertBack(true, typeof(bool), null, CultureInfo.InvariantCulture);

        Assert.Equal(true, result);
    }

    [Fact]
    public void ConvertBack_ReturnsFalse_WhenValueIsBooleanTrueAndFalseValueIsBooleanTrue()
    {
        var converter = new BoolToObjectConverter { TrueValue = false, FalseValue = true };

        var result = converter.ConvertBack(true, typeof(bool), null, CultureInfo.InvariantCulture);

        Assert.Equal(false, result);
    }
}
