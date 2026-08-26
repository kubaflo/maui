#if ANDROID
using System.Globalization;
using ImageMagick;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue37306 : _IssuesUITest
{
	const string ScrollViewId = "Issue37306ScrollView";

	public Issue37306(TestDevice device) : base(device) { }

	public override string Issue => "ScrollView clips content at the bottom safe-area inset while scrolling";

	[Test]
	[Category(UITestCategories.ScrollView)]
	public void ScrolledContentDrawsThroughBottomSafeAreaInset()
	{
		App.SetOrientationPortrait();

		var initialDiagnostics = WaitForDiagnostics("ready");
		Assert.That(initialDiagnostics.FrameWidth, Is.GreaterThan(0), "The native ScrollView must have a nonempty frame.");
		Assert.That(initialDiagnostics.FrameHeight, Is.GreaterThan(0), "The native ScrollView must have a nonempty frame.");
		Assert.That(initialDiagnostics.ItemWidth, Is.GreaterThan(0), "Item 7 must have a nonempty native frame.");
		Assert.That(initialDiagnostics.ScrollOffset, Is.EqualTo(0), "The ScrollView must start at its resting offset.");
		Assert.That(initialDiagnostics.BottomInset, Is.GreaterThan(0),
			"The Android runner must supply a nonzero bottom system-bar inset.");
		Assert.That(initialDiagnostics.ItemHeight, Is.EqualTo(56 * initialDiagnostics.Density).Within(initialDiagnostics.Density * 2),
			"Item 7 must retain the reported 56-dip height.");

		using (var initialImage = new MagickImage(App.Screenshot()))
		{
			var itemSampleX = initialDiagnostics.ItemX + (int)Math.Round(8 * initialDiagnostics.Density);
			var itemSampleY = initialDiagnostics.ItemY + (initialDiagnostics.ItemHeight / 4);
			Assert.That(IsWhite(initialImage, itemSampleX, itemSampleY), Is.True,
				"Item 7 must be visibly rendered as the reported white Border before scrolling.");

			var amberSampleX = initialDiagnostics.FrameX + (int)Math.Round(8 * initialDiagnostics.Density);
			Assert.That(IsAmber(initialImage, amberSampleX, itemSampleY), Is.True,
				"The ScrollView's amber background must be visible beside Item 7.");
		}

		var itemRect = App.WaitForElement("Issue37306Item7").GetRect();
		var startX = itemRect.X + (itemRect.Width / 2);
		var startY = itemRect.Y + (itemRect.Height / 2);
		var endY = Math.Max(
			initialDiagnostics.FrameY + (int)Math.Round(80 * initialDiagnostics.Density),
			startY - (initialDiagnostics.FrameHeight * 0.56f));
		App.DragCoordinates(startX, startY, startX, endY);

		var scrolledDiagnostics = WaitForDiagnostics("scrolled");
		Assert.That(scrolledDiagnostics.ScrollOffset, Is.GreaterThan(0),
			"The touch drag must advance the native ScrollView to a positive offset.");
		Assert.That(scrolledDiagnostics.ItemY, Is.LessThan(initialDiagnostics.ItemY),
			"Item 7 must visibly move upward after the touch drag.");
		Assert.That(scrolledDiagnostics.BottomInset, Is.EqualTo(initialDiagnostics.BottomInset),
			"The runtime bottom inset must remain active after scrolling.");
		Assert.That(scrolledDiagnostics.PaddingBottom, Is.EqualTo(scrolledDiagnostics.BottomInset),
			"The normal handler path must apply the real bottom system inset as native ScrollView padding.");
		Assert.That(scrolledDiagnostics.ClipToPadding, Is.False,
			"Issue37306: scrolled content was clipped at the bottom safe-area inset padding");
	}

	Diagnostics WaitForDiagnostics(string state)
	{
		App.WaitForElement(AppiumQuery.ByXPath(
			$"//android.widget.ScrollView[starts-with(@content-desc,'{state}|')]"));

		var element = App.FindElement(ScrollViewId);
		Assert.That(element, Is.Not.Null, $"Element '{ScrollViewId}' must exist.");
		var diagnostics = element.GetAttribute<string>("contentDescription");
		if (diagnostics is null)
			throw new AssertionException($"Element '{ScrollViewId}' returned null diagnostics.");
		return ParseDiagnostics(diagnostics);
	}

	static Diagnostics ParseDiagnostics(string text)
	{
		var sections = text.Split('|');
		if (sections.Length != 8)
			throw new AssertionException($"Unexpected Issue37306 diagnostics: '{text}'.");

		var frame = ParseIntList(sections[1], "frame=", 4);
		var density = ParseDouble(sections[2], "density=");
		var inset = ParseInt(sections[3], "inset=");
		var offset = ParseInt(sections[4], "offset=");
		var paddingBottom = ParseInt(sections[5], "paddingBottom=");
		var clipToPadding = ParseBool(sections[6], "clipToPadding=");
		var item = ParseIntList(sections[7], "item7=", 4);
		return new Diagnostics(frame[0], frame[1], frame[2], frame[3], density, inset, offset, paddingBottom, clipToPadding,
			item[0], item[1], item[2], item[3]);
	}

	static int[] ParseIntList(string section, string prefix, int count)
	{
		if (!section.StartsWith(prefix, StringComparison.Ordinal))
			throw new AssertionException($"Expected diagnostics section '{prefix}'.");

		var values = section[prefix.Length..].Split(',');
		if (values.Length != count)
			throw new AssertionException($"Expected {count} values in diagnostics section '{section}'.");

		var result = new int[count];
		for (var i = 0; i < count; i++)
		{
			if (!int.TryParse(values[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out result[i]))
				throw new AssertionException($"Invalid integer in diagnostics section '{section}'.");
		}
		return result;
	}

	static int ParseInt(string section, string prefix)
	{
		if (!section.StartsWith(prefix, StringComparison.Ordinal) ||
			!int.TryParse(section[prefix.Length..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
		{
			throw new AssertionException($"Invalid diagnostics section '{section}'.");
		}
		return value;
	}

	static double ParseDouble(string section, string prefix)
	{
		if (!section.StartsWith(prefix, StringComparison.Ordinal) ||
			!double.TryParse(section[prefix.Length..], NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
		{
			throw new AssertionException($"Invalid diagnostics section '{section}'.");
		}
		return value;
	}

	static bool ParseBool(string section, string prefix)
	{
		if (!section.StartsWith(prefix, StringComparison.Ordinal) ||
			!bool.TryParse(section[prefix.Length..], out var value))
		{
			throw new AssertionException($"Invalid diagnostics section '{section}'.");
		}
		return value;
	}

	static bool IsWhite(MagickImage image, int x, int y)
	{
		EnsurePixelIsInBounds(image, x, y);
		using var pixels = image.GetPixels();
		var color = pixels.GetPixel(x, y).ToColor();
		if (color is null)
			throw new AssertionException($"Pixel ({x},{y}) did not return a color.");
		return color.R >= 240 && color.G >= 240 && color.B >= 240;
	}

	static bool IsAmber(MagickImage image, int x, int y)
	{
		EnsurePixelIsInBounds(image, x, y);
		using var pixels = image.GetPixels();
		var color = pixels.GetPixel(x, y).ToColor();
		if (color is null)
			throw new AssertionException($"Pixel ({x},{y}) did not return a color.");
		return color.R >= 230 && color.G >= 170 && color.G <= 235 && color.B <= 130;
	}

	static void EnsurePixelIsInBounds(MagickImage image, int x, int y)
	{
		if (x < 0 || y < 0 || x >= (int)image.Width || y >= (int)image.Height)
			throw new AssertionException($"Pixel ({x},{y}) is outside screenshot {image.Width}x{image.Height}.");
	}

	readonly record struct Diagnostics(
		int FrameX,
		int FrameY,
		int FrameWidth,
		int FrameHeight,
		double Density,
		int BottomInset,
		int ScrollOffset,
		int PaddingBottom,
		bool ClipToPadding,
		int ItemX,
		int ItemY,
		int ItemWidth,
		int ItemHeight);
}
#endif
