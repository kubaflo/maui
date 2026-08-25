#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue30203 : _IssuesUITest
{
	public override string Issue => "[Windows] Navigation briefly exposes the window background";

	public Issue30203(TestDevice testDevice) : base(testDevice)
	{
	}

	[Test]
	[Category(UITestCategories.Navigation)]
	public void AnimatedNavigationRetainsPageBackground()
	{
		Assert.That(
			App.WaitForTextToBePresentInElement("FirstTransitionResult", "PASS: Ready for cycle 1", TimeSpan.FromSeconds(15)),
			Is.True);

		var observedBackgrounds = new List<string>();

		for (var cycle = 1; cycle <= 3; cycle++)
		{
			App.Tap("NavigateButton");
			App.WaitForElement("SecondPage");
			Assert.That(
				App.WaitForTextToBePresentInElement("SecondTransitionResult", $"Cycle {cycle} complete", TimeSpan.FromSeconds(15)),
				Is.True);
			var observedBackground = App.WaitForElement("SecondTransitionResult").GetText();
			if (observedBackground is null)
				throw new AssertionException($"Cycle {cycle} completed without reporting its native transition surface.");

			observedBackgrounds.Add(observedBackground);

			if (cycle < 3)
			{
				App.Tap("ReturnButton");
				App.WaitForElement("FirstPage");
				Assert.That(
					App.WaitForTextToBePresentInElement("FirstTransitionResult", $"PASS: Ready for cycle {cycle + 1}", TimeSpan.FromSeconds(15)),
					Is.True);
			}
		}

		Assert.That(observedBackgrounds, Has.Count.EqualTo(3), "Not every animated navigation cycle reported its native transition surface.");
		Assert.That(
			observedBackgrounds,
			Is.EqualTo(new[]
			{
				"Cycle 1 complete; navigation surface background=#FFF2E85C",
				"Cycle 2 complete; navigation surface background=#FFF2E85C",
				"Cycle 3 complete; navigation surface background=#FFF2E85C"
			}),
			$"Navigation transition exposed non-page-background pixels; expected native transition surface #FFF2E85C, " +
			$"observed: {string.Join(" | ", observedBackgrounds)}");
	}
}
#endif
