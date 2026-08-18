#if ANDROID
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue37281 : _IssuesUITest
{
	public override string Issue => "Scrolling redraws content inside a shadowed container on Android";

	public Issue37281(TestDevice device)
		: base(device)
	{
	}

	[Test]
	[Category(UITestCategories.ScrollView)]
	public void TouchScrollingDoesNotRedrawShadowedContent()
	{
		const string statusId = "Issue37281Status";
		const string failureSignature = "Shadowed ScrollView redraw counts after touch-scroll cycles";

		App.SetOrientationPortrait();
		var windowSize = ((AppiumApp)App).Driver.Manage().Window.Size;
		Assert.That(windowSize.Height, Is.GreaterThan(windowSize.Width), "The issue requires portrait orientation.");

		var scrollRect = App.WaitForElement("AffectedScrollView").GetRect();
		var firstRow = App.WaitForElement("ScrollableRow1");
		Assert.That(firstRow.GetText(), Is.EqualTo("Scrollable row 1"));
		Assert.That(firstRow.GetRect().Y, Is.InRange(scrollRect.Y, scrollRect.Y + scrollRect.Height));

		App.WaitForTextToBePresentInElement(statusId, "Initial=complete", timeout: TimeSpan.FromSeconds(10));
		string initialStatus = ReadStatus();
		Assert.That(initialStatus, Does.Contain("Rows=120"));
		Assert.That(initialStatus, Does.Contain("Shadow=6,6/12/0.8"));

		int[] redrawCounts = new int[3];
		double[] offsetsBefore = new double[3];
		double[] offsetsAfter = new double[3];
		double[] nativeOffsetsBefore = new double[3];
		double[] nativeOffsetsAfter = new double[3];

		for (int cycle = 1; cycle <= 3; cycle++)
		{
			App.Tap("Issue37281ArmButton");
			App.WaitForTextToBePresentInElement(statusId, $"Cycle={cycle}; Draws=0; Scrolled=no; CallbackY=-1");

			string armedStatus = ReadStatus();
			Assert.That(ReadMetric(armedStatus, "Draws"), Is.Zero);
			Assert.That(ReadMetric(armedStatus, "CallbackY"), Is.EqualTo(-1));
			offsetsBefore[cycle - 1] = ReadMetric(armedStatus, "Offset");
			nativeOffsetsBefore[cycle - 1] = ReadMetric(armedStatus, "NativeY");

			float centerX = scrollRect.X + (scrollRect.Width / 2);
			float dragStartY = scrollRect.Y + (scrollRect.Height * 0.75f);
			float dragEndY = scrollRect.Y + (scrollRect.Height * 0.25f);
			App.DragCoordinates(centerX, dragStartY, centerX, dragEndY);
			App.DragCoordinates(centerX, dragStartY, centerX, dragEndY);

			App.WaitForTextToBePresentInElement(statusId, $"Cycle={cycle};", timeout: TimeSpan.FromSeconds(10));
			App.WaitForTextToBePresentInElement(statusId, "Scrolled=yes", timeout: TimeSpan.FromSeconds(10));
			string scrolledStatus = ReadStatus();
			double callbackY = ReadMetric(scrolledStatus, "CallbackY");
			offsetsAfter[cycle - 1] = ReadMetric(scrolledStatus, "Offset");
			nativeOffsetsAfter[cycle - 1] = ReadMetric(scrolledStatus, "NativeY");
			redrawCounts[cycle - 1] = (int)ReadMetric(scrolledStatus, "Draws");

			Assert.That(callbackY, Is.GreaterThanOrEqualTo(0), $"Cycle {cycle} did not report a managed Scrolled callback.");
			Assert.That(nativeOffsetsAfter[cycle - 1], Is.GreaterThan(nativeOffsetsBefore[cycle - 1] + 1),
				$"Cycle {cycle} did not advance the native ScrollView offset.");
			Assert.That(offsetsAfter[cycle - 1], Is.GreaterThan(offsetsBefore[cycle - 1] + 1),
				$"Cycle {cycle} did not advance the managed touch-scroll offset.");
		}

		Assert.That(redrawCounts, Is.EqualTo(new[] { 0, 0, 0 }),
			$"{failureSignature}. Counts: {string.Join(", ", redrawCounts)}; " +
			$"offsets: {offsetsBefore[0]:0.##}->{offsetsAfter[0]:0.##}, " +
			$"{offsetsBefore[1]:0.##}->{offsetsAfter[1]:0.##}, " +
			$"{offsetsBefore[2]:0.##}->{offsetsAfter[2]:0.##}; native: " +
			$"{nativeOffsetsBefore[0]:0.##}->{nativeOffsetsAfter[0]:0.##}, " +
			$"{nativeOffsetsBefore[1]:0.##}->{nativeOffsetsAfter[1]:0.##}, " +
			$"{nativeOffsetsBefore[2]:0.##}->{nativeOffsetsAfter[2]:0.##}.");

		string ReadStatus() =>
			App.WaitForElement(statusId).GetText()
			?? throw new InvalidOperationException("Issue37281 status text was null.");

		static double ReadMetric(string status, string metric)
		{
			string marker = $"{metric}=";
			int start = status.IndexOf(marker, StringComparison.Ordinal);
			if (start < 0)
				throw new InvalidOperationException($"Metric '{metric}' was missing from '{status}'.");

			start += marker.Length;
			int end = status.IndexOf(';', start);
			string value = end < 0 ? status[start..] : status[start..end];
			return double.Parse(value, CultureInfo.InvariantCulture);
		}
	}
}
#endif
