#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue34071 : _IssuesUITest
{
	public override string Issue => "Shell foreground color is not applied to ToolbarItems";

	public Issue34071(TestDevice testDevice) : base(testDevice) { }

	[Test]
	[Category(UITestCategories.Shell)]
	public void ShellForegroundColorAppliesToToolbarItem()
	{
		Assert.That(App.WaitForElement("Issue34071Page"), Is.Not.Null);
		Assert.That(App.WaitForElement("AffectedToolbarItem"), Is.Not.Null);

		var managedForeground = App.FindElement("ManagedForeground");
		Assert.That(managedForeground, Is.Not.Null);
		Assert.That(managedForeground!.GetText(), Is.EqualTo("MANAGED:#800080FF"));

		var initialResult = App.FindElement("NativeForegroundResult");
		Assert.That(initialResult, Is.Not.Null);
		Assert.That(initialResult!.GetText(), Is.EqualTo("PENDING"));

		App.Tap("CheckToolbarForegroundButton");
		Assert.That(
			App.WaitForTextToBePresentInElement("NativeForegroundResult", "MEASURED:", TimeSpan.FromSeconds(5)),
			Is.True,
			"The native toolbar foreground measurement did not complete.");

		var measuredResultElement = App.FindElement("NativeForegroundResult");
		Assert.That(measuredResultElement, Is.Not.Null);
		var measuredResult = measuredResultElement!.GetText();
		Assert.That(measuredResult, Is.Not.Null);
		Assert.That(
			measuredResult,
			Is.EqualTo("MEASURED:actual=128,0,128,255;expected=128,0,128,255"),
			$"Toolbar item foreground was not purple. Native measurement: {measuredResult}; expected RGBA: 128,0,128,255");
	}
}
#endif
