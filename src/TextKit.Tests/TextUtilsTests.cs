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
	[InlineData("", true)]
	[InlineData("a", true)]
	[InlineData("abba", true)]
	[InlineData("ab", false)]
	[InlineData("abca", false)]
	[InlineData("Aba", false)]
	public void IsPalindromeComparesMirroredCharacters(string input, bool expected)
		=> Assert.Equal(expected, TextUtils.IsPalindrome(input));
}
