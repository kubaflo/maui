#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35667 : _IssuesUITest
{
	public Issue35667(TestDevice device) : base(device) { }

	public override string Issue => "TextTransform.Uppercase does not work on Shell SearchHandler";

	[Test]
	[Category(UITestCategories.Shell)]
	public void SearchHandlerDisplaysUppercaseText()
	{
		Assert.That(
			App.WaitForElement("Issue35667Configuration").GetText(),
			Is.EqualTo("Uppercase|Expanded"));
		Assert.That(
			App.WaitForElement("Issue35667QueryChanged").GetText(),
			Is.EqualTo("QUERY_NOT_CHANGED"));

		var searchHandler = App.GetShellSearchHandler();
		searchHandler.Click();
		App.EnterTextInShellSearchHandler("maui");

		Assert.That(
			App.WaitForTextToBePresentInElement(
				"Issue35667QueryChanged",
				"QUERY_CHANGED",
				timeout: TimeSpan.FromSeconds(10)),
			Is.True,
			"The SearchHandler Query callback did not run after text entry.");

		App.RetryAssert(
			() => Assert.That(
				App.GetShellSearchHandler().GetText(),
				Is.Not.Empty,
				"The native Shell search field did not receive the typed text."),
			timeout: TimeSpan.FromSeconds(10));

		var displayedText = App.GetShellSearchHandler().GetText();
		Assert.That(
			displayedText,
			Is.EqualTo("MAUI"),
			$"Shell SearchHandler displayed '{displayedText}'; TextTransform.Uppercase expected 'MAUI' after typing 'maui'.");
	}
}
#endif
