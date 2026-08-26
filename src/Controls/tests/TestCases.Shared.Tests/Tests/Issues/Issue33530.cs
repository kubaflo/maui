#if ANDROID
using System.Diagnostics;
using System.Drawing;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue33530 : _IssuesUITest
{
	const int EdgeTolerance = 2;

	public override string Issue => "Border with Rotation and Start alignment is positioned incorrectly on initial load";

	public Issue33530(TestDevice device) : base(device) { }

	[Test]
	[Category(UITestCategories.Border)]
	public void InitiallyRotatedStartAlignedBorderTouchesModalPageLeftEdge()
	{
		App.SetOrientationPortrait();

		var launcherRect = WaitForStableRect("Issue33530Launcher");
		Assert.Multiple(() =>
		{
			Assert.That(launcherRect.Width, Is.GreaterThan(0), "The issue page must have a nonzero width.");
			Assert.That(launcherRect.Height, Is.GreaterThan(launcherRect.Width), "The device must be in portrait orientation.");
		});

		App.WaitForNoElement("AffectedModalPage");

		App.Tap("OpenAffectedModal");
		var affectedTitle = App.WaitForElement("AffectedTitle");
		Assert.That(affectedTitle.GetText(), Is.EqualTo("ROTATED BORDER"), "The affected Border title must identify the intended element.");

		var affectedPageRect = WaitForStableRect("AffectedModalPage");
		var affectedBorderRect = WaitForStableRect("AffectedBorder");
		var affectedTitleRect = WaitForStableRect("AffectedTitle");
		Assert.Multiple(() =>
		{
			Assert.That(affectedBorderRect.Width, Is.GreaterThan(0), "The affected Border must have a nonzero width.");
			Assert.That(affectedBorderRect.Height, Is.GreaterThan(affectedBorderRect.Width), "The affected Border must expose the transformed dimensions of a -90 degree rotation.");
			Assert.That(
				(double)affectedBorderRect.Height / affectedBorderRect.Width,
				Is.EqualTo(300d / 180d).Within(0.02),
				"The native frame must preserve the requested 300-by-180 Border dimensions after rotation.");
			Assert.That(affectedBorderRect.IntersectsWith(affectedTitleRect), Is.True, "The expected title must be located within the affected Border rendering.");
		});

		var observedDelta = Math.Abs(affectedBorderRect.Left - affectedPageRect.Left);
		Assert.That(
			observedDelta,
			Is.LessThanOrEqualTo(EdgeTolerance),
			$"Rotated Border visual left edge must touch the modal page left edge; observed={affectedBorderRect.Left}, expected={affectedPageRect.Left}, delta={observedDelta}, tolerance={EdgeTolerance}.");
	}

	Rectangle WaitForStableRect(string automationId)
	{
		var timeout = Stopwatch.StartNew();
		var previous = Rectangle.Empty;
		var stableSamples = 0;

		while (timeout.Elapsed < TimeSpan.FromSeconds(10))
		{
			var current = App.WaitForElementTillPageNavigationSettled(
				automationId,
				timeout: TimeSpan.FromSeconds(10)).GetRect();
			if (current.Width > 0 && current.Height > 0)
			{
				stableSamples = current == previous ? stableSamples + 1 : 1;
				previous = current;

				if (stableSamples >= 3)
					return current;
			}
		}

		Assert.Fail($"Element '{automationId}' did not reach a stable nonzero native rectangle.");
		return Rectangle.Empty;
	}
}
#endif
