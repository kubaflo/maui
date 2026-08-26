#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue26094 : _IssuesUITest
{
	const double ExpectedWidth = 168;
	const double ExpectedHeight = 208;
	const double GeometryTolerance = 2;

	public Issue26094(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "Image renders at full window size instead of its intrinsic size on AbsoluteLayout";

	[Test]
	[Category(UITestCategories.Image)]
	public void SwappedImageDoesNotExceedItsIntrinsicSize()
	{
		App.WaitForElement("SwapButton");

		var initialImageRect = App.WaitForElement("AffectedImage").GetRect();
		Assert.Multiple(() =>
		{
			Assert.That(initialImageRect.Width, Is.GreaterThan(0));
			Assert.That(initialImageRect.Height, Is.GreaterThan(0));
		});

		var initialSource = App.WaitForElement("SourceState").GetText();
		if (initialSource is null)
		{
			Assert.Fail("The initial image source state was not exposed.");
		}

		Assert.That(initialSource, Is.EqualTo("shopping_cart.png"));

		App.Tap("SwapButton");

		Assert.That(
			App.WaitForTextToBePresentInElement("SourceState", "dotnet_bot.png", TimeSpan.FromSeconds(10)),
			Is.True,
			"The image source did not transition to dotnet_bot.png.");

		App.RetryAssert(() =>
		{
			var generationText = App.WaitForElement("ImageLoadGeneration").GetText();
			if (generationText is null)
			{
				Assert.Fail("The replacement image load generation was not exposed.");
			}

			Assert.That(int.TryParse(generationText, out var generation), Is.True);
			Assert.That(generation, Is.GreaterThan(-1));
		}, timeout: TimeSpan.FromSeconds(10));

		var finalSource = App.WaitForElement("SourceState").GetText();
		if (finalSource is null)
		{
			Assert.Fail("The final image source state was not exposed.");
		}

		Assert.That(finalSource, Is.EqualTo("dotnet_bot.png"));

		var finalImageRect = App.WaitForElement("AffectedImage").GetRect();
		Assert.Multiple(() =>
		{
			Assert.That(finalImageRect.X, Is.GreaterThanOrEqualTo(0));
			Assert.That(finalImageRect.Y, Is.GreaterThanOrEqualTo(0));
			Assert.That(finalImageRect.Width, Is.GreaterThan(0));
			Assert.That(finalImageRect.Height, Is.GreaterThan(0));
		});

		var imageFitsIntrinsicSize =
			finalImageRect.Width <= ExpectedWidth + GeometryTolerance &&
			finalImageRect.Height <= ExpectedHeight + GeometryTolerance;

		Assert.That(
			imageFitsIntrinsicSize,
			Is.True,
			$"Issue26094 swapped image native frame exceeded its 168x208 intrinsic size; observed {finalImageRect.Width}x{finalImageRect.Height}, expected at most {ExpectedWidth}x{ExpectedHeight}.");
	}
}
#endif
