#if IOS
using System.Drawing;
using System.Globalization;
using NUnit.Framework;
using OpenQA.Selenium.Appium;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue27664 : _IssuesUITest
{
	const double FrameTolerance = 2;
	const string EnteredText = "First editor segment remains visible while typing. Second editor segment makes the content wrap across several visual lines. Final marker 27664";

	public override string Issue => "Editor does not resize when the iOS keyboard appears";

	public Issue27664(TestDevice device) : base(device) { }

	[Test]
	[Category(UITestCategories.Editor)]
	public void EditorResizesAboveKeyboard()
	{
		Assert.That(App, Is.InstanceOf<AppiumApp>());
		var app = (AppiumApp)App;

		app.SetOrientationPortrait();
		Assert.That(app.GetOrientation(), Is.EqualTo(OpenQA.Selenium.ScreenOrientation.Portrait));
		Assert.That(app.IsKeyboardShown(), Is.False, "The keyboard must be hidden before measuring the Editor.");

		var windowSize = app.Driver.Manage().Window.Size;
		var windowFrame = new Rectangle(0, 0, windowSize.Width, windowSize.Height);
		AssertPositiveFrame(windowFrame, "Window");
		Assert.That(windowFrame.Height, Is.GreaterThan(windowFrame.Width), "The app window must be in portrait orientation.");

		var gridElement = app.WaitForElement("IssueGrid");
		var buttonElement = app.WaitForElement("CheckResizeButton");
		var editorElement = app.WaitForElement("IssueEditor");
		var nativeHeightElement = app.WaitForElement("NativeHeightTracker");
		Assert.That(gridElement, Is.Not.Null);
		Assert.That(buttonElement, Is.Not.Null);
		Assert.That(editorElement, Is.Not.Null);
		Assert.That(nativeHeightElement, Is.Not.Null);

		var gridBefore = gridElement.GetRect();
		var buttonBefore = buttonElement.GetRect();
		var editorBefore = editorElement.GetRect();
		AssertPositiveFrame(gridBefore, "Grid");
		AssertPositiveFrame(buttonBefore, "Button");
		AssertPositiveFrame(editorBefore, "Editor");
		Assert.That(gridBefore.Right, Is.LessThanOrEqualTo(windowFrame.Right));
		Assert.That(gridBefore.Bottom, Is.LessThanOrEqualTo(windowFrame.Bottom));
		Assert.That(editorBefore.Y, Is.GreaterThanOrEqualTo(buttonBefore.Bottom), "The Editor must occupy the Grid's second row.");
		Assert.That(editorBefore.Left, Is.GreaterThanOrEqualTo(gridBefore.Left));
		Assert.That(editorBefore.Right, Is.LessThanOrEqualTo(gridBefore.Right));
		var bottomInsetBefore = gridBefore.Bottom - editorBefore.Bottom;
		Assert.That(bottomInsetBefore, Is.GreaterThanOrEqualTo(16 - FrameTolerance),
			"The keyboard-hidden Editor must preserve at least the Grid's 16-point bottom padding.");
		app.Tap("IssueEditor");
		app.RetryAssert(() =>
		{
			Assert.That(app.IsFocused("IssueEditor"), Is.True, "The Editor must be focused after the tap.");
		});

		double nativeHeightBefore = -1;
		app.RetryAssert(() =>
		{
			var nativeHeightTextBefore = app.WaitForElement("NativeHeightTracker").GetText();
			if (nativeHeightTextBefore is null)
				throw new AssertionException("The native height tracker must expose its pre-trigger measurements.");

			var (capturedNativeHeightBefore, nativeHeightAfterBeforeTrigger) = ParseNativeHeights(nativeHeightTextBefore);
			Assert.That(capturedNativeHeightBefore, Is.GreaterThan(0), "The keyboard-hidden native Editor height must be captured.");
			Assert.That(nativeHeightAfterBeforeTrigger, Is.EqualTo(-1), "The post-trigger native Editor height must start at its sentinel.");
			nativeHeightBefore = capturedNativeHeightBefore;
		});

		app.EnterText("IssueEditor", EnteredText);

		app.RetryAssert(() =>
		{
			var exposedText = app.WaitForElement("IssueEditor").GetText();
			Assert.That(exposedText, Is.Not.Null);
			Assert.That(exposedText, Does.Contain("Final marker 27664"));
		});
		app.RetryAssert(() =>
		{
			Assert.That(app.IsKeyboardShown(), Is.True, "The iOS software keyboard must be shown.");
		});
		app.Tap("CheckResizeButton");
		app.RetryAssert(() =>
		{
			Assert.That(app.IsKeyboardShown(), Is.True, "The keyboard must remain shown while the native Editor frame is captured.");
		});

		var keyboardFrame = new Rectangle(-1, -1, -1, -1);
		app.RetryAssert(() =>
		{
			var keyboard = app.Driver.FindElement(MobileBy.ClassName("UIAKeyboard"));
			Assert.That(keyboard, Is.Not.Null);
			keyboardFrame = new Rectangle(keyboard.Location, keyboard.Size);
			AssertPositiveFrame(keyboardFrame, "Keyboard");
		});

		var editorAfter = new Rectangle(-1, -1, -1, -1);
		var gridAfter = new Rectangle(-1, -1, -1, -1);
		app.RetryAssert(() =>
		{
			var currentGrid = app.WaitForElement("IssueGrid");
			var currentEditor = app.WaitForElement("IssueEditor");
			Assert.That(currentGrid, Is.Not.Null);
			Assert.That(currentEditor, Is.Not.Null);

			gridAfter = currentGrid.GetRect();
			editorAfter = currentEditor.GetRect();
			AssertPositiveFrame(gridAfter, "Grid");
			AssertPositiveFrame(editorAfter, "Editor");
			Assert.That(gridAfter.Right, Is.LessThanOrEqualTo(windowFrame.Right));
			Assert.That(gridAfter.Bottom, Is.LessThanOrEqualTo(windowFrame.Bottom));
			Assert.That(editorAfter.Y, Is.GreaterThanOrEqualTo(buttonBefore.Bottom), "The Editor must remain in the Grid's second row.");
			Assert.That(editorAfter.Left, Is.GreaterThanOrEqualTo(gridAfter.Left));
			Assert.That(editorAfter.Right, Is.LessThanOrEqualTo(gridAfter.Right));

			var nativeHeightTextAfter = app.WaitForElement("NativeHeightTracker").GetText();
			if (nativeHeightTextAfter is null)
				throw new AssertionException("The native height tracker must expose its post-trigger measurements.");

			var (capturedNativeHeightBefore, nativeHeightAfter) = ParseNativeHeights(nativeHeightTextAfter);
			Assert.That(capturedNativeHeightBefore, Is.EqualTo(nativeHeightBefore).Within(FrameTolerance));
			Assert.That(nativeHeightAfter, Is.GreaterThan(0), "The check action must capture the post-trigger native Editor height.");

			var expectedBottom = Math.Min(editorBefore.Bottom, keyboardFrame.Top - 16);
			var expectedHeight = expectedBottom - editorAfter.Y;
			var nativeBottom = editorAfter.Y + nativeHeightAfter;
			var overlap = nativeBottom - keyboardFrame.Top;
			Assert.That(expectedHeight, Is.GreaterThan(0), "The keyboard-aware Grid row must have positive available height.");
			Assert.That(nativeHeightAfter, Is.EqualTo(expectedHeight).Within(FrameTolerance),
				$"Issue27664 Editor keyboard resize mismatch: measured={nativeHeightAfter}, expected={expectedHeight}, overlap={overlap}, tolerance={FrameTolerance}, Editor={editorAfter}, Keyboard={keyboardFrame}, Grid={gridAfter}, Window={windowFrame}");
		});
	}

	static (double Before, double After) ParseNativeHeights(string text)
	{
		Assert.That(text, Is.Not.Null);
		var values = text.Split('|', StringSplitOptions.TrimEntries);
		Assert.That(values, Has.Length.EqualTo(2), $"Native height tracker must contain two values, but was '{text}'.");
		Assert.That(double.TryParse(values[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var before), Is.True,
			$"The pre-trigger native height was invalid: '{values[0]}'.");
		Assert.That(double.TryParse(values[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var after), Is.True,
			$"The post-trigger native height was invalid: '{values[1]}'.");
		return (before, after);
	}

	static void AssertPositiveFrame(Rectangle frame, string name)
	{
		Assert.That(frame.Width, Is.GreaterThan(0), $"{name} width must be positive.");
		Assert.That(frame.Height, Is.GreaterThan(0), $"{name} height must be positive.");
		Assert.That(frame.X, Is.GreaterThanOrEqualTo(0), $"{name} X must be on-screen.");
		Assert.That(frame.Y, Is.GreaterThanOrEqualTo(0), $"{name} Y must be on-screen.");
	}
}
#endif
