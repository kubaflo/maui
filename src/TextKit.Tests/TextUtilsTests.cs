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

	[Theory]
	[InlineData('#')]
	[InlineData('/')]
	[InlineData('?')]
	[InlineData('\\')]
	[InlineData('%')]
	public void SlugifyNormalizesUrlDelimiters(char delimiter)
	{
		var prefix = new string('a', 25);
		var suffix = new string('b', 24);
		Assert.Equal(prefix + "-" + suffix, TextUtils.Slugify(prefix + delimiter + suffix));
	}

	[Theory]
	[InlineData(".")]
	[InlineData("..")]
	public void SlugifyDoesNotReturnDotSegments(string title)
		=> Assert.Equal("", TextUtils.Slugify(title));

	[Theory]
	[InlineData("", "")]
	[InlineData("abc", "cba")]
	[InlineData("A\U0001F600B", "B\U0001F600A")]
	[InlineData("e\u0301x", "xe\u0301")]
	[InlineData("A\U0001F469\u200D\U0001F4BBe\u0301B", "Be\u0301\U0001F469\u200D\U0001F4BBA")]
	public void ReverseTextReversesWholeTextElements(string input, string expected)
		=> Assert.Equal(expected, TextUtils.ReverseText(input));

	[Fact]
	public void ReverseTextTreatsNullAsEmpty()
		=> Assert.Equal("", TextUtils.ReverseText(null));
}
