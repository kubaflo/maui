using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue37149 : _IssuesUITest
{
	public Issue37149(TestDevice device) : base(device)
	{
	}

	public override string Issue => "Shell Background is not applied to the Windows TabBar";

#if WINDOWS
	[Test]
	[Category(UITestCategories.Shell)]
	public void ShellBackgroundIsAppliedToTabBar()
	{
		Assert.That(
			App.WaitForTextToBePresentInElement("PageLoadedStatus", "Page loaded: complete"),
			Is.True,
			"The first Shell ContentPage did not complete loading.");
		Assert.That(
			App.WaitForTextToBePresentInElement("TemplateReadyStatus", "Tab template: ready"),
			Is.True,
			"The Shell tab template did not become ready.");

		Assert.That(
			App.WaitForElement("ManagedBackgroundStatus").GetText(),
			Is.EqualTo("Managed background: LinearGradientBrush"));
		Assert.That(
			App.WaitForElement("TabIdentityStatus").GetText(),
			Is.EqualTo("Tab identity: First tab|Second tab (2)"));

		var observedStatus = App.WaitForElement("TabBarBackgroundStatus").GetText();

		Assert.That(
			observedStatus,
			Is.EqualTo("Tab background: gradient applied"),
			$"Shell tab bar background did not use Shell.Background; observed status={observedStatus}");
	}
#endif
}
