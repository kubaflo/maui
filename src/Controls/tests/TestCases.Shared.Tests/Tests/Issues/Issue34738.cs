#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue34738 : _IssuesUITest
{
	public Issue34738(TestDevice device)
		: base(device)
	{
	}

	public override string Issue => "TabBarDisabledColor is not applied when the TabBar is disabled";

	[Test]
	[Category(UITestCategories.Shell)]
	public void DisabledTabUsesTabBarDisabledColor()
	{
		App.WaitForElement("Enabled Tab");
		App.WaitForElement("Disabled Tab");

		App.Tap("DisableButton");

		Assert.That(App.WaitForTextToBePresentInElement("ManagedStateLabel", "False"), Is.True);
		Assert.That(App.WaitForTextToBePresentInElement("NativeItemLabel", "Disabled Tab"), Is.True);
		Assert.That(App.WaitForTextToBePresentInElement("NativeStateLabel", "False"), Is.True);

		App.Tap("ObserveButton");

		Assert.That(App.WaitForTextToBePresentInElement("ObservationLabel", "Color observation complete"), Is.True);

		var calibrationColor = App.WaitForElement("CalibrationColorLabel").GetText();
		var disabledColor = App.WaitForElement("DisabledColorLabel").GetText();
		Assert.That(calibrationColor, Is.Not.Null);
		Assert.That(disabledColor, Is.Not.Null);
		Assert.That(calibrationColor, Is.EqualTo("#FF008000"));

		Assert.That(
			disabledColor,
			Is.EqualTo(calibrationColor),
			"Disabled Shell tab title foreground did not match TabBarDisabledColor");
	}
}
#endif
