using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue36228 : _IssuesUITest
{
	public Issue36228(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "Repeated button taps open the destination page multiple times";

#if ANDROID
	[Test]
	[Category(UITestCategories.Navigation)]
	public void RepeatedTapsOpenDestinationOnlyOnce()
	{
		const string resultId = "Issue36228NavigationResult";
		const string resultPrefix = "Destination count: ";
		const string checkNavigationStack = "Check navigation stack";

		App.WaitForElement("Issue36228Navigate");
		var initialText = App.WaitForElement(resultId).GetText();
		Assert.That(initialText, Is.EqualTo("-1"));

		int RunAttempt()
		{
			App.Tap("Issue36228Navigate");
			App.Tap("Issue36228Navigate");
			App.Tap("Issue36228Navigate");

			App.WaitForElementTillPageNavigationSettled("Issue36228Destination");
			App.WaitForElement(checkNavigationStack);
			App.Tap(checkNavigationStack);

			App.WaitForElement(resultId);
			var resultChanged = App.WaitForTextToBePresentInElement(resultId, resultPrefix, TimeSpan.FromSeconds(10));
			Assert.That(resultChanged, Is.True, "Issue36228 navigation stack count did not change from the -1 sentinel.");

			var resultText = App.WaitForElement(resultId).GetText();
			if (resultText is null)
				throw new AssertionException("Issue36228 navigation stack count was null.");

			Assert.That(resultText, Does.StartWith(resultPrefix));
			var countText = resultText[resultPrefix.Length..];
			var parsed = int.TryParse(countText, NumberStyles.None, CultureInfo.InvariantCulture, out var destinationCount);
			Assert.That(parsed, Is.True, $"Issue36228 navigation stack count '{countText}' was not an integer.");
			return destinationCount;
		}

		var firstDestinationCount = RunAttempt();
		Assert.That(firstDestinationCount, Is.EqualTo(1),
			$"Issue36228 navigation stack contained {firstDestinationCount} destination pages after the first attempt.");

		App.Tap("Issue36228Reset");
		var resetCompleted = App.WaitForTextToBePresentInElement(resultId, "-1", TimeSpan.FromSeconds(5));
		Assert.That(resetCompleted, Is.True, "Issue36228 reset did not restore the -1 sentinel.");
		Assert.That(App.WaitForElement(resultId).GetText(), Is.EqualTo("-1"));

		var secondDestinationCount = RunAttempt();
		Assert.That(secondDestinationCount, Is.EqualTo(1),
			$"Issue36228 navigation stack contained {secondDestinationCount} destination pages after the second attempt.");
	}
#endif
}
