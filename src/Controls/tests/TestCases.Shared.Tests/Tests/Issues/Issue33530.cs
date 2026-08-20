#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue33530 : _IssuesUITest
{
	public override string Issue => "Border with Rotation and HorizontalOptions.Start is positioned incorrectly on initial load";

	public Issue33530(TestDevice device) : base(device) { }

	[Test]
	[Category(UITestCategories.Border)]
	public void InitiallyRotatedStartBorderUsesTransformedVisualEdge()
	{
		App.SetOrientationPortrait();
		var launchRect = App.WaitForElement("LaunchGrid").GetRect();
		Assert.That(launchRect.Height, Is.GreaterThan(launchRect.Width),
			"The Issue33530 scenario must run in portrait orientation.");

		App.Tap("OpenInitialButton");
		WaitForDescendants(
			"InitialBorder",
			"InitialColorBand",
			"InitialEdgeLabel",
			"InitialStatus",
			"InitialActionButton");
		var initial = WaitForInitialLayout();

		var initialRect = App.FindElement("InitialBorder").GetRect();
		Assert.That(initialRect.Width, Is.GreaterThan(0), "The affected Border must be rendered.");
		Assert.That(initialRect.Height, Is.GreaterThan(0), "The affected Border must be rendered.");

		App.Tap("InitialActionButton");
		initial = WaitForStatusTransition();

		Assert.That(initial.Edge, Is.EqualTo("ALIGNED"),
			$"Issue33530 initial rotated Border edge was not aligned: edge={initial.Edge}, rect={Format(initialRect)}.");
	}

	void WaitForDescendants(params string[] automationIds)
	{
		foreach (var automationId in automationIds)
		{
			App.WaitForElement(automationId);
		}
	}

	(int Generation, string Edge) WaitForInitialLayout()
	{
		App.RetryAssert(() =>
		{
			var status = ParseStatus();
			Assert.That(status.Generation, Is.GreaterThanOrEqualTo(0),
				"INITIAL layout callback did not occur after modal attachment.");
			Assert.That(status.Edge, Is.EqualTo("UNCHECKED"),
				"The edge result must remain at its sentinel until the user action.");
		});
		return ParseStatus();
	}

	(int Generation, string Edge) WaitForStatusTransition()
	{
		App.RetryAssert(() =>
		{
			var status = ParseStatus();
			Assert.That(status.Edge, Is.Not.EqualTo("UNCHECKED"),
				"The edge hit test did not run after the user action.");
		});
		return ParseStatus();
	}

	(int Generation, string Edge) ParseStatus()
	{
		var text = App.FindElement("InitialStatus").GetText() ?? string.Empty;
		var parts = text.Split(';');
		Assert.That(parts, Has.Length.EqualTo(3), $"INITIAL status was malformed: '{text}'.");
		Assert.That(parts[0], Is.EqualTo("INITIAL"), "Status identified the wrong modal.");

		var generation = int.Parse(parts[1].Split('=')[1]);
		var edge = parts[2].Split('=')[1];
		return (generation, edge);
	}

	static string Format(System.Drawing.Rectangle rect) =>
		$"({rect.X},{rect.Y},{rect.Width},{rect.Height})";
}
#endif
