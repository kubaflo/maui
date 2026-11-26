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
	[InlineData("ada lovelace", "AL")]
	[InlineData("Ada  Lovelace", "AL")]
	[InlineData(" Ada Lovelace", "AL")]
	[InlineData("Ada Lovelace ", "AL")]
	[InlineData("", "")]
	[InlineData("   ", "")]
	public void InitialsIgnoresEmptyNameParts(string input, string expected)
		=> Assert.Equal(expected, TextUtils.Initials(input));

	[Theory]
	[InlineData("\U00020BB7 tanaka", "\U00020BB7T")]
	[InlineData("ada \U00020BB7", "A\U00020BB7")]
	public void InitialsPreservesSupplementaryCharacters(string input, string expected)
		=> Assert.Equal(expected, TextUtils.Initials(input));

	[Theory]
	[InlineData("Ada\tLovelace", "AL")]
	[InlineData("\tAda \r\n Lovelace\n", "AL")]
	[InlineData("Ada\u00A0Lovelace", "AL")]
	[InlineData("\t\r\n\u00A0", "")]
	public void InitialsSplitsOnAnyWhitespace(string input, string expected)
		=> Assert.Equal(expected, TextUtils.Initials(input));
}
