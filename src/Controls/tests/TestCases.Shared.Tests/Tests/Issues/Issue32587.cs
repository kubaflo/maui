#if WINDOWS
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue32587 : _IssuesUITest
{
	public Issue32587(TestDevice device) : base(device) { }

	public override string Issue => "ContentView inside CollectionView reports invalid bounds during gesture events";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void ContentViewGestureBoundsStayPositive()
	{
		App.WaitForTextToBePresentInElement("ReadyState", "Direct ContentView is loaded");

		var contentView = App.WaitForElement("GestureBoundsView");
		if (contentView is null)
			throw new AssertionException("The directly templated ContentView was not found.");

		var nativeRect = contentView.GetRect();
		Assert.Multiple(() =>
		{
			Assert.That(nativeRect.Width, Is.GreaterThan(0), "The directly templated ContentView must have a positive native width.");
			Assert.That(nativeRect.Height, Is.GreaterThan(0), "The directly templated ContentView must have a positive native height.");
		});

		Assert.That(GetRequiredText("TapCount"), Is.EqualTo("Tap handler fired: 0"));
		Assert.That(GetRequiredText("TapBounds"), Is.EqualTo("Inside tap: not measured"));

		App.Tap("GestureBoundsView");
		App.WaitForTextToBePresentInElement("TapCount", "Tap handler fired: 1");
		Assert.That(GetRequiredText("TapCount"), Is.EqualTo("Tap handler fired: 1"));

		var tapBounds = ParseBounds(GetRequiredText("TapBounds"), "Inside tap");
		var failureMessage = string.Format(
			CultureInfo.InvariantCulture,
			"ContentView gesture bounds must stay positive after rendering; Width={0}, Height={1}",
			tapBounds.Width,
			tapBounds.Height);

		Assert.Multiple(() =>
		{
			Assert.That(tapBounds.Width, Is.GreaterThan(0.01d), failureMessage);
			Assert.That(tapBounds.Height, Is.GreaterThan(0.01d), failureMessage);
		});
	}

	string GetRequiredText(string automationId)
	{
		var element = App.WaitForElement(automationId);
		if (element is null)
			throw new AssertionException($"Element '{automationId}' was not found.");

		var text = element.GetText();
		if (text is null)
			throw new AssertionException($"Element '{automationId}' did not expose text.");

		return text;
	}

	static (double Width, double Height) ParseBounds(string text, string prefix)
	{
		var values = text
			.Replace($"{prefix} Width=", string.Empty, StringComparison.Ordinal)
			.Split(", Height=", StringSplitOptions.None);

		if (values.Length != 2)
			throw new AssertionException($"Bounds text '{text}' was not in the expected format.");

		return (
			double.Parse(values[0], CultureInfo.InvariantCulture),
			double.Parse(values[1], CultureInfo.InvariantCulture));
	}
}
#endif
