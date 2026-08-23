#if IOS
using ImageMagick;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue34560 : _IssuesUITest
{
	public Issue34560(TestDevice device)
		: base(device)
	{
	}

	public override string Issue => "Switch iOS Liquid glass rendering issue";

	[Test]
	[Category(UITestCategories.Switch)]
	public void ToggledOnSwitchRendersWithoutArtifacts()
	{
		if (App is not AppiumIOSApp iosApp || !HelperExtensions.IsIOS26OrHigher(iosApp))
			return;

		var switchElement = App.WaitForElement("SwitchUnderTest");
		var switchRect = switchElement.GetRect();
		var windowRect = App.WaitForElement(AppiumQuery.ByXPath("//XCUIElementTypeWindow")).GetRect();

		Assert.Multiple(() =>
		{
			Assert.That(switchRect.Width, Is.GreaterThan(0), "The native Switch must have a nonempty width.");
			Assert.That(switchRect.Height, Is.GreaterThan(0), "The native Switch must have a nonempty height.");
			Assert.That(switchElement.GetAttribute<string>("value"), Is.EqualTo("0"), "The Switch must begin in its default off state.");
			Assert.That(App.WaitForElement("CallbackLabel").GetText(), Is.EqualTo("Callback token: -1; native rendering equivalent: -1"), "No Toggled callback or rendering comparison may run before the tap.");
		});

		bool offPixelsSettled = TryCaptureStableAnalysis(switchRect, windowRect, out var offAnalysis);
		Assert.Multiple(() =>
		{
			Assert.That(offPixelsSettled, Is.True, "The off-state Switch pixels must settle before calibration.");
			Assert.That(offAnalysis.BackgroundVariation, Is.LessThanOrEqualTo(4), "The Switch pixel oracle requires a uniform page surface around the control.");
			Assert.That(offAnalysis.ForegroundPixels, Is.GreaterThan(0), "The off-state native Switch must render at its identified screen location.");
			Assert.That(offAnalysis.ComponentCount, Is.EqualTo(1), "The off-state native Switch must calibrate as one connected silhouette.");
			Assert.That(offAnalysis.ArtifactPixels, Is.EqualTo(0), "The off-state native Switch must calibrate with no pixels outside its clean capsule envelope.");
		});

		App.Tap("SwitchUnderTest");

		bool callbackObserved = App.WaitForTextToBePresentInElement(
			"CallbackLabel",
			"Callback token: 1",
			TimeSpan.FromSeconds(5));
		bool nativeRenderingEquivalent = App.WaitForTextToBePresentInElement(
			"CallbackLabel",
			"native rendering equivalent: 1",
			TimeSpan.FromSeconds(5));
		bool nativeOnObserved = false;
		App.WaitForElement(
			() =>
			{
				var candidate = App.FindElement("SwitchUnderTest");
				nativeOnObserved = IsNativeSwitchOn(candidate);
				return nativeOnObserved ? candidate : null;
			},
			"The native Switch did not expose its on accessibility value.",
			TimeSpan.FromSeconds(5));

		Assert.Multiple(() =>
		{
			Assert.That(callbackObserved, Is.True, "The real Appium tap must raise the MAUI Toggled callback.");
			Assert.That(nativeOnObserved, Is.True, "The real Appium tap must update the native accessibility value.");
		});
		Assert.That(
			nativeRenderingEquivalent,
			Is.True,
			"Issue34560 toggled-on Switch native track rendering differs from a platform-default UISwitch.");

		var toggledRect = App.WaitForElement("SwitchUnderTest").GetRect();
		Assert.Multiple(() =>
		{
			Assert.That(toggledRect.X, Is.EqualTo(switchRect.X).Within(0.5), "The same Switch must remain at the identified horizontal location.");
			Assert.That(toggledRect.Y, Is.EqualTo(switchRect.Y).Within(0.5), "The same Switch must remain at the identified vertical location.");
			Assert.That(toggledRect.Width, Is.EqualTo(switchRect.Width).Within(0.5), "The same Switch must retain its native width.");
			Assert.That(toggledRect.Height, Is.EqualTo(switchRect.Height).Within(0.5), "The same Switch must retain its native height.");
		});

		bool onPixelsSettled = TryCaptureStableAnalysis(toggledRect, windowRect, out var onAnalysis);
		Assert.That(onPixelsSettled, Is.True, "The toggled-on Switch pixels must settle after the native animation.");
		Assert.That(onAnalysis.ForegroundPixels, Is.GreaterThan(0), "The toggled-on native Switch must render at its identified screen location.");
		Assert.That(
			(onAnalysis.ArtifactPixels, onAnalysis.ComponentCount),
			Is.EqualTo((0, 1)),
			$"Issue34560 toggled-on Switch artifact pixels: measured artifact count {onAnalysis.ArtifactPixels}, component count {onAnalysis.ComponentCount}, rect {toggledRect}, screenshot scale {onAnalysis.Scale:F2}; expected zero artifact pixels and one component.");
	}

	static bool IsNativeSwitchOn(IUIElement element)
	{
		var value = element.GetAttribute<string>("value");
		return value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
	}

	bool TryCaptureStableAnalysis(System.Drawing.Rectangle switchRect, System.Drawing.Rectangle windowRect, out PixelAnalysis analysis)
	{
		const int maximumCaptures = 8;
		ulong previousHash = 0;
		bool hasPrevious = false;
		analysis = default;

		for (int capture = 0; capture < maximumCaptures; capture++)
		{
			using var image = new MagickImage(App.Screenshot());
			PixelAnalysis current = Analyze(image, switchRect, windowRect);
			if (hasPrevious && current.RegionHash == previousHash)
			{
				analysis = current;
				return true;
			}

			previousHash = current.RegionHash;
			hasPrevious = true;
			analysis = current;
		}

		return false;
	}

	static PixelAnalysis Analyze(MagickImage image, System.Drawing.Rectangle switchRect, System.Drawing.Rectangle windowRect)
	{
		double scaleX = (double)image.Width / windowRect.Width;
		double scaleY = (double)image.Height / windowRect.Height;
		double scale = (scaleX + scaleY) / 2;
		int left = (int)Math.Round((switchRect.X - windowRect.X) * scaleX);
		int top = (int)Math.Round((switchRect.Y - windowRect.Y) * scaleY);
		int right = (int)Math.Round((switchRect.X + switchRect.Width - windowRect.X) * scaleX);
		int bottom = (int)Math.Round((switchRect.Y + switchRect.Height - windowRect.Y) * scaleY);
		int sampleMargin = Math.Max(4, (int)Math.Round(8 * scale));

		Assert.That(left - sampleMargin, Is.GreaterThanOrEqualTo(0), "The Switch background sample must be within the screenshot.");
		Assert.That(top - sampleMargin, Is.GreaterThanOrEqualTo(0), "The Switch background sample must be within the screenshot.");
		Assert.That(right + sampleMargin, Is.LessThan((int)image.Width), "The Switch background sample must be within the screenshot.");
		Assert.That(bottom + sampleMargin, Is.LessThan((int)image.Height), "The Switch background sample must be within the screenshot.");

		byte[] pixels = image.ToByteArray(MagickFormat.Rgba);
		var redSamples = new List<byte>();
		var greenSamples = new List<byte>();
		var blueSamples = new List<byte>();
		for (int x = left - sampleMargin; x <= right + sampleMargin; x++)
		{
			AddSample(pixels, (int)image.Width, x, top - sampleMargin, redSamples, greenSamples, blueSamples);
			AddSample(pixels, (int)image.Width, x, bottom + sampleMargin, redSamples, greenSamples, blueSamples);
		}
		for (int y = top - sampleMargin + 1; y < bottom + sampleMargin; y++)
		{
			AddSample(pixels, (int)image.Width, left - sampleMargin, y, redSamples, greenSamples, blueSamples);
			AddSample(pixels, (int)image.Width, right + sampleMargin, y, redSamples, greenSamples, blueSamples);
		}

		byte backgroundRed = Median(redSamples);
		byte backgroundGreen = Median(greenSamples);
		byte backgroundBlue = Median(blueSamples);
		int backgroundVariation = Math.Max(
			MaximumVariation(redSamples, backgroundRed),
			Math.Max(MaximumVariation(greenSamples, backgroundGreen), MaximumVariation(blueSamples, backgroundBlue)));
		int roiLeft = left - sampleMargin / 2;
		int roiTop = top - sampleMargin / 2;
		int roiRight = right + sampleMargin / 2;
		int roiBottom = bottom + sampleMargin / 2;
		int roiWidth = roiRight - roiLeft + 1;
		int roiHeight = roiBottom - roiTop + 1;
		var foreground = new bool[roiWidth * roiHeight];
		int foregroundPixels = 0;
		int artifactPixels = 0;
		ulong hash = 14695981039346656037UL;
		double radius = (bottom - top) / 2.0;
		double tolerance = Math.Max(2, 1.5 * scale);
		double centerY = (top + bottom) / 2.0;
		double rightCenterX = right - radius;

		for (int y = roiTop; y <= roiBottom; y++)
		{
			for (int x = roiLeft; x <= roiRight; x++)
			{
				int pixelIndex = (y * (int)image.Width + x) * 4;
				hash = (hash ^ pixels[pixelIndex]) * 1099511628211UL;
				hash = (hash ^ pixels[pixelIndex + 1]) * 1099511628211UL;
				hash = (hash ^ pixels[pixelIndex + 2]) * 1099511628211UL;

				bool isForeground =
					Math.Abs(pixels[pixelIndex] - backgroundRed) > 12 ||
					Math.Abs(pixels[pixelIndex + 1] - backgroundGreen) > 12 ||
					Math.Abs(pixels[pixelIndex + 2] - backgroundBlue) > 12;
				if (!isForeground)
					continue;

				foreground[(y - roiTop) * roiWidth + x - roiLeft] = true;
				foregroundPixels++;

				if (x >= rightCenterX && Distance(x, y, rightCenterX, centerY) > radius + tolerance)
					artifactPixels++;
			}
		}

		int componentCount = CountSignificantComponents(foreground, roiWidth, roiHeight, Math.Max(2, (int)Math.Round(scale * scale)));
		return new PixelAnalysis(foregroundPixels, artifactPixels, componentCount, backgroundVariation, scale, hash);
	}

	static double Distance(double x, double y, double centerX, double centerY)
		=> Math.Sqrt(Math.Pow(x - centerX, 2) + Math.Pow(y - centerY, 2));

	static void AddSample(byte[] pixels, int width, int x, int y, List<byte> reds, List<byte> greens, List<byte> blues)
	{
		int index = (y * width + x) * 4;
		reds.Add(pixels[index]);
		greens.Add(pixels[index + 1]);
		blues.Add(pixels[index + 2]);
	}

	static byte Median(List<byte> values)
	{
		values.Sort();
		return values[values.Count / 2];
	}

	static int MaximumVariation(List<byte> values, byte median)
	{
		int maximum = 0;
		foreach (byte value in values)
			maximum = Math.Max(maximum, Math.Abs(value - median));

		return maximum;
	}

	static int CountSignificantComponents(bool[] foreground, int width, int height, int minimumArea)
	{
		var visited = new bool[foreground.Length];
		var queue = new int[foreground.Length];
		int componentCount = 0;

		for (int start = 0; start < foreground.Length; start++)
		{
			if (!foreground[start] || visited[start])
				continue;

			int head = 0;
			int tail = 0;
			int area = 0;
			queue[tail++] = start;
			visited[start] = true;

			while (head < tail)
			{
				int current = queue[head++];
				int currentX = current % width;
				int currentY = current / width;
				area++;

				for (int offsetY = -1; offsetY <= 1; offsetY++)
				{
					for (int offsetX = -1; offsetX <= 1; offsetX++)
					{
						int nextX = currentX + offsetX;
						int nextY = currentY + offsetY;
						if ((offsetX == 0 && offsetY == 0) || nextX < 0 || nextX >= width || nextY < 0 || nextY >= height)
							continue;

						int next = nextY * width + nextX;
						if (foreground[next] && !visited[next])
						{
							visited[next] = true;
							queue[tail++] = next;
						}
					}
				}
			}

			if (area >= minimumArea)
				componentCount++;
		}

		return componentCount;
	}

	readonly struct PixelAnalysis
	{
		public PixelAnalysis(int foregroundPixels, int artifactPixels, int componentCount, int backgroundVariation, double scale, ulong regionHash)
		{
			ForegroundPixels = foregroundPixels;
			ArtifactPixels = artifactPixels;
			ComponentCount = componentCount;
			BackgroundVariation = backgroundVariation;
			Scale = scale;
			RegionHash = regionHash;
		}

		public int ForegroundPixels { get; }
		public int ArtifactPixels { get; }
		public int ComponentCount { get; }
		public int BackgroundVariation { get; }
		public double Scale { get; }
		public ulong RegionHash { get; }
	}
}
#endif
