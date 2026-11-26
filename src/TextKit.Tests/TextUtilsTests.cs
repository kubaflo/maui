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
	[InlineData("", 0)]
	[InlineData(" \t\r\n", 0)]
	[InlineData("one", 1)]
	[InlineData("one two", 2)]
	[InlineData("one  two", 2)]
	[InlineData(" one two ", 2)]
	[InlineData("one\ttwo", 2)]
	[InlineData("one\ntwo", 2)]
	[InlineData("one\u00A0two", 2)]
	public void WordCountCountsNonemptyWhitespaceSeparatedWords(string input, int expected)
		=> Assert.Equal(expected, TextUtils.WordCount(input));

	[Fact]
	public void WordCountReturnsZeroForNull()
		=> Assert.Equal(0, TextUtils.WordCount(null));
}
