#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue37705 : _IssuesUITest
{
	public override string Issue => "Status bar icons are unreadable when Material 3 is enabled";

	public Issue37705(TestDevice device) : base(device) { }

	[Test]
	[Category(UITestCategories.Material3)]
	public void LightStatusBarUsesContrastingSystemContent()
	{
		App.SetOrientationPortrait();
		App.WaitForElement("Issue37705CheckButton");
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue37705Observation", "Token=1"),
			Is.True,
			"The attached decor-view observation did not complete.");

		App.Tap("Issue37705CheckButton");
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue37705Observation", "Action=Tapped"),
			Is.True,
			"The recorded check action did not update the page.");

		var observationText = App.FindElement("Issue37705Observation").GetText();
		if (observationText is null)
		{
			Assert.Fail("The status-bar observation text was null.");
			return;
		}

		Assert.Multiple(() =>
		{
			Assert.That(observationText, Does.Contain("UiMode=Light"), "The activity was not using light mode.");
			Assert.That(observationText, Does.Contain("Orientation=Portrait"), "The activity was not in portrait orientation.");
			Assert.That(observationText, Does.Match(@"StatusBarInset=[1-9][0-9]*"), "The runtime status-bar inset was not positive.");
			Assert.That(observationText, Does.Contain("ImeVisible=False"), "The software keyboard was visible.");
			Assert.That(observationText, Does.Contain("Attached=True"), "The decor view was not attached.");
			Assert.That(observationText, Does.Contain("Action=Tapped"), "The recorded check action was not observed.");
		});

		Assert.That(
			observationText,
			Does.Contain("LightStatusBars=True"),
			$"Material 3 status bar icons do not contrast with the light default surface. Observed: {observationText}");
	}
}
#endif
