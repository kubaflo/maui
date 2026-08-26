#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue26173 : _IssuesUITest
{
	public Issue26173(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "Fancy Sample Code Uses Copyrighted Fonts";

	[Test]
	[Category(UITestCategories.Fonts)]
	public void GeneratedSampleDoesNotPackageRestrictedFonts()
	{
		const string callbackPrefix = "CallbackToken:";

		string GetRequiredText(string automationId)
		{
			var element = App.WaitForElement(automationId);
			if (element is null)
			{
				throw new AssertionException($"Element '{automationId}' was not found.");
			}

			var text = element.GetText();
			if (text is null)
			{
				throw new AssertionException($"Element '{automationId}' did not expose text.");
			}

			return text;
		}

		Assert.That(GetRequiredText("SampleContentState"), Is.EqualTo("Include Sample Content: selected"));
		Assert.That(App.FindElements("GeneratedProject"), Is.Empty);

		var initialCallback = GetRequiredText("CallbackToken");
		Assert.That(initialCallback, Does.StartWith(callbackPrefix));
		Assert.That(initialCallback[callbackPrefix.Length..], Is.Empty);

		App.Tap("CreateSampleProjectButton");

		App.WaitForElement("GeneratedProject");
		var completedCallback = GetRequiredText("CallbackToken");
		Assert.That(completedCallback, Is.EqualTo("CallbackToken:created"));

		var restrictedFonts = new List<string>();
		var restrictedFontElements = new[]
		{
			("FluentSystemIconsFont", "FluentSystemIcons-Regular.ttf"),
			("SegoeUIFont", "SegoeUI-Semibold.ttf"),
		};

		foreach (var (automationId, fontName) in restrictedFontElements)
		{
			if (App.FindElements(automationId).Count > 0)
			{
				Assert.That(GetRequiredText(automationId), Is.EqualTo(fontName));
				restrictedFonts.Add(fontName);
			}
		}

		Assert.That(restrictedFonts, Is.Empty,
			$"Generated iOS sample package unexpectedly contains restricted fonts: {string.Join(", ", restrictedFonts)}");
	}
}
#endif
