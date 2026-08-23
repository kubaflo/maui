#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue34071 : _IssuesUITest
{
	public override string Issue => "[Windows] The Shell's foreground color is not applied to the ToolbarItems";

	public Issue34071(TestDevice device) : base(device)
	{
	}

	[Test]
	[Category(UITestCategories.Shell)]
	public void ShellForegroundColorIsAppliedToToolbarItem()
	{
		var toolbarItem = App.WaitForElement("AffectedToolbarItem");
		var toolbarBounds = toolbarItem.GetRect();
		Assert.That(toolbarBounds.Width, Is.GreaterThan(0), "The affected ToolbarItem should be realized.");
		Assert.That(toolbarBounds.Height, Is.GreaterThan(0), "The affected ToolbarItem should be arranged.");

		Assert.That(App.WaitForElement("ToolbarForegroundMeasurementComplete").GetText(), Is.EqualTo("1"));
		Assert.That(App.WaitForElement("AffectedToolbarIdentity").GetText(), Is.EqualTo("AffectedToolbarItem"));

		var measuredForeground = App.WaitForElement("AffectedToolbarForeground").GetText();
		Assert.That(measuredForeground, Is.EqualTo("#FF800080"),
			$"Toolbar foreground measured {measuredForeground}; expected #FF800080 inherited from Shell.ForegroundColor");
	}
}
#endif
