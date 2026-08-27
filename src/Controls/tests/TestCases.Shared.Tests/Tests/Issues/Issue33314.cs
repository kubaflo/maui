#if WINDOWS
using System.Globalization;
using ImageMagick;
using NUnit.Framework;
using OpenQA.Selenium;
using UITest.Appium;
using UITest.Core;
using DrawingRectangle = System.Drawing.Rectangle;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue33314 : _IssuesUITest
{
	public Issue33314(TestDevice testDevice) : base(testDevice)
	{ }

	public override string Issue => "Editor caret renders as a dot after Shift clears text and hides adjacent content";

	[Test]
	[Category(UITestCategories.Editor)]
	public void CaretRemainsVerticalAfterShiftClearsEditor()
	{
		var editor = RequireElement("IssueEditor");
		var fontSize = ReadDouble("NativeFontSize");
		Assert.That(fontSize, Is.GreaterThan(0), "The attached native TextBox must report its default font size.");

		App.Tap("IssueEditor");
		Assert.That(App.WaitForTextToBePresentInElement("FocusStatus", "Focused"), Is.True,
			"The Editor must receive focus before its initial caret is inspected.");
		var editorFrame = editor.GetRect();
		var minimumCaretHeight = Math.Max(8, (int)Math.Floor(fontSize * 0.55));
		var initialCaret = CaptureCaret(editorFrame, DrawingRectangle.Empty);

		Assert.That(initialCaret.Height, Is.GreaterThanOrEqualTo(minimumCaretHeight),
			$"The initially focused empty Editor must expose a clean vertical caret. Measured {initialCaret.Width}x{initialCaret.Height}, minimum height {minimumCaretHeight}, frame {editorFrame}.");

		editor.SendKeys("caret");
		Assert.That(RequireElement("IssueEditor").GetText(), Is.EqualTo("caret"));
		RequireElement("CancelContent");
		var textSequenceBeforeShift = ReadInt("TextChangedSequence");
		var nativeSequenceBeforeShift = ReadInt("NativeKeyDownSequence");

		editor.SendKeys(Keys.Shift);

		App.WaitForNoElement("CancelContent");
		Assert.That(App.WaitForTextToBePresentInElement("PostClearLayoutStatus", "Complete"), Is.True,
			"The Editor must complete the relayout caused by collapsing the adjacent ContentView.");
		var clearedEditor = RequireElement("IssueEditor");
		Assert.That(clearedEditor.GetText(), Is.Empty, "Shift must clear the Editor text.");
		Assert.That(ReadInt("NativeKeyDownSequence"), Is.GreaterThan(nativeSequenceBeforeShift),
			"The native TextBox must receive the Shift KeyDown callback.");
		Assert.That(ReadInt("TextChangedSequence"), Is.GreaterThan(textSequenceBeforeShift),
			"Clearing must raise TextChanged before the adjacent ContentView collapses.");

		var currentFrame = clearedEditor.GetRect();
		var currentCaret = CaptureCaret(currentFrame, initialCaret);

		Assert.That(currentCaret.Height, Is.GreaterThanOrEqualTo(minimumCaretHeight),
			$"Issue33314 caret after Shift clear must remain a vertical insertion line. Measured component {currentCaret.Width}x{currentCaret.Height} at ({currentCaret.X},{currentCaret.Y}) in frame {currentFrame}; native-font-derived minimum height is {minimumCaretHeight}.");
	}

	IUIElement RequireElement(string automationId)
	{
		var element = App.WaitForElement(automationId);
		if (element is null)
			throw new AssertionException($"Required element '{automationId}' was not found.");

		return element;
	}

	int ReadInt(string automationId)
	{
		var text = RequireElement(automationId).GetText();
		if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
			throw new AssertionException($"Element '{automationId}' did not contain an integer value.");

		return value;
	}

	double ReadDouble(string automationId)
	{
		var text = RequireElement(automationId).GetText();
		if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
			throw new AssertionException($"Element '{automationId}' did not contain a numeric value.");

		return value;
	}

	DrawingRectangle CaptureCaret(DrawingRectangle frame, DrawingRectangle initialCaret)
	{
		var screenshot = App.Screenshot();
		if (screenshot is null || screenshot.Length == 0)
			throw new AssertionException("Appium returned no screenshot bytes for caret inspection.");

		return FindCaretComponent(screenshot, frame, initialCaret);
	}

	static DrawingRectangle FindCaretComponent(byte[] screenshot, DrawingRectangle frame, DrawingRectangle initialCaret)
	{
		using var image = new MagickImage(screenshot);
		using var pixels = image.GetPixels();

		var imageWidth = (int)image.Width;
		var imageHeight = (int)image.Height;
		var frameLeft = Math.Clamp(frame.X, 0, imageWidth - 1);
		var frameTop = Math.Clamp(frame.Y, 0, imageHeight - 1);
		var frameRight = Math.Clamp(frame.X + frame.Width, frameLeft + 1, imageWidth);
		var frameBottom = Math.Clamp(frame.Y + frame.Height, frameTop + 1, imageHeight);
		var insetY = Math.Max(4, (frameBottom - frameTop) / 10);
		var searchTop = Math.Min(frameTop + insetY, frameBottom - 1);
		var searchBottom = Math.Max(searchTop + 1, frameBottom - insetY);

		int searchLeft;
		int searchRight;
		if (initialCaret.IsEmpty)
		{
			searchLeft = Math.Min(frameLeft + 4, frameRight - 1);
			searchRight = Math.Min(frameRight, searchLeft + Math.Max(16, Math.Min(48, (frameRight - frameLeft) / 3)));
		}
		else
		{
			searchLeft = Math.Max(frameLeft + 2, initialCaret.X - 4);
			searchRight = Math.Min(frameRight, initialCaret.X + initialCaret.Width + 5);
		}

		var background = ReadColor(pixels,
			Math.Clamp(frameLeft + ((frameRight - frameLeft) * 3 / 4), frameLeft, frameRight - 1),
			Math.Clamp(frameTop + ((frameBottom - frameTop) / 2), frameTop, frameBottom - 1));
		var width = searchRight - searchLeft;
		var height = searchBottom - searchTop;
		var foreground = new bool[width, height];
		var visited = new bool[width, height];

		for (var x = 0; x < width; x++)
		{
			for (var y = 0; y < height; y++)
			{
				var color = ReadColor(pixels, searchLeft + x, searchTop + y);
				foreground[x, y] = ColorDistance(color, background) >= 35;
			}
		}

		var best = DrawingRectangle.Empty;
		for (var x = 0; x < width; x++)
		{
			for (var y = 0; y < height; y++)
			{
				if (!foreground[x, y] || visited[x, y])
					continue;

				var component = FloodFill(foreground, visited, x, y, searchLeft, searchTop);
				if (component.Width <= 7 && component.Height > best.Height)
					best = component;
			}
		}

		return best;
	}

	static (double Red, double Green, double Blue) ReadColor(IPixelCollection<byte> pixels, int x, int y)
	{
		var color = pixels.GetPixel(x, y).ToColor();
		if (color is null)
			throw new AssertionException($"Unable to read screenshot pixel at ({x},{y}).");

		return (color.R, color.G, color.B);
	}

	static double ColorDistance(
		(double Red, double Green, double Blue) first,
		(double Red, double Green, double Blue) second)
	{
		var red = first.Red - second.Red;
		var green = first.Green - second.Green;
		var blue = first.Blue - second.Blue;
		return Math.Sqrt((red * red) + (green * green) + (blue * blue));
	}

	static DrawingRectangle FloodFill(bool[,] foreground, bool[,] visited, int startX, int startY, int offsetX, int offsetY)
	{
		var queue = new Queue<(int X, int Y)>();
		queue.Enqueue((startX, startY));
		visited[startX, startY] = true;
		var minX = startX;
		var maxX = startX;
		var minY = startY;
		var maxY = startY;

		while (queue.Count > 0)
		{
			var point = queue.Dequeue();
			minX = Math.Min(minX, point.X);
			maxX = Math.Max(maxX, point.X);
			minY = Math.Min(minY, point.Y);
			maxY = Math.Max(maxY, point.Y);

			for (var x = Math.Max(0, point.X - 1); x <= Math.Min(foreground.GetLength(0) - 1, point.X + 1); x++)
			{
				for (var y = Math.Max(0, point.Y - 1); y <= Math.Min(foreground.GetLength(1) - 1, point.Y + 1); y++)
				{
					if (!foreground[x, y] || visited[x, y])
						continue;

					visited[x, y] = true;
					queue.Enqueue((x, y));
				}
			}
		}

		return new DrawingRectangle(offsetX + minX, offsetY + minY, maxX - minX + 1, maxY - minY + 1);
	}
}
#endif
