#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35624 : _IssuesUITest
{
	public Issue35624(TestDevice device) : base(device) { }

	public override string Issue => "SearchHandler CharacterSpacing is not applied";

	[Test]
	[Category(UITestCategories.Shell)]
	public void SearchHandlerAppliesCharacterSpacingToEnteredText()
	{
		App.SetOrientationPortrait();
		App.WaitForElement("Issue35624Reference");
		var windowSize = ((AppiumApp)App).Driver.Manage().Window.Size;
		Assert.That(windowSize.Height, Is.GreaterThan(windowSize.Width),
			$"The app window must be portrait, but measured {windowSize.Width}x{windowSize.Height}.");

		Assert.Multiple(() =>
		{
			Assert.That(App.FindElement("Issue35624ManagedQuery").GetText(), Is.EqualTo("Query: <empty>"));
			Assert.That(App.FindElement("Issue35624CallbackSequence").GetText(), Is.EqualTo("Callback: -1"));
			Assert.That(App.FindElement("Issue35624SearchKerning").GetText(), Is.EqualTo("Search kerning: -1; full range: False"));
			Assert.That(App.FindElement("Issue35624ConfiguredSpacing").GetText(), Is.EqualTo("Configured spacing: 12"));
			Assert.That(App.FindElement("Issue35624Reference").GetText(), Is.EqualTo("MAUI SEARCH"));
		});

		var searchField = App.GetShellSearchHandler();
		searchField.Tap();
		searchField.Clear();
		searchField.SendKeys("MAUI SEARCH");

		Assert.That(
			App.WaitForTextToBePresentInElement("Issue35624ManagedQuery", "Query: MAUI SEARCH"),
			Is.True,
			"SearchHandler Query callback did not receive the entered text.");
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue35624Inspection", "Inspection: complete"),
			Is.True,
			"Native inspection did not complete after the query changed.");

		Assert.Multiple(() =>
		{
			Assert.That(App.FindElement("Issue35624CallbackSequence").GetText(), Is.Not.EqualTo("Callback: -1"));
			Assert.That(searchField.GetText(), Is.EqualTo("MAUI SEARCH"));
			Assert.That(
				App.FindElement("Issue35624NativeState").GetText(),
				Is.EqualTo("Native attached: True; text: MAUI SEARCH"));
			Assert.That(
				App.FindElement("Issue35624ReferenceKerning").GetText(),
				Is.EqualTo("Reference kerning: 12; full range: True"));
		});

		var searchKerning = App.FindElement("Issue35624SearchKerning").GetText();
		var measuredKerning = searchKerning is null ? "<missing>" : ReadKerning(searchKerning);
		Assert.That(
			searchKerning,
			Is.EqualTo("Search kerning: 12; full range: True"),
			$"SearchHandler native kerning expected 12 but measured {measuredKerning}");
	}

	static string ReadKerning(string diagnostic)
	{
		const string prefix = "Search kerning: ";
		var start = diagnostic.IndexOf(prefix, StringComparison.Ordinal) + prefix.Length;
		var end = diagnostic.IndexOf(';', start);
		return diagnostic[start..end];
	}
}
#endif
