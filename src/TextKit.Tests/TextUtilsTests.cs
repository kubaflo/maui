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
	[InlineData("123456", 4, "**3456")]
	[InlineData("123456", 0, "******")]
	[InlineData("123456", 6, "123456")]
	[InlineData("a", 4, "a")]
	[InlineData("", 4, "")]
	public void MaskPreservesAvailableSuffix(string value, int visible, string expected)
		=> Assert.Equal(expected, TextUtils.Mask(value, visible));

	[Theory]
	[InlineData("abc", -1)]
	[InlineData("", int.MinValue)]
	public void MaskRejectsNegativeVisibility(string value, int visible)
		=> Assert.Throws<ArgumentOutOfRangeException>(nameof(visible), () => TextUtils.Mask(value, visible));
}
