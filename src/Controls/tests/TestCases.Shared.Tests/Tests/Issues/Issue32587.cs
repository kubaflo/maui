#if WINDOWS
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue32587 : _IssuesUITest
{
	public Issue32587(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "ContentView inside CollectionView reports invalid bounds during gesture events";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void TappedDirectDataTemplateContentViewHasPositiveBounds()
	{
		const string expectedLabelText = "Tap this custom ContentView item";
		const string uncapturedStatus = "Callbacks=0; Width=NaN; Height=NaN";

		var referenceLabel = App.WaitForElement("Issue32587ReferenceLabel");
		Assert.That(referenceLabel, Is.Not.Null);
		Assert.That(referenceLabel!.GetText(), Is.EqualTo(expectedLabelText));

		var directLabel = App.WaitForElement("Issue32587DirectLabel");
		Assert.That(directLabel, Is.Not.Null);
		Assert.That(directLabel!.GetText(), Is.EqualTo(expectedLabelText));

		var referenceTarget = App.WaitForElement("Issue32587ReferenceTarget");
		Assert.That(referenceTarget, Is.Not.Null);
		var referenceRect = referenceTarget!.GetRect();
		Assert.Multiple(() =>
		{
			Assert.That(referenceRect.Width, Is.GreaterThan(0), "Wrapped reference ContentView native width must be positive");
			Assert.That(referenceRect.Height, Is.GreaterThan(0), "Wrapped reference ContentView native height must be positive");
		});

		var referenceStatusElement = App.WaitForElement("Issue32587ReferenceStatus");
		Assert.That(referenceStatusElement, Is.Not.Null);
		Assert.That(referenceStatusElement!.GetText(), Is.EqualTo(uncapturedStatus));

		App.Tap("Issue32587ReferenceTarget");
		App.RetryAssert(() =>
		{
			var statusElement = App.WaitForElement("Issue32587ReferenceStatus");
			Assert.That(statusElement, Is.Not.Null);
			Assert.That(statusElement!.GetText(), Does.StartWith("Callbacks=1;"));
		});
		App.RetryAssert(() =>
		{
			var statusElement = App.WaitForElement("Issue32587ReferenceStatus");
			Assert.That(statusElement, Is.Not.Null);
			Assert.That(statusElement!.GetText(), Does.Not.Contain("NaN"));
		});

		var capturedReferenceStatusElement = App.WaitForElement("Issue32587ReferenceStatus");
		Assert.That(capturedReferenceStatusElement, Is.Not.Null);
		var capturedReferenceStatus = capturedReferenceStatusElement!.GetText();
		Assert.That(capturedReferenceStatus, Is.Not.Null);
		var referenceWidth = ReadDimension(capturedReferenceStatus!, "Width");
		var referenceHeight = ReadDimension(capturedReferenceStatus!, "Height");
		Assert.Multiple(() =>
		{
			Assert.That(referenceWidth, Is.GreaterThan(0), "Tapped wrapped reference ContentView width must be positive");
			Assert.That(referenceHeight, Is.GreaterThan(0), "Tapped wrapped reference ContentView height must be positive");
		});

		var directTarget = App.WaitForElement("Issue32587DirectTarget");
		Assert.That(directTarget, Is.Not.Null);
		var directRect = directTarget!.GetRect();
		Assert.Multiple(() =>
		{
			Assert.That(directRect.Width, Is.GreaterThan(0), "Direct DataTemplate ContentView native width must be positive");
			Assert.That(directRect.Height, Is.GreaterThan(0), "Direct DataTemplate ContentView native height must be positive");
		});

		var directStatusElement = App.WaitForElement("Issue32587DirectStatus");
		Assert.That(directStatusElement, Is.Not.Null);
		Assert.That(directStatusElement!.GetText(), Is.EqualTo(uncapturedStatus));

		App.Tap("Issue32587DirectTarget");
		App.RetryAssert(() =>
		{
			var statusElement = App.WaitForElement("Issue32587DirectStatus");
			Assert.That(statusElement, Is.Not.Null);
			Assert.That(statusElement!.GetText(), Does.StartWith("Callbacks=1;"));
		});
		App.RetryAssert(() =>
		{
			var statusElement = App.WaitForElement("Issue32587DirectStatus");
			Assert.That(statusElement, Is.Not.Null);
			Assert.That(statusElement!.GetText(), Does.Not.Contain("NaN"));
		});

		var capturedDirectStatusElement = App.WaitForElement("Issue32587DirectStatus");
		Assert.That(capturedDirectStatusElement, Is.Not.Null);
		var capturedDirectStatus = capturedDirectStatusElement!.GetText();
		Assert.That(capturedDirectStatus, Is.Not.Null);
		var directWidth = ReadDimension(capturedDirectStatus!, "Width");
		var directHeight = ReadDimension(capturedDirectStatus!, "Height");

		Assert.Multiple(() =>
		{
			Assert.That(
				directWidth,
				Is.GreaterThan(0),
				$"Tapped direct DataTemplate ContentView bounds must be positive; managed Width={directWidth}, Height={directHeight}; native rectangle={directRect}");
			Assert.That(
				directHeight,
				Is.GreaterThan(0),
				$"Tapped direct DataTemplate ContentView bounds must be positive; managed Width={directWidth}, Height={directHeight}; native rectangle={directRect}");
		});
	}

	static double ReadDimension(string status, string dimension)
	{
		var marker = $"{dimension}=";
		var valueStart = status.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
		var valueEnd = status.IndexOf(';', valueStart);
		var value = valueEnd >= 0
			? status[valueStart..valueEnd]
			: status[valueStart..];

		return double.Parse(value, CultureInfo.InvariantCulture);
	}
}
#endif
