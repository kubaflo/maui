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
	[InlineData("", "")]
	[InlineData("a", "a")]
	[InlineData("abcd", "dcba")]
	[InlineData("abcde", "edcba")]
	public void ReverseReturnsExpectedOutput(string input, string expected)
		=> Assert.Equal(expected, TextUtils.Reverse(input));
}
