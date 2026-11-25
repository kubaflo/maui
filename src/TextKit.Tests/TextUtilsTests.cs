#nullable enable

using TextKit;
using Xunit;

public class TextUtilsTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("maui", "Maui")]
    public void CapitalizeUppercasesFirstLetter(string? input, string expected)
        => Assert.Equal(expected, TextUtils.Capitalize(input));

    [Theory]
    [InlineData(null, 5, "")]
    [InlineData("", 5, "")]
    [InlineData("abc", 5, "abc")]
    [InlineData("abcde", 5, "abcde")]
    [InlineData("abcdef", 5, "ab...")]
    public void TruncateHandlesNullAndPreservesTextWithinLimit(string? input, int maxLength, string expected)
        => Assert.Equal(expected, TextUtils.Truncate(input, maxLength));
}
