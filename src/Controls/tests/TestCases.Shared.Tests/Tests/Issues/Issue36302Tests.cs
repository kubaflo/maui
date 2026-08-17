#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue36302 : _IssuesUITest
{
	public override string Issue => "Image and ImageButton BackgroundColor does not reset when set to null";

	public Issue36302(TestDevice testDevice) : base(testDevice)
	{
	}

	[Test]
	[Category(UITestCategories.Image)]
	public void ClearingBackgroundColorUpdatesNativeViews()
	{
		App.SetOrientationPortrait();

		var pageBounds = App.WaitForElement("Issue36302Page").GetRect();
		Assert.That(pageBounds.Height, Is.GreaterThan(pageBounds.Width), "The issue requires portrait orientation");

		var imageBounds = App.WaitForElement("IssueImage").GetRect();
		var imageButtonBounds = App.WaitForElement("IssueImageButton").GetRect();
		Assert.Multiple(() =>
		{
			Assert.That(imageBounds.Width, Is.EqualTo(120).Within(1));
			Assert.That(imageBounds.Height, Is.EqualTo(120).Within(1));
			Assert.That(imageButtonBounds.Width, Is.EqualTo(120).Within(1));
			Assert.That(imageButtonBounds.Height, Is.EqualTo(120).Within(1));
		});

		Assert.That(App.WaitForElement("ResultLabel").GetText(), Is.EqualTo("PENDING: 0"));

		App.Tap("SetRedButton");
		App.WaitForTextToBePresentInElement("StateLabel", "Current BackgroundColor: Red");
		App.WaitForTextToBePresentInElement("ResultLabel", "RED: 1");

		App.Tap("ClearButton");
		App.WaitForTextToBePresentInElement("StateLabel", "Current BackgroundColor: null");
		App.WaitForTextToBePresentInElement("ResultLabel", ": 2");

		var result = App.FindElement("ResultLabel").GetText();
		Assert.That(result, Is.EqualTo("CLEARED: 2"),
			"Image and ImageButton native backgrounds should be transparent after BackgroundColor is set to null");
	}
}
#endif
