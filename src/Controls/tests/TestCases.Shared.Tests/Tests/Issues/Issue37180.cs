#if IOS
using System.Drawing;
using ImageMagick;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue37180 : _IssuesUITest
{
	public Issue37180(TestDevice device) : base(device)
	{
	}

	public override string Issue => "Label background does not reset when set to null";

	[Test]
	[Category(UITestCategories.Label)]
	public void LabelBackgroundReturnsToDefaultAfterBeingSetToNull()
	{
		var label = App.WaitForElement("BackgroundLabel");
		if (label is null)
			throw new InvalidOperationException("The background label was not found.");

		var window = App.WaitForElement(AppiumQuery.ByXPath("//XCUIElementTypeWindow"));
		if (window is null)
			throw new InvalidOperationException("The application window was not found.");

		var labelRect = label.GetRect();
		var windowRect = window.GetRect();
		var initialSample = CapturePaddingSample(labelRect, windowRect);

		App.Tap("SetRedButton");
		WaitForStatus("READY: red background applied");

		PixelSample redSample = default;
		App.RetryAssert(() =>
		{
			redSample = CapturePaddingSample(labelRect, windowRect);

			Assert.Multiple(() =>
			{
				Assert.That(initialSample.Redness, Is.LessThan(50), "The initial transparent Label padding should not render red.");
				Assert.That(redSample.Red, Is.GreaterThan(220), "The Label padding should render red after the red Background assignment.");
				Assert.That(redSample.Green, Is.LessThan(45), "The Label padding should render red after the red Background assignment.");
				Assert.That(redSample.Blue, Is.LessThan(45), "The Label padding should render red after the red Background assignment.");
				Assert.That(ColorDistance(redSample, initialSample), Is.GreaterThan(100), "The red assignment should visibly change the initially transparent Label padding.");
			});
		});

		App.Tap("SetNullButton");
		WaitForStatus("READY: null background applied");

		App.RetryAssert(() =>
		{
			var postNullSample = CapturePaddingSample(labelRect, windowRect);
			var distance = ColorDistance(postNullSample, initialSample);

			Assert.That(
				distance,
				Is.LessThanOrEqualTo(15),
				$"Issue37180: Label pixels remained red after Background was set to null. Initial={initialSample}; Red={redSample}; PostNull={postNullSample}");
		});
	}

	void WaitForStatus(string expected)
	{
		App.RetryAssert(() =>
		{
			var status = App.FindElement("ActionStatus");
			if (status is null)
				throw new InvalidOperationException("The action status label was not found.");

			Assert.That(status.GetText(), Is.EqualTo(expected));
		});
	}

	PixelSample CapturePaddingSample(Rectangle labelRect, Rectangle windowRect)
	{
		using var image = new MagickImage(App.Screenshot());
		var width = (int)image.Width;
		var height = (int)image.Height;
		var scaleX = width / (double)windowRect.Width;
		var scaleY = height / (double)windowRect.Height;
		var sampleX = (labelRect.Right - 5 - windowRect.Left) * scaleX;
		var sampleY = (labelRect.Top + (labelRect.Height / 2.0) - windowRect.Top) * scaleY;
		var radius = Math.Max(2, (int)Math.Round(2 * Math.Min(scaleX, scaleY)));

		var left = Math.Clamp((int)Math.Round(sampleX) - radius, 0, width - 1);
		var right = Math.Clamp((int)Math.Round(sampleX) + radius, 0, width - 1);
		var top = Math.Clamp((int)Math.Round(sampleY) - radius, 0, height - 1);
		var bottom = Math.Clamp((int)Math.Round(sampleY) + radius, 0, height - 1);
		image.Crop(new MagickGeometry(left, top, (uint)(right - left + 1), (uint)(bottom - top + 1)));
		image.ResetPage();
		var pixels = image.ToByteArray(MagickFormat.Rgb);
		double red = 0;
		double green = 0;
		double blue = 0;

		for (var offset = 0; offset < pixels.Length; offset += 3)
		{
			red += pixels[offset];
			green += pixels[offset + 1];
			blue += pixels[offset + 2];
		}

		var count = pixels.Length / 3;
		return new PixelSample(red / count, green / count, blue / count);
	}

	static double ColorDistance(PixelSample first, PixelSample second)
	{
		var red = first.Red - second.Red;
		var green = first.Green - second.Green;
		var blue = first.Blue - second.Blue;
		return Math.Sqrt((red * red) + (green * green) + (blue * blue));
	}

	readonly record struct PixelSample(double Red, double Green, double Blue)
	{
		public double Redness => Red - Math.Max(Green, Blue);

		public override string ToString() => $"rgb({Red:F1},{Green:F1},{Blue:F1})";
	}
}
#endif
