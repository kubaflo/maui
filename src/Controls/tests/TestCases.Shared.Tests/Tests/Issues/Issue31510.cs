#if WINDOWS
using System.Globalization;
using System.Linq;
using ImageMagick;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue31510 : _IssuesUITest
{
	const int SampleRadius = 3;
	const int SampleSpacing = 2;
	const int ColorTolerance = 3;

	public Issue31510(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "Shell Flyout and windowTitleBar Background Transparency Overlap on Windows";

	[Test]
	[Category(UITestCategories.Shell)]
	public void MatchingTransparentFlyoutAndTitleBarRenderAsOneLayer()
	{
		App.WaitForElement("Issue31510AttachmentStatus");
		var becameReady = App.WaitForTextToBePresentInElement(
			"Issue31510AttachmentStatus",
			"READY|",
			TimeSpan.FromSeconds(10));

		var statusElement = App.FindElements("Issue31510AttachmentStatus").FirstOrDefault();
		if (statusElement is null)
		{
			Assert.Fail("The attachment status element was not found.");
			return;
		}

		var status = statusElement.GetText();
		if (status is null)
		{
			Assert.Fail("The attachment status did not expose text.");
			return;
		}

		Assert.That(becameReady, Is.True, $"The Windows handler scene did not become ready. Status: {status}");

		var titleFrame = ParseFrame(status, "T");
		var paneFrame = ParseFrame(status, "P");
		var nativeReferenceFrame = ParseFrame(status, "M");
		var referenceElement = App.FindElements("Issue31510MicaReference").FirstOrDefault();
		if (referenceElement is null)
		{
			Assert.Fail("The Mica reference element was not found.");
			return;
		}

		var referenceRect = referenceElement.GetRect();
		var appiumReferenceFrame = new Frame(referenceRect.X, referenceRect.Y, referenceRect.Width, referenceRect.Height);
		AssertFrameHasArea(titleFrame, "title bar");
		AssertFrameHasArea(paneFrame, "flyout pane");
		AssertFrameHasArea(nativeReferenceFrame, "native Mica reference");
		AssertFrameHasArea(appiumReferenceFrame, "Appium Mica reference");

		using var screenshot = new MagickImage(App.Screenshot());
		var scaleX = appiumReferenceFrame.Width / nativeReferenceFrame.Width;
		var scaleY = appiumReferenceFrame.Height / nativeReferenceFrame.Height;
		Assert.That(scaleX, Is.GreaterThan(0));
		Assert.That(scaleY, Is.GreaterThan(0));

		var sceneRight = Math.Max(titleFrame.Right, paneFrame.Right) * scaleX;
		var sceneBottom = Math.Max(titleFrame.Bottom, paneFrame.Bottom) * scaleY;
		var screenshotUsesScreenCoordinates =
			screenshot.Width > sceneRight + 100 ||
			screenshot.Height > sceneBottom + 100;
		var offsetX = screenshotUsesScreenCoordinates
			? appiumReferenceFrame.X - (nativeReferenceFrame.X * scaleX)
			: 0;
		var offsetY = screenshotUsesScreenCoordinates
			? appiumReferenceFrame.Y - (nativeReferenceFrame.Y * scaleY)
			: 0;

		var renderedTitleFrame = Scale(titleFrame, scaleX, scaleY, offsetX, offsetY);
		var renderedPaneFrame = Scale(paneFrame, scaleX, scaleY, offsetX, offsetY);
		var renderedReferenceFrame = screenshotUsesScreenCoordinates
			? appiumReferenceFrame
			: Scale(nativeReferenceFrame, scaleX, scaleY, 0, 0);
		AssertFrameInImage(renderedTitleFrame, screenshot, "title bar");
		AssertFrameInImage(renderedPaneFrame, screenshot, "flyout pane");
		AssertFrameInImage(renderedReferenceFrame, screenshot, "Mica reference");

		var titleBarBand = new Frame(
			renderedPaneFrame.Left,
			renderedTitleFrame.Top,
			renderedPaneFrame.Width,
			renderedTitleFrame.Height);
		var overlapFrame = Frame.Intersect(titleBarBand, renderedPaneFrame);
		AssertFrameHasArea(overlapFrame, "title-bar/flyout overlap");

		var nonOverlapLeft = Math.Max(renderedTitleFrame.Left, renderedPaneFrame.Right + 16);
		var nonOverlapRight = renderedTitleFrame.Right - 16;
		Assert.That(nonOverlapRight - nonOverlapLeft, Is.GreaterThan(40),
			$"The title-bar-only region was not available. Title={renderedTitleFrame}, Pane={renderedPaneFrame}");

		var titleOnly = SampleAverage(
			screenshot,
			(nonOverlapLeft + nonOverlapRight) / 2,
			renderedTitleFrame.CenterY,
			"title-bar-only region");

		var overlap = SampleAverage(
			screenshot,
			overlapFrame.Right - (SampleRadius * SampleSpacing) - 2,
			overlapFrame.CenterY,
			"title-bar/flyout overlap");
		Assert.That(overlap.MaximumDifference(titleOnly), Is.LessThanOrEqualTo(ColorTolerance),
			$"Issue31510 overlap rendered opaque: overlap={overlap}, titleOnly={titleOnly}, tolerance={ColorTolerance}, title={renderedTitleFrame}, pane={renderedPaneFrame}, overlapFrame={overlapFrame}");
	}

	static Frame ParseFrame(string status, string name)
	{
		var prefix = name + ":";
		foreach (var part in status.Split('|'))
		{
			if (!part.StartsWith(prefix, StringComparison.Ordinal))
				continue;

			var values = part[prefix.Length..].Split(',');
			Assert.That(values, Has.Length.EqualTo(4), $"Frame {name} was malformed: {part}");
			return new Frame(
				double.Parse(values[0], CultureInfo.InvariantCulture),
				double.Parse(values[1], CultureInfo.InvariantCulture),
				double.Parse(values[2], CultureInfo.InvariantCulture),
				double.Parse(values[3], CultureInfo.InvariantCulture));
		}

		Assert.Fail($"Frame {name} was missing from status: {status}");
		return default;
	}

	static Frame Scale(Frame frame, double scaleX, double scaleY, double offsetX, double offsetY) =>
		new(
			(frame.X * scaleX) + offsetX,
			(frame.Y * scaleY) + offsetY,
			frame.Width * scaleX,
			frame.Height * scaleY);

	static void AssertFrameHasArea(Frame frame, string name)
	{
		Assert.That(frame.Width, Is.GreaterThan(0), $"{name} width was not positive: {frame}");
		Assert.That(frame.Height, Is.GreaterThan(0), $"{name} height was not positive: {frame}");
	}

	static void AssertFrameInImage(Frame frame, MagickImage image, string name)
	{
		var margin = SampleRadius * SampleSpacing;
		Assert.That(frame.Left + margin, Is.GreaterThanOrEqualTo(0), $"{name} starts outside the screenshot: {frame}");
		Assert.That(frame.Top + margin, Is.GreaterThanOrEqualTo(0), $"{name} starts outside the screenshot: {frame}");
		Assert.That(frame.Right - margin, Is.LessThan(image.Width), $"{name} ends outside the screenshot: {frame}, Image={image.Width}x{image.Height}");
		Assert.That(frame.Bottom - margin, Is.LessThan(image.Height), $"{name} ends outside the screenshot: {frame}, Image={image.Width}x{image.Height}");
	}

	static Pixel SampleAverage(MagickImage image, double centerX, double centerY, string name)
	{
		var radius = SampleRadius * SampleSpacing;
		var left = (int)Math.Round(centerX) - radius;
		var top = (int)Math.Round(centerY) - radius;
		var size = (radius * 2) + 1;
		Assert.That(left, Is.GreaterThanOrEqualTo(0), $"{name} sample starts outside the screenshot.");
		Assert.That(top, Is.GreaterThanOrEqualTo(0), $"{name} sample starts outside the screenshot.");
		Assert.That(left + size, Is.LessThanOrEqualTo(image.Width), $"{name} sample ends outside the screenshot.");
		Assert.That(top + size, Is.LessThanOrEqualTo(image.Height), $"{name} sample ends outside the screenshot.");

		using var sample = image.Clone();
		sample.Crop(new MagickGeometry(left, top, (uint)size, (uint)size));
		var rgba = sample.ToByteArray(MagickFormat.Rgba);
		var red = 0;
		var green = 0;
		var blue = 0;
		var alpha = 0;
		for (var offset = 0; offset < rgba.Length; offset += 4)
		{
			red += rgba[offset];
			green += rgba[offset + 1];
			blue += rgba[offset + 2];
			alpha += rgba[offset + 3];
		}

		var count = rgba.Length / 4;
		return new Pixel(
			(byte)Math.Round(red / (double)count),
			(byte)Math.Round(green / (double)count),
			(byte)Math.Round(blue / (double)count),
			(byte)Math.Round(alpha / (double)count));
	}

	readonly record struct Frame(double X, double Y, double Width, double Height)
	{
		public double Left => X;
		public double Top => Y;
		public double Right => X + Width;
		public double Bottom => Y + Height;
		public double CenterX => X + (Width / 2);
		public double CenterY => Y + (Height / 2);

		public static Frame Intersect(Frame first, Frame second)
		{
			var left = Math.Max(first.Left, second.Left);
			var top = Math.Max(first.Top, second.Top);
			var right = Math.Min(first.Right, second.Right);
			var bottom = Math.Min(first.Bottom, second.Bottom);
			return new Frame(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
		}

		public override string ToString() =>
			FormattableString.Invariant($"({X:0.##},{Y:0.##},{Width:0.##},{Height:0.##})");
	}

	readonly record struct Pixel(byte R, byte G, byte B, byte A)
	{
		public int MaximumDifference(Pixel other) =>
			Math.Max(
				Math.Max(Math.Abs(R - other.R), Math.Abs(G - other.G)),
				Math.Max(Math.Abs(B - other.B), Math.Abs(A - other.A)));

		public override string ToString() => $"rgba({R},{G},{B},{A})";
	}

}
#endif
