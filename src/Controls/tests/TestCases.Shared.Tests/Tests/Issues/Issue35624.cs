#if IOS
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35624 : _IssuesUITest
{
	public Issue35624(TestDevice device) : base(device)
	{
	}

	public override string Issue => "SearchHandler CharacterSpacing is not applied";

	[Test]
	[Category(UITestCategories.Shell)]
	public void SearchHandlerAppliesCharacterSpacingToNativeText()
	{
		App.WaitForTextToBePresentInElement("Issue35624InitialState", "InitialReady=True");
		var initialState = App.FindElement("Issue35624InitialState").GetText();
		Assert.That(initialState, Does.Contain("QueryEmpty=True"));
		Assert.That(initialState, Does.Contain("CharacterSpacing=10"));
		Assert.That(initialState, Does.Contain("ReferenceText=SPACING"));
		Assert.That(initialState, Does.Contain("ReferenceRange=0,7"));
		Assert.That(initialState, Does.Contain("ReferenceKerning=10"));

		var searchField = App.GetShellSearchHandler();
		searchField.Tap();
		searchField.SendKeys("SPACING");
		Assert.That(searchField.GetText(), Is.EqualTo("SPACING"));

		App.WaitForTextToBePresentInElement("Issue35624Inspection", "Callback=True");
		var inspection = App.FindElement("Issue35624Inspection").GetText();
		Assert.That(inspection, Does.Contain("NativeField=True"));
		Assert.That(inspection, Does.Contain("Text=SPACING"));
		Assert.That(inspection, Does.Contain("Range=0,7"));
		Assert.That(inspection, Does.Contain("Attached=True"));
		Assert.That(inspection, Does.Contain("Visible=True"));

		var kerningText = App.FindElement("Issue35624Kerning").GetText();
		Assert.That(double.TryParse(kerningText, NumberStyles.Float, CultureInfo.InvariantCulture, out var kerning), Is.True);
		Assert.That(kerning, Is.EqualTo(10).Within(0.01),
			$"SearchHandler native character spacing was {kerning} but expected 10.");
	}
}
#endif
