#if WINDOWS
using ImageMagick;
using NUnit.Framework;
using OpenQA.Selenium;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue33314 : _IssuesUITest
{
	const string ExpectedFailure = "Editor caret should remain a vertical insertion line after Shift clears text and hides the sibling ContentView; observed caret bounds";

	public Issue33314(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "Editor caret renders as a dot after clearing text and hiding a sibling";

	[Test]
	[Category(UITestCategories.Editor)]
	public void EditorCaretRemainsVerticalAfterTextAndSiblingAreCleared()
	{
		var editor = App.WaitForElement("IssueEditor");
		Assert.That(editor, Is.Not.Null);
		Assert.That(editor.GetText(), Is.Empty);

		editor.Click();

		var initialEditorBounds = editor.GetRect();
		var initialCaret = FindBlinkingComponent(initialEditorBounds);
		Assert.That(initialCaret.Height >= 8 && initialCaret.Height >= initialCaret.Width * 3, Is.True,
			$"The initially empty Editor should expose a vertical blinking caret, but observed {initialCaret}.");

		editor.SendKeys("Caret sample");
		App.WaitForTextToBePresentInElement("IssueEditor", "Caret sample");
		Assert.That(App.FindElements("CancelIndicator"), Is.Not.Empty);

		editor.SendKeys(Keys.Shift);
		App.WaitForTextToBePresentInElement("TriggerStatus", "Shift key received");

		editor = App.WaitForElement("IssueEditor");
		Assert.That(editor, Is.Not.Null);
		Assert.That(editor.GetText(), Is.Empty);
		Assert.That(App.FindElements("CancelIndicator"), Is.Empty);

		var finalEditorBounds = editor.GetRect();
		var finalCaret = FindBlinkingComponent(finalEditorBounds);
		var retainedVerticalGeometry =
			finalCaret.Height >= initialCaret.Height - 2 &&
			finalCaret.Width <= initialCaret.Width + 2 &&
			finalCaret.Height >= finalCaret.Width * 3;

		Assert.That(retainedVerticalGeometry, Is.True,
			$"{ExpectedFailure}: initial {initialCaret} in {initialEditorBounds}; post-trigger {finalCaret} in {finalEditorBounds}.");
	}

	PixelBounds FindBlinkingComponent(System.Drawing.Rectangle editorBounds)
	{
		using var previous = Capture();
		var best = PixelBounds.Empty;

		for (int frame = 0; frame < 24; frame++)
		{
			using var current = Capture();
			var candidate = FindBestDifference(previous, current, editorBounds);
			if (candidate.Height > best.Height)
				best = candidate;

			previous.Read(current.ToByteArray(MagickFormat.Png));
		}

		return best;
	}

	MagickImage Capture()
	{
		var image = new MagickImage(App.Screenshot());
		image.Depth = 8;
		return image;
	}

	static PixelBounds FindBestDifference(MagickImage first, MagickImage second, System.Drawing.Rectangle bounds)
	{
		if (first.Width != second.Width || first.Height != second.Height)
			return PixelBounds.Empty;

		var firstPixels = first.ToByteArray(MagickFormat.Rgba);
		var secondPixels = second.ToByteArray(MagickFormat.Rgba);
		int imageWidth = checked((int)first.Width);
		int imageHeight = checked((int)first.Height);
		int left = Math.Clamp(bounds.Left + 2, 0, imageWidth);
		int top = Math.Clamp(bounds.Top + 2, 0, imageHeight);
		int right = Math.Clamp(bounds.Right - 2, left, imageWidth);
		int bottom = Math.Clamp(bounds.Bottom - 2, top, imageHeight);
		int searchRight = Math.Min(right, left + Math.Max(32, bounds.Width / 4));
		int searchWidth = searchRight - left;
		int searchHeight = bottom - top;
		var changed = new bool[searchWidth * searchHeight];

		for (int y = 0; y < searchHeight; y++)
		{
			for (int x = 0; x < searchWidth; x++)
			{
				int pixel = ((top + y) * imageWidth + left + x) * 4;
				int difference =
					Math.Abs(firstPixels[pixel] - secondPixels[pixel]) +
					Math.Abs(firstPixels[pixel + 1] - secondPixels[pixel + 1]) +
					Math.Abs(firstPixels[pixel + 2] - secondPixels[pixel + 2]);
				changed[y * searchWidth + x] = difference > 90;
			}
		}

		var visited = new bool[changed.Length];
		var best = PixelBounds.Empty;
		for (int y = 0; y < searchHeight; y++)
		{
			for (int x = 0; x < searchWidth; x++)
			{
				int start = y * searchWidth + x;
				if (!changed[start] || visited[start])
					continue;

				var component = MeasureComponent(changed, visited, searchWidth, searchHeight, x, y);
				if (component.Width <= 6 && component.Height > best.Height)
					best = new PixelBounds(component.X + left, component.Y + top, component.Width, component.Height);
			}
		}

		return best;
	}

	static PixelBounds MeasureComponent(bool[] changed, bool[] visited, int width, int height, int startX, int startY)
	{
		var pending = new Queue<(int X, int Y)>();
		pending.Enqueue((startX, startY));
		visited[startY * width + startX] = true;
		int minX = startX;
		int maxX = startX;
		int minY = startY;
		int maxY = startY;

		while (pending.Count > 0)
		{
			var point = pending.Dequeue();
			minX = Math.Min(minX, point.X);
			maxX = Math.Max(maxX, point.X);
			minY = Math.Min(minY, point.Y);
			maxY = Math.Max(maxY, point.Y);

			for (int offsetY = -1; offsetY <= 1; offsetY++)
			{
				for (int offsetX = -1; offsetX <= 1; offsetX++)
				{
					int x = point.X + offsetX;
					int y = point.Y + offsetY;
					if (x < 0 || x >= width || y < 0 || y >= height)
						continue;

					int index = y * width + x;
					if (!changed[index] || visited[index])
						continue;

					visited[index] = true;
					pending.Enqueue((x, y));
				}
			}
		}

		return new PixelBounds(minX, minY, maxX - minX + 1, maxY - minY + 1);
	}

	readonly record struct PixelBounds(int X, int Y, int Width, int Height)
	{
		public static PixelBounds Empty => new(0, 0, 0, 0);
	}
}
#endif
