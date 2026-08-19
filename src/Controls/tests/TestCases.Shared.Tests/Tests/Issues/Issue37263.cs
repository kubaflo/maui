#if IOS && !MACCATALYST
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue37263 : _IssuesUITest
{
	public override string Issue => "ScrollView Container safe area is horizontally misaligned in landscape";

	public Issue37263(TestDevice device) : base(device)
	{
	}

	[Test]
	[Category(UITestCategories.SafeAreaEdges)]
	public void ContainerSafeAreaKeepsScrollViewContentHorizontallyAligned()
	{
		App.SetOrientationLandscape();

		var page = App.WaitForElement(() =>
		{
			var element = App.FindElement("Issue37263Page");
			var rect = element.GetRect();
			return rect.Width > rect.Height ? element : null;
		}, "Issue37263 did not reach landscape orientation.");

		var initialStatusElement = App.WaitForElement(() =>
		{
			var element = App.FindElement("Issue37263LayoutStatus");
			var text = element.GetText() ?? string.Empty;
			if (!text.Contains("Mode=Default", StringComparison.Ordinal))
				return null;

			return Math.Max(ReadMetric(text, "InsetLeft"), ReadMetric(text, "InsetRight")) > 0 ? element : null;
		}, "Issue37263 did not complete its initial Default layout.");
		var initialStatus = initialStatusElement.GetText() ?? string.Empty;
		var initialGeneration = ReadMetric(initialStatus, "Generation");
		var initialLeftInset = ReadMetric(initialStatus, "InsetLeft");
		var initialRightInset = ReadMetric(initialStatus, "InsetRight");

		Assert.That(initialGeneration, Is.GreaterThanOrEqualTo(0), "The initial native layout callback must occur.");
		Assert.That(Math.Max(initialLeftInset, initialRightInset), Is.GreaterThan(0),
			$"The landscape window must supply a nonzero horizontal adjusted inset. Status: {initialStatus}");

		var pageRect = page.GetRect();
		var initialGridRect = App.WaitForElement("Issue37263Grid").GetRect();
		var initialLeftMarker = App.WaitForElement("Issue37263LeftMarker").GetRect();
		var initialRightMarker = App.WaitForElement("Issue37263RightMarker").GetRect();
		AssertMarkerColumns(initialGridRect, initialLeftMarker, initialRightMarker);

		var initialLeftGap = initialGridRect.X - pageRect.X;
		var initialRightGap = pageRect.X + pageRect.Width - initialGridRect.X - initialGridRect.Width;
		Assert.That(Math.Abs(initialLeftGap - initialRightGap), Is.LessThanOrEqualTo(1.01),
			$"The Default reference layout must be aligned. Left={initialLeftGap:F2}, Right={initialRightGap:F2}.");

		App.Tap("Issue37263ContainerButton");

		var containerStatusElement = App.WaitForElement(() =>
		{
			var element = App.FindElement("Issue37263LayoutStatus");
			var text = element.GetText() ?? string.Empty;
			if (!text.Contains("Mode=Container", StringComparison.Ordinal))
				return null;

			return ReadMetric(text, "Generation") > initialGeneration ? element : null;
		}, "Issue37263 did not observe the Container state transition.");
		var containerStatus = containerStatusElement.GetText() ?? string.Empty;
		Assert.That(App.WaitForElement("Issue37263Mode").GetText(), Is.EqualTo("SafeAreaEdges: Container"));

		App.RetryAssert(() =>
		{
			var currentPageRect = App.FindElement("Issue37263Page").GetRect();
			var gridRect = App.FindElement("Issue37263Grid").GetRect();
			var leftMarker = App.FindElement("Issue37263LeftMarker").GetRect();
			var rightMarker = App.FindElement("Issue37263RightMarker").GetRect();
			AssertMarkerColumns(gridRect, leftMarker, rightMarker);

			var leftGap = gridRect.X - currentPageRect.X;
			var rightGap = currentPageRect.X + currentPageRect.Width - gridRect.X - gridRect.Width;
			Assert.That(
				Math.Abs(leftGap - rightGap),
				Is.LessThanOrEqualTo(1.01),
				$"Issue37263 Container alignment mismatch: leftGap={leftGap:F2}, rightGap={rightGap:F2}, " +
				$"pageFrame={currentPageRect}, gridFrame={gridRect}, safeAreaInsets={containerStatus}, tolerance=1.01.");
		}, timeout: TimeSpan.FromSeconds(5));
	}

	static void AssertMarkerColumns(System.Drawing.Rectangle grid, System.Drawing.Rectangle left, System.Drawing.Rectangle right)
	{
		Assert.Multiple(() =>
		{
			Assert.That(left.Width, Is.EqualTo(60).Within(1.01), "The green marker must retain its 60-point width.");
			Assert.That(right.Width, Is.EqualTo(60).Within(1.01), "The orange marker must retain its 60-point width.");
			Assert.That(left.X, Is.EqualTo(grid.X).Within(1.01), "The green marker must occupy the left grid column.");
			Assert.That(right.X + right.Width, Is.EqualTo(grid.X + grid.Width).Within(1.01),
				"The orange marker must occupy the right grid column.");
		});
	}

	static double ReadMetric(string status, string name)
	{
		var prefix = name + "=";
		foreach (var component in status.Split(';'))
		{
			if (component.StartsWith(prefix, StringComparison.Ordinal))
				return double.Parse(component[prefix.Length..], System.Globalization.CultureInfo.InvariantCulture);
		}

		Assert.Fail($"Missing {name} in native layout status: {status}");
		return double.NaN;
	}
}
#endif
