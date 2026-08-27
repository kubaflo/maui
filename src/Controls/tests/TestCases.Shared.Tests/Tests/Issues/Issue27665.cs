#if ANDROID
using NUnit.Framework;
using OpenQA.Selenium.Appium.Interactions;
using OpenQA.Selenium.Interactions;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue27665 : _IssuesUITest
{
	public Issue27665(TestDevice device) : base(device) { }

	public override string Issue => "Flickering when hiding or showing elements from ScrollView.Scrolled on Android";

	[Test]
	[Category(UITestCategories.ScrollView)]
	public void HeaderVisibilityChangesOnlyOnceDuringContinuousScroll()
	{
		if (App is not AppiumAndroidApp androidApp)
			throw new InvalidOperationException($"Invalid app type for Android test: {App}.");

		App.SetOrientationPortrait();
		Assert.That(
			App.WaitForTextToBePresentInElement("NativeState", "Ready=1", timeout: TimeSpan.FromSeconds(10)),
			Is.True,
			"The native visibility observer did not become ready.");

		var initialState = ReadState();
		Assert.That(initialState["EntryState"], Is.EqualTo("Visible"));
		Assert.That(initialState["ImageState"], Is.EqualTo("Visible"));
		Assert.That(ParseInt(initialState, "EntryTransitions"), Is.Zero);
		Assert.That(ParseInt(initialState, "ImageTransitions"), Is.Zero);
		Assert.That(ParseInt(initialState, "ImageLoaded"), Is.EqualTo(1));
		Assert.That(ParseInt(initialState, "ContentHeight"), Is.GreaterThan(ParseInt(initialState, "ViewportHeight")));

		var element = App.WaitForElement("Element4");
		var elementRect = element.GetRect();
		var scrollRect = App.WaitForElement("ScrollHost").GetRect();
		Assert.That(element.GetText(), Is.EqualTo("Elemento 4"));
		Assert.That(elementRect.CenterY(), Is.GreaterThan(scrollRect.Y).And.LessThan(scrollRect.Bottom));

		var windowHeight = androidApp.Driver.Manage().Window.Size.Height;
		int segment = (int)(windowHeight * 0.15);
		int startX = elementRect.CenterX();
		int startY = elementRect.CenterY();
		int middleY = startY - segment;
		int endY = middleY - segment;

		var touch = new OpenQA.Selenium.Appium.Interactions.PointerInputDevice(PointerKind.Touch);
		var drag = new ActionSequence(touch, 0);
		drag.AddAction(touch.CreatePointerMove(CoordinateOrigin.Viewport, startX, startY, TimeSpan.Zero));
		drag.AddAction(touch.CreatePointerDown(PointerButton.TouchContact));
		drag.AddAction(touch.CreatePointerMove(CoordinateOrigin.Viewport, startX, middleY, TimeSpan.FromMilliseconds(350)));
		drag.AddAction(touch.CreatePointerMove(CoordinateOrigin.Viewport, startX, endY, TimeSpan.FromMilliseconds(350)));
		drag.AddAction(touch.CreatePointerUp(PointerButton.TouchContact));
		androidApp.Driver.PerformActions([drag]);

		Assert.That(
			App.WaitForTextToBePresentInElement("NativeState", "PostTriggerDraw=1", timeout: TimeSpan.FromSeconds(10)),
			Is.True,
			"No native pre-draw occurred after the scroll callback.");

		var finalState = ReadState();
		Assert.That(ParseInt(finalState, "ScrollCallbacks"), Is.GreaterThan(0), "The gesture did not raise ScrollView.Scrolled.");
		Assert.That(ParseInt(finalState, "MaxOffset"), Is.GreaterThan(0), "The native ScrollView never reached a positive vertical offset.");

		int entryTransitions = ParseInt(finalState, "EntryTransitions");
		int imageTransitions = ParseInt(finalState, "ImageTransitions");
		string entryState = finalState["EntryState"];
		string imageState = finalState["ImageState"];

		Assert.That(
			entryTransitions == 1 && imageTransitions == 1 && entryState == "Gone" && imageState == "Gone",
			Is.True,
			"Header native visibility changed more than once during one downward drag.");
	}

	Dictionary<string, string> ReadState()
	{
		var stateText = App.FindElement("NativeState").GetText();
		if (stateText is null)
			throw new AssertionException("The native state element returned null text.");
		if (stateText.Length == 0)
			Assert.Fail("The native state element returned empty text.");

		return stateText.Split(';')
			.Select(part => part.Split('=', 2))
			.ToDictionary(parts => parts[0], parts => parts[1]);
	}

	static int ParseInt(Dictionary<string, string> state, string key) =>
		int.Parse(state[key], System.Globalization.CultureInfo.InvariantCulture);
}
#endif
