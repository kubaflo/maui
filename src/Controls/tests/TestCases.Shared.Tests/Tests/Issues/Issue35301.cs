#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35301 : _IssuesUITest
{
	public Issue35301(TestDevice device) : base(device) { }

	public override string Issue => "Windows CollectionView applies WinUI styling by default";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void SelectingPlainLabelDoesNotAddPlatformSelectionChrome()
	{
		App.WaitForElement("Issue35301CollectionView");
		var apple = App.WaitForElement("Apple").GetRect();
		var banana = App.WaitForElement("Banana").GetRect();
		var cherry = App.WaitForElement("Cherry").GetRect();
		Assert.That(apple.Y, Is.LessThan(banana.Y));
		Assert.That(banana.Y, Is.LessThan(cherry.Y));

		var readyElement = App.WaitForElement(() =>
		{
			var element = App.FindElement("Issue35301Metrics");
			string text = element?.GetText() ?? string.Empty;
			return text.StartsWith("READY:", StringComparison.Ordinal) ? element : null;
		}, "Timed out waiting for the unselected CollectionView");
		string readyMetrics = readyElement.GetText() ?? string.Empty;
		Assert.That(readyMetrics, Does.Contain("callbacks=0"));
		Assert.That(readyMetrics, Does.Contain("selectedItem=<null>"));
		Assert.That(readyMetrics, Does.Contain("nativeListReady=True"));

		App.Tap("Apple");

		var completedElement = App.WaitForElement(() =>
		{
			var element = App.FindElement("Issue35301Metrics");
			string text = element?.GetText() ?? string.Empty;
			return text.StartsWith("COMPLETE:", StringComparison.Ordinal) ? element : null;
		}, "Timed out waiting for the post-selection callback");
		string metrics = completedElement.GetText() ?? string.Empty;
		Assert.That(metrics, Does.Contain("callbacks=1"));
		Assert.That(metrics, Does.Contain("selectedItem=Apple"));
		Assert.That(
			metrics,
			Does.Contain("selectionIndicatorSuppressed=True").And.Contain("roundedCornersSuppressed=True"),
			$"Selected Apple rendered non-template selection chrome: {metrics}");
	}
}
#endif
