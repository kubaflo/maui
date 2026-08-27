#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue34738 : _IssuesUITest
{
	public override string Issue => "[Windows] TabBarDisabledColor is not applied to a disabled tab";

	public Issue34738(TestDevice testDevice) : base(testDevice)
	{
	}

	[Test]
	[Category(UITestCategories.Shell)]
	public void DisabledTabUsesTabBarDisabledColor()
	{
		var tab2 = App.WaitForElement("Tab2");
		var tab2Frame = tab2.GetRect();
		Assert.That(tab2Frame.Width, Is.GreaterThan(0), "Tab2 should have a nonempty native width.");
		Assert.That(tab2Frame.Height, Is.GreaterThan(0), "Tab2 should have a nonempty native height.");
		Assert.That(tab2.IsEnabled(), Is.True, "Tab2 should initially be enabled.");

		Assert.That(
			App.WaitForTextToBePresentInElement("Issue34738OracleStatus", "Ready:Tab2;Icon=groceries.png", System.TimeSpan.FromSeconds(10)),
			Is.True,
			"The native Tab2 title and icon oracle should become ready while Tab2 is enabled.");

		App.Tap("Issue34738DisableTab2");

		Assert.That(
			App.WaitForTextToBePresentInElement("Issue34738OracleStatus", "Observed:SameItem=True;ManagedEnabled=False;NativeEnabled=False", System.TimeSpan.FromSeconds(10)),
			Is.True,
			"The same native Tab2 item should report the enabled-to-disabled transition.");

		var expectedForeground = App.WaitForElement("Issue34738ExpectedForeground").GetText();
		var titleForeground = App.WaitForElement("Issue34738TitleForeground").GetText();
		var iconForeground = App.WaitForElement("Issue34738IconForeground").GetText();

		Assert.That(expectedForeground, Is.Not.Null, "The expected disabled foreground should be available.");
		Assert.That(titleForeground, Is.Not.Null, "The native disabled title foreground should be available.");
		Assert.That(iconForeground, Is.Not.Null, "The native disabled icon foreground should be available.");
		Assert.That(expectedForeground, Is.EqualTo("#FF008000"), "The oracle should derive Green from Shell.TabBarDisabledColor.");

		Assert.Multiple(() =>
		{
			Assert.That(titleForeground, Is.EqualTo(expectedForeground), "Issue34738 disabled Tab2 title foreground mismatch");
			Assert.That(iconForeground, Is.EqualTo(expectedForeground), "Issue34738 disabled Tab2 icon foreground mismatch");
		});
	}
}
#endif
