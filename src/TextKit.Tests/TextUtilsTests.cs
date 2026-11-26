using System.Globalization;
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

	[Fact]
	public void SlugifyUsesInvariantCasing()
	{
		var previousCulture = CultureInfo.CurrentCulture;
		try
		{
			CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
			Assert.Equal(new string('i', 50), TextUtils.Slugify(new string('I', 50)));
		}
		finally
		{
			CultureInfo.CurrentCulture = previousCulture;
		}
	}
}
