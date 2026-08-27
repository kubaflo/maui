#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue29120 : _IssuesUITest
{
	public Issue29120(TestDevice device) : base(device)
	{
	}

	public override string Issue => "CollectionView jumps to the top during incremental loading";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void IncrementalLoadingPreservesVisibleItems()
	{
		App.WaitForElement("CompletedLoadGeneration");
		App.WaitForElement("Bear 1");
		Assert.That(App.FindElements("Bear 1").Count, Is.EqualTo(1));
		Assert.That(App.FindElement("CompletedLoadGeneration").GetText(), Is.EqualTo("-1"));

		var appiumApp = App as AppiumApp;
		if (appiumApp is null)
			throw new InvalidOperationException("The Windows UI test requires an Appium application.");

		var windowSize = appiumApp.Driver.Manage().Window.Size;
		var centerX = windowSize.Width / 2;
		var startY = windowSize.Height * 4 / 5;
		var endY = windowSize.Height / 5;

		for (var gesture = 0; gesture < 3; gesture++)
		{
			var scrollGenerationTextBeforeGesture = App.FindElement("ScrollGeneration").GetText();
			if (scrollGenerationTextBeforeGesture is null)
				throw new InvalidOperationException("ScrollGeneration did not expose text before the gesture.");

			var scrollGenerationBeforeGesture = int.Parse(scrollGenerationTextBeforeGesture);

			App.DragCoordinates(centerX, startY, centerX, endY);

			App.RetryAssert(() =>
			{
				var scrollGenerationTextAfterGesture = App.FindElement("ScrollGeneration").GetText();
				if (scrollGenerationTextAfterGesture is null)
					throw new InvalidOperationException("ScrollGeneration did not expose text after the gesture.");

				var scrollGenerationAfterGesture = int.Parse(scrollGenerationTextAfterGesture);
				Assert.That(
					scrollGenerationAfterGesture,
					Is.GreaterThan(scrollGenerationBeforeGesture),
					$"CollectionView did not process scroll gesture {gesture + 1}.");
			});
		}

		Assert.That(
			App.WaitForTextToBePresentInElement(
				"CompletedLoadGeneration",
				"0",
				timeout: TimeSpan.FromSeconds(10)),
			Is.True);

		var firstItemDisplayedCount = App.FindElements("Bear 1").Count;
		Assert.That(
			firstItemDisplayedCount,
			Is.EqualTo(0),
			$"CollectionView jumped to the first item after incremental loading; displayed count was {firstItemDisplayedCount}, expected 0.");
	}
}
#endif
