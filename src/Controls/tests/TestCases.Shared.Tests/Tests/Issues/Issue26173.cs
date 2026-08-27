using NUnit.Framework;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue26173
{
#if IOS
	[Test]
	[Category(UITestCategories.Fonts)]
	public void IncludeSampleContentDoesNotGenerateRestrictedFonts()
	{
		string[] productionTemplatePayload =
		{
			"Resources/Fonts/FluentSystemIcons-Regular.ttf",
			"Resources/Fonts/OpenSans-Regular.ttf",
			"Resources/Fonts/OpenSans-Semibold.ttf",
			"Resources/Fonts/SegoeUI-Semibold.ttf",
		};
		string[] includeSampleContentExclusions =
		{
			"MainPage.xaml",
			"MainPage.xaml.cs",
			"Resources/Images/dotnet_bot.png",
		};
		string[] restrictedFonts =
		{
			"FluentSystemIcons-Regular.ttf",
			"SegoeUI-Semibold.ttf",
		};

		var excludedWhenSampleContentIsIncluded = includeSampleContentExclusions
			.ToHashSet(StringComparer.Ordinal);
		var generatedFontNames = productionTemplatePayload
			.Where(resource => !excludedWhenSampleContentIsIncluded.Contains(resource))
			.Select(GetResourceName)
			.ToHashSet(StringComparer.Ordinal);
		var includedRestrictedFonts = restrictedFonts
			.Where(generatedFontNames.Contains)
			.ToArray();

		Assert.That(
			includedRestrictedFonts,
			Is.Empty,
			$"Generated sample content included restricted font files: {string.Join(", ", includedRestrictedFonts)}");
	}

	static string GetResourceName(string resource) => resource[(resource.LastIndexOf('/') + 1)..];
#endif
}

