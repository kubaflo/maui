using System;
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
	[InlineData("ab", 0, "")]
	[InlineData("ab", 1, "ab")]
	[InlineData("ab", 3, "ababab")]
	public void RepeatReturnsExactlyCountCopies(string text, int count, string expected)
		=> Assert.Equal(expected, TextUtils.Repeat(text, count));

	[Fact]
	public void RepeatThrowsForNegativeCount()
		=> Assert.Throws<ArgumentOutOfRangeException>("count", () => TextUtils.Repeat("ab", -1));
}
