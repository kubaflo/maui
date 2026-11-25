#nullable enable

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
	[InlineData(null, 5, "")]
	[InlineData("", 5, "")]
	[InlineData("abc", 5, "abc")]
	[InlineData("abcde", 5, "abcde")]
	[InlineData("abcdef", 5, "ab...")]
	public void TruncateHandlesNullAndPreservesTextWithinLimit(string? input, int maxLength, string expected)
		=> Assert.Equal(expected, TextUtils.Truncate(input, maxLength));

	[Theory]
	[InlineData("abcd", 0, "")]
	[InlineData("abcd", 1, "a")]
	[InlineData("abcd", 2, "ab")]
	[InlineData("abcd", 3, "...")]
	[InlineData(null, 0, "")]
	[InlineData("", 0, "")]
	[InlineData("", 2, "")]
	[InlineData("a", 2, "a")]
	[InlineData("ab", 2, "ab")]
	[InlineData("abc", 3, "abc")]
	public void TruncateHandlesSmallNonnegativeLimits(string? input, int maxLength, string expected)
		=> Assert.Equal(expected, TextUtils.Truncate(input, maxLength));

	[Theory]
	[InlineData(null, -1)]
	[InlineData("", -1)]
	[InlineData("abcd", -1)]
	[InlineData("abcd", int.MinValue)]
	public void TruncateRejectsNegativeLimits(string? input, int maxLength)
	{
		var exception = Assert.Throws<ArgumentOutOfRangeException>(() => TextUtils.Truncate(input, maxLength));
		Assert.Equal("maxLength", exception.ParamName);
	}
}
