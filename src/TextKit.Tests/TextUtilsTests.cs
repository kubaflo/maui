using System;
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
	[InlineData("abcdef", 5, "abcd…")]
	[InlineData("x", 0, "")]
	[InlineData("ab", 1, "…")]
	public void TruncateCountsEllipsisTowardMaxLength(string text, int maxLength, string expected)
		=> Assert.Equal(expected, TextUtils.Truncate(text, maxLength));

	[Theory]
	[InlineData("\U0001F600x", 1, "…")]
	[InlineData("\U0001F600xy", 2, "…")]
	[InlineData("a\U0001F600xy", 3, "a…")]
	[InlineData("\U0001F600xy", 3, "\U0001F600…")]
	public void TruncateDoesNotSplitSurrogatePairs(string text, int maxLength, string expected)
		=> Assert.Equal(expected, TextUtils.Truncate(text, maxLength));

	[Theory]
	[InlineData("abc", 4)]
	[InlineData("abc", 3)]
	[InlineData("", 0)]
	public void TruncateReturnsTextUnchangedWhenWithinMaxLength(string text, int maxLength)
		=> Assert.Equal(text, TextUtils.Truncate(text, maxLength));

	[Theory]
	[InlineData(-1)]
	[InlineData(int.MinValue)]
	public void TruncateRejectsNegativeMaxLength(int maxLength)
		=> Assert.Throws<ArgumentOutOfRangeException>(() => TextUtils.Truncate("abc", maxLength));
}
