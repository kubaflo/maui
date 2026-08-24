using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

#if IOS && !MACCATALYST
public class Issue35624 : _IssuesUITest
{
	public Issue35624(TestDevice device) : base(device)
	{
	}

	public override string Issue => "SearchHandler CharacterSpacing property is not applied";

	[Test]
	[Category(UITestCategories.Shell)]
	public void SearchHandlerAppliesCharacterSpacingToNativeText()
	{
		Assert.That(
			App.WaitForTextToBePresentInElement(
				"Issue35624Configuration",
				"CharacterSpacing=10; Placeholder=SearchHandler; Visibility=Collapsible; Query=<empty>",
				TimeSpan.FromSeconds(10)),
			Is.True);
		Assert.That(
			App.WaitForTextToBePresentInElement(
				"Issue35624Kerning",
				"Search=-1; Label=-1",
				TimeSpan.FromSeconds(10)),
			Is.True);

		var initialCallbackStatus = App.WaitForElement("Issue35624CallbackStatus");
		if (initialCallbackStatus is null)
			throw new AssertionException("The initial Query callback status was not found.");

		var initialCallbackText = initialCallbackStatus.GetText();
		if (initialCallbackText is null)
			throw new AssertionException("The initial Query callback status had no text.");

		var initialCallbackCount = ParseCallbackCount(initialCallbackText);

		var searchHandler = App.GetShellSearchHandler();
		if (searchHandler is null)
			throw new AssertionException("The native Shell SearchHandler was not found.");

		searchHandler.Tap();
		searchHandler.SendKeys("SPACING");

		Assert.That(
			App.WaitForTextToBePresentInElement(
				"Issue35624CallbackStatus",
				"Query=SPACING",
				TimeSpan.FromSeconds(10)),
			Is.True,
			"The SearchHandler Query callback did not observe the entered text.");
		Assert.That(
			App.WaitForTextToBePresentInElement(
				"Issue35624NativeViews",
				"Search=True; Label=True; AttributedSearch=True; AttributedLabel=True",
				TimeSpan.FromSeconds(10)),
			Is.True,
			"The intended native SearchHandler and Label attributed strings were not found.");
		Assert.That(
			App.WaitForTextToBePresentInElement(
				"Issue35624Ranges",
				"SearchRange=7; LabelRange=7",
				TimeSpan.FromSeconds(10)),
			Is.True,
			"The native kerning measurements did not cover the entered SPACING text.");

		var callbackStatus = App.WaitForElement("Issue35624CallbackStatus");
		if (callbackStatus is null)
			throw new AssertionException("The Query callback status was not found.");

		var callbackText = callbackStatus.GetText();
		if (callbackText is null)
			throw new AssertionException("The Query callback status had no text.");

		Assert.That(ParseCallbackCount(callbackText), Is.GreaterThan(initialCallbackCount),
			"The SearchHandler Query callback count did not increase after text entry.");
		Assert.That(callbackText.Split(';')[1].Trim(), Is.EqualTo("Query=SPACING"));

		var kerningStatus = App.WaitForElement("Issue35624Kerning");
		if (kerningStatus is null)
			throw new AssertionException("The native kerning result was not found.");

		var kerningText = kerningStatus.GetText();
		if (kerningText is null)
			throw new AssertionException("The native kerning result had no text.");

		var values = kerningText.Split(';');
		var searchKerning = double.Parse(
			values[0].Replace("Search=", string.Empty, StringComparison.Ordinal),
			CultureInfo.InvariantCulture);
		var labelKerning = double.Parse(
			values[1].Replace("Label=", string.Empty, StringComparison.Ordinal).Trim(),
			CultureInfo.InvariantCulture);

		Assert.That(labelKerning, Is.EqualTo(10).Within(0.01),
			$"Reference Label native kerning was {labelKerning} instead of configured CharacterSpacing 10.");
		Assert.That(searchKerning, Is.EqualTo(10).Within(0.01),
			$"SearchHandler native kerning was {searchKerning} instead of configured CharacterSpacing 10.");
	}

	static int ParseCallbackCount(string callbackText)
	{
		var callbackCountText = callbackText.Split(';')[0].Replace("Callbacks=", string.Empty, StringComparison.Ordinal);
		return int.Parse(callbackCountText, CultureInfo.InvariantCulture);
	}
}
#endif
