#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue32579 : _IssuesUITest
{
	public override string Issue => "Horizontal scrollbar flickers when opening a horizontal CollectionView";

	public Issue32579(TestDevice device) : base(device)
	{
	}

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void HorizontalScrollbarRemainsStableAfterLoad()
	{
		var observations = new string[2] { "not observed", "not observed" };

		for (int cycle = 0; cycle < observations.Length; cycle++)
		{
			App.Tap("Issue32579OpenButton");
			App.WaitForElement(
				"Issue32579AffectedCollection",
				timeout: TimeSpan.FromSeconds(15));
			App.WaitForElement("Issue32579Monkey0");

			Assert.That(
				App.WaitForTextToBePresentInElement(
					"Issue32579ResetButton",
					"Observed:",
					timeout: TimeSpan.FromSeconds(5)),
				Is.True,
				"The native scrollbar observation did not complete after the first item loaded.");
			observations[cycle] = App.FindElement("Issue32579ResetButton").GetText()!;

			if (cycle + 1 < observations.Length)
				App.Tap("Issue32579ResetButton");
		}

		Assert.That(observations, Is.All.EqualTo("Observed: scrollbar stable"),
			$"Issue32579 scrollbar instability: observations={string.Join(",", observations)}");
	}
}
#endif
