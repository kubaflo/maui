using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues
{
#if IOS
	public class Issue26173 : UITest
	{
		public Issue26173(TestDevice testDevice) : base(testDevice) { }

		[Test]
		[Category(UITestCategories.Fonts)]
		public void GeneratedSampleContentDoesNotIncludeRestrictedFonts()
		{
			var generatedSampleFonts = new[]
			{
				"FluentSystemIcons-Regular.ttf",
				"SegoeUI-Semibold.ttf",
			};

			Assert.That(
				generatedSampleFonts,
				Is.Empty,
				"Issue26173: generated sample includes FluentSystemIcons-Regular.ttf");
		}
	}
#endif
}

