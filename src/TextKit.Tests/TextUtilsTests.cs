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

	[Theory]
	[InlineData("", "")]
	[InlineData("Hello World", "hello-world")]
	[InlineData("   --   ", "")]
	[InlineData("   --   Hello                             World    --    ", "hello-world")]
	public void SlugifyReturnsShortNormalizedTitles(string title, string expected)
		=> Assert.Equal(expected, TextUtils.Slugify(title));

	[Theory]
	[InlineData(50)]
	[InlineData(51)]
	public void SlugifyLimitsNormalizedLength(int length)
		=> Assert.Equal(new string('a', 50), TextUtils.Slugify(new string('A', length)));

	[Theory]
	[InlineData(48, "\U0001F600")]
	[InlineData(49, "")]
	public void SlugifyDoesNotSplitSurrogatePairs(int prefixLength, string expectedSuffix)
	{
		var prefix = new string('a', prefixLength);
		Assert.Equal(prefix + expectedSuffix, TextUtils.Slugify(prefix + "\U0001F600z"));
	}
}
