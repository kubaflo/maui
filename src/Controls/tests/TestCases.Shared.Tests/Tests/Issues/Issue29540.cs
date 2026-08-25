#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue29540 : _IssuesUITest
{
	public Issue29540(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "TabbedViewHandler implementation incomplete on iOS";

	[Test]
	[Category(UITestCategories.TabbedPage)]
	public void CustomTabbedViewHandlerRendersHomeTab()
	{
		App.WaitForElement("HierarchyDescription");
		App.WaitForElement("NavigateButton");

		Assert.That(App.FindElements("HomeTabLabel").Count, Is.Zero);
		Assert.That(App.FindElements("NavigationCompletedMarker").Count, Is.Zero);

		App.Tap("NavigateButton");
		App.WaitForElement("NavigationCompletedMarker");

		var homeTabCount = App.FindElements("HomeTabLabel").Count;
		Assert.That(
			homeTabCount,
			Is.EqualTo(1),
			"Issue29540 custom TabbedViewHandler navigation should render one Home tab label");
	}
}
#endif
