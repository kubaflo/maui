#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue36216 : _IssuesUITest
{
	public Issue36216(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "Accelerometer.ReadingChanged retains subscribers after Stop";

	[Test]
	[Category(UITestCategories.Essentials)]
	public void AccelerometerStopDoesNotRetainRemovedPages()
	{
		Assert.That(
			App.WaitForElement("Issue36216CycleStatus").GetText(),
			Is.EqualTo("Ready: 0 of 5 page cycles complete"));
		Assert.That(
			App.WaitForElement("Issue36216RetentionDetails").GetText(),
			Is.EqualTo("Retention check not started"));

		App.Tap("Issue36216RunCycles");

		Assert.That(
			App.WaitForTextToBePresentInElement(
				"Issue36216CycleStatus",
				"Ready: 5 of 5 page cycles complete",
				TimeSpan.FromSeconds(20)),
			Is.True);
		Assert.That(
			App.WaitForElement("Issue36216CycleStatus").GetText(),
			Is.EqualTo("Ready: 5 of 5 page cycles complete"));

		App.Tap("Issue36216CheckRetention");

		Assert.That(
			App.WaitForTextToBePresentInElement(
				"Issue36216RetentionDetails",
				"Retention check complete",
				TimeSpan.FromSeconds(10)),
			Is.True);
		var retainedPages = App.FindElements("Issue36216RetainedPage");

		Assert.That(
			retainedPages,
			Is.Empty,
			$"Accelerometer.Stop retained removed pages. Expected no retained page markers, but observed {retainedPages.Count}.");
	}
}
#endif
