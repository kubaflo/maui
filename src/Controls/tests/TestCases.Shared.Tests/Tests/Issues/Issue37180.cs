#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue37180 : _IssuesUITest
{
	public Issue37180(TestDevice device) : base(device)
	{
	}

	public override string Issue => "Label Background reset to null does not restore the default";

	[Test]
	[Category(UITestCategories.Label)]
	public void ClearingBackgroundRestoresTransparentNativeBackground()
	{
		App.SetOrientationPortrait();
		Assert.That(
			App.WaitForTextToBePresentInElement("ResultLabel", "NOT_TRIGGERED"),
			Is.True,
			"The issue page did not confirm portrait orientation before the trigger.");
		Assert.That(App.WaitForElement("BackgroundLabel").GetText(), Is.EqualTo("Label Background Test"));

		App.Tap("SetRedButton");
		Assert.That(
			App.WaitForTextToBePresentInElement("ResultLabel", "RED_CONFIRMED"),
			Is.True,
			"The attached native label did not reach the opaque red reference state.");

		App.Tap("SetBackgroundButton");
		Assert.That(
			App.WaitForTextToBePresentInElement("ResultLabel", "INSPECTION_COMPLETE:"),
			Is.True,
			"The post-trigger native background inspection did not complete.");

		var result = App.FindElement("ResultLabel").GetText();
		Assert.That(result, Is.EqualTo("INSPECTION_COMPLETE:TRANSPARENT"),
			"Label background should be transparent after clearing.");
	}
}
#endif
