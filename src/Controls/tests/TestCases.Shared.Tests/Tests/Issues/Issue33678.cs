#if ANDROID
using System.Drawing;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue33678 : _IssuesUITest
{
	public Issue33678(TestDevice device) : base(device) { }

	public override string Issue => "Edge to edge on Android doesn't work with Shell Navigation bar";

	[Test]
	[Category(UITestCategories.Shell)]
	public void TransparentShellNavigationBarDoesNotReserveContentSpace()
	{
		const int tolerance = 2;

		App.SetOrientationPortrait();
		App.WaitForElement("Issue33678Launch");
		App.Tap("Issue33678Launch");

		var hiddenMarker = WaitForMarker("Issue33678HiddenLayout", "HiddenLayout:-1");
		var hiddenMarkerText = hiddenMarker.GetText()
			?? throw new InvalidOperationException("The hidden layout marker did not expose text.");
		var hiddenSequence = ParseSequence(hiddenMarkerText, "HiddenLayout:");
		Assert.That(hiddenSequence, Is.GreaterThan(-1), "The hidden navigation-bar layout callback did not occur.");

		var bandText = App.WaitForElement("Issue33678BandText");
		Assert.That(bandText.GetText(), Is.EqualTo("EDGE-TO-EDGE CONTENT START"));

		var hiddenBand = WaitForStableRect("Issue33678TopBand", tolerance);
		var hiddenRoot = WaitForStableRect("Issue33678ContentRoot", tolerance);
		var densityDpi = (long?)((AppiumApp)App).Driver.Capabilities.GetCapability("deviceScreenDensity")
			?? throw new InvalidOperationException("deviceScreenDensity capability is missing or null.");
		Assert.That(densityDpi, Is.GreaterThan(0), "The active Android display density must be available.");
		var expectedBandHeight = 170d * densityDpi / 160d;

		Assert.Multiple(() =>
		{
			Assert.That(hiddenRoot.Height, Is.GreaterThan(0), "The edge-to-edge content root must be visible.");
			Assert.That(hiddenRoot.Height, Is.GreaterThan(hiddenRoot.Width), "The scenario must be in portrait orientation.");
			Assert.That(hiddenBand.Width, Is.EqualTo(hiddenRoot.Width).Within(tolerance), "The identified top band must span the content root.");
			Assert.That(hiddenBand.Height, Is.EqualTo(expectedBandHeight).Within(tolerance), "The identified top band must retain its issue-derived 170-DIP height.");
			Assert.That(hiddenBand.X, Is.EqualTo(hiddenRoot.X).Within(tolerance), "The identified top band must align with the content root.");
			Assert.That(hiddenBand.Y, Is.GreaterThanOrEqualTo(hiddenRoot.Y), "The identified top band must be inside the content root.");
		});

		App.Tap("Issue33678ShowNavBar");
		App.WaitForElement("Edge-to-edge");

		var visibleMarker = WaitForMarker("Issue33678VisibleLayout", "VisibleLayout:-1");
		var visibleMarkerText = visibleMarker.GetText()
			?? throw new InvalidOperationException("The visible layout marker did not expose text.");
		var visibleSequence = ParseSequence(visibleMarkerText, "VisibleLayout:");
		Assert.That(visibleSequence, Is.GreaterThan(hiddenSequence), "The visible navigation-bar layout callback must occur after the hidden state.");

		var visibleBand = WaitForStableRect("Issue33678TopBand", tolerance);
		var visibleRoot = WaitForStableRect("Issue33678ContentRoot", tolerance);
		Assert.That(App.WaitForElement("Issue33678BandText").GetText(), Is.EqualTo("EDGE-TO-EDGE CONTENT START"));

		Assert.Multiple(() =>
		{
			Assert.That(visibleBand.Width, Is.EqualTo(visibleRoot.Width).Within(tolerance), "The identified top band must still span the content root.");
			Assert.That(visibleBand.Height, Is.EqualTo(expectedBandHeight).Within(tolerance), "The identified top band must still have its issue-derived 170-DIP height.");
			Assert.That(visibleBand.X, Is.EqualTo(visibleRoot.X).Within(tolerance), "The identified top band must still align with the content root.");
			Assert.That(visibleBand.Y, Is.GreaterThanOrEqualTo(visibleRoot.Y), "The identified top band must remain inside the content root.");
			Assert.That(visibleBand.Y, Is.EqualTo(hiddenBand.Y).Within(tolerance),
				$"Transparent Shell navigation bar moved edge-to-edge content: hidden={hiddenBand}, visible={visibleBand}, densityDpi={densityDpi}, tolerance={tolerance}.");
			Assert.That(visibleRoot.Height, Is.EqualTo(hiddenRoot.Height).Within(tolerance),
				$"Transparent Shell navigation bar moved edge-to-edge content: hidden={hiddenRoot}, visible={visibleRoot}, densityDpi={densityDpi}, tolerance={tolerance}.");
		});
	}

	IUIElement WaitForMarker(string automationId, string sentinel)
	{
		return App.WaitForElement(() =>
		{
			var marker = App.FindElement(automationId);
			if (marker is null)
			{
				return null;
			}

			var markerText = marker.GetText();
			return markerText is not null && markerText != sentinel ? marker : null;
		});
	}

	Rectangle WaitForStableRect(string automationId, int tolerance)
	{
		var previous = App.WaitForElement(automationId).GetRect();
		var stableSamples = 0;
		return App.WaitForElement(() =>
		{
			var element = App.FindElement(automationId);
			if (element is null)
			{
				return null;
			}

			var current = element.GetRect();
			var stable = Math.Abs(current.X - previous.X) <= tolerance
				&& Math.Abs(current.Y - previous.Y) <= tolerance
				&& Math.Abs(current.Width - previous.Width) <= tolerance
				&& Math.Abs(current.Height - previous.Height) <= tolerance;
			stableSamples = stable ? stableSamples + 1 : 0;
			previous = current;
			return stableSamples >= 2 ? element : null;
		}).GetRect();
	}

	static int ParseSequence(string marker, string prefix)
	{
		Assert.That(marker, Does.StartWith(prefix));
		return int.Parse(marker[prefix.Length..], System.Globalization.CultureInfo.InvariantCulture);
	}
}
#endif
