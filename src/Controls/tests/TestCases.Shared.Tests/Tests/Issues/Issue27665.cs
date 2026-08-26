#if ANDROID
using System.Globalization;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OpenQA.Selenium.Appium.Interactions;
using OpenQA.Selenium.Interactions;
using UITest.Appium;
using UITest.Core;
using PointerInputDevice = OpenQA.Selenium.Appium.Interactions.PointerInputDevice;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue27665 : _IssuesUITest
{
	public Issue27665(TestDevice device) : base(device)
	{
	}

	public override string Issue => "Flickering when hiding or showing elements in the ScrollView Scrolled event on Android";

	[Test]
	[Category(UITestCategories.ScrollView)]
	public void HeaderRemainsNativelyHiddenDuringHeldPointerScroll()
	{
		App.SetOrientationPortrait();
		Assert.That(App, Is.TypeOf<AppiumAndroidApp>());
		var androidApp = (AppiumAndroidApp)App;

		App.RetryAssert(() =>
		{
			var windowSize = androidApp.Driver.Manage().Window.Size;
			Assert.That(windowSize.Height, Is.GreaterThan(windowSize.Width), "The Issue27665 test requires portrait orientation.");
		});

		var entry = App.WaitForElement("Issue27665Entry");
		var image = App.WaitForElement("Issue27665Image");
		var heading = App.WaitForElement("Issue27665Heading");
		var firstItem = App.WaitForElement("Issue27665Item1");
		var scrollRect = App.WaitForElement("Issue27665ScrollView").GetRect();
		App.RetryAssert(() =>
		{
			Assert.That(GetToken(GetDiagnosticState(), "imageHasContent"), Is.EqualTo("True"),
				"The registered dotnet_bot.png asset did not reach the native Image view.");
		});
		var initialState = GetDiagnosticState();

		Assert.Multiple(() =>
		{
			Assert.That(entry.GetRect().Height, Is.GreaterThan(0));
			Assert.That(image.GetRect().Height, Is.GreaterThan(0));
			Assert.That(heading.GetText(), Is.EqualTo("Element's list"));
			Assert.That(firstItem.GetText(), Is.EqualTo("Elemento 1"));
			Assert.That(firstItem.GetRect().Y, Is.GreaterThan(heading.GetRect().Y));
			Assert.That(GetToken(initialState, "item20Text"), Is.EqualTo("Elemento 20"));
			Assert.That(GetToken(initialState, "item20BelowHeading"), Is.EqualTo("True"));
			Assert.That(GetToken(initialState, "imageHasContent"), Is.EqualTo("True"));
			Assert.That(GetInt(initialState, "initialCallback"), Is.EqualTo(-1));
			Assert.That(GetInt(initialState, "initialEntryTransitions"), Is.EqualTo(-1));
			Assert.That(GetInt(initialState, "initialImageTransitions"), Is.EqualTo(-1));
			Assert.That(GetInt(initialState, "initialOffset"), Is.EqualTo(0));
			Assert.That(GetToken(initialState, "initialEntryVisibility"), Is.EqualTo("Visible"));
			Assert.That(GetToken(initialState, "initialImageVisibility"), Is.EqualTo("Visible"));
		});

		var windowFrame = androidApp.Driver.Manage().Window.Size;
		var x = scrollRect.CenterX();
		var startY = scrollRect.CenterY();
		var segmentLength = windowFrame.Height * 12 / 100;
		Assert.That(startY - (segmentLength * 4), Is.GreaterThan(0), "The held pointer path must remain inside the app window.");

		var touchDevice = new PointerInputDevice(PointerKind.Touch);
		var dragSequence = new ActionSequence(touchDevice, 0);
		dragSequence.AddAction(touchDevice.CreatePointerMove(CoordinateOrigin.Viewport, x, startY, TimeSpan.Zero));
		dragSequence.AddAction(touchDevice.CreatePointerDown(PointerButton.TouchContact));
		for (var segment = 1; segment <= 4; segment++)
		{
			dragSequence.AddAction(touchDevice.CreatePointerMove(
				CoordinateOrigin.Viewport,
				x,
				startY - (segmentLength * segment),
				TimeSpan.FromMilliseconds(150)));
		}
		dragSequence.AddAction(touchDevice.CreatePointerUp(PointerButton.TouchContact));
		androidApp.Driver.PerformActions([dragSequence]);

		App.RetryAssert(() =>
		{
			var callbackState = GetDiagnosticState();
			Assert.Multiple(() =>
			{
				Assert.That(GetInt(callbackState, "currentCallback"), Is.GreaterThan(-1), "A post-gesture Scrolled callback was not observed.");
				Assert.That(GetInt(callbackState, "maximumOffset"), Is.GreaterThan(0), "The native ScrollView never reached a positive offset.");
			});
		});

		var finalState = GetDiagnosticState();
		var entryTransitions = GetInt(finalState, "entryTransitions");
		var imageTransitions = GetInt(finalState, "imageTransitions");
		var finalOffset = GetInt(finalState, "currentOffset");
		var entryVisibility = GetToken(finalState, "currentEntryVisibility");
		var imageVisibility = GetToken(finalState, "currentImageVisibility");

		Assert.That(entryTransitions, Is.EqualTo(1),
			$"Issue27665 native header visibility changed: Entry transitions={entryTransitions}, visibility={entryVisibility}, offset={finalOffset}.");
		Assert.That(imageTransitions, Is.EqualTo(1),
			$"Issue27665 native header visibility changed: Image transitions={imageTransitions}, visibility={imageVisibility}, offset={finalOffset}.");
		Assert.That(entryVisibility, Is.EqualTo("Gone"), "The native Entry should remain hidden after scrolling away from the top.");
		Assert.That(imageVisibility, Is.EqualTo("Gone"), "The native Image should remain hidden after scrolling away from the top.");
		Assert.That(finalOffset, Is.GreaterThan(0), "The native ScrollView should remain away from the top.");
		Assert.That(GetToken(finalState, "currentEntryId"), Is.EqualTo(GetToken(initialState, "initialEntryId")));
		Assert.That(GetToken(finalState, "currentImageId"), Is.EqualTo(GetToken(initialState, "initialImageId")));
	}

	string GetDiagnosticState()
	{
		var scrollView = App.WaitForElement("Issue27665ScrollView");
		var state = scrollView.GetAttribute<string>("content-desc");
		if (state is null)
			throw new AssertionException("Issue27665 ScrollView diagnostic description was null.");

		return state;
	}

	static int GetInt(string state, string key) =>
		int.Parse(GetToken(state, key), NumberStyles.Integer, CultureInfo.InvariantCulture);

	static string GetToken(string state, string key)
	{
		var match = Regex.Match(state, $@"(?:^|\|){Regex.Escape(key)}=([^|]+)");
		Assert.That(match.Success, Is.True, $"Issue27665 diagnostic key '{key}' was missing from '{state}'.");
		return match.Groups[1].Value;
	}
}
#endif
