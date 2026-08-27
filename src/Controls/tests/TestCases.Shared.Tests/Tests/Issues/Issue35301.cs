#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35301 : _IssuesUITest
{
	public Issue35301(TestDevice device) : base(device) { }

	public override string Issue => "Windows CollectionView applies WinUI styling on default";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void DefaultSingleSelectionDoesNotAddPlatformSelectionChrome()
	{
		App.WaitForElement("Apple");
		App.WaitForElement("Banana");
		App.WaitForElement("Cherry");

		Assert.That(
			App.WaitForTextToBePresentInElement("NativeProbe", "ProbeSequence=0;"),
			Is.True,
			"The initial native selection probe did not complete.");

		var initialProbeElement = App.FindElement("NativeProbe");
		if (initialProbeElement is null)
		{
			Assert.Fail("The initial native selection probe was not found.");
			return;
		}

		var initialProbe = initialProbeElement.GetText();
		Assert.That(initialProbe, Does.Contain("ManagedSelected=<null>;"));
		Assert.That(initialProbe, Does.Contain("NativeSelected=False;"));
		Assert.That(initialProbe, Does.Contain("VisibleSelectionChrome=0;"),
			$"The unselected CollectionView item did not have a clean native baseline: {initialProbe}");

		App.Tap("Apple");

		Assert.That(
			App.WaitForTextToBePresentInElement("NativeProbe", "ProbeSequence=1;"),
			Is.True,
			"The post-selection native probe did not complete.");
		Assert.That(
			App.WaitForTextToBePresentInElement("NativeProbe", "ManagedSelected=Apple;"),
			Is.True,
			"Apple was not selected through the managed CollectionView API.");
		Assert.That(
			App.WaitForTextToBePresentInElement("NativeProbe", "NativeSelected=True;"),
			Is.True,
			"Apple's realized WinUI ListViewItem was not selected.");

		var selectedProbeElement = App.FindElement("NativeProbe");
		if (selectedProbeElement is null)
		{
			Assert.Fail("The post-selection native probe was not found.");
			return;
		}

		var selectedProbe = selectedProbeElement.GetText();
		Assert.That(
			selectedProbe,
			Does.Contain("VisibleSelectionChrome=0;"),
			$"Selected CollectionView item exposed platform-added WinUI selection chrome: {selectedProbe}");
	}
}
#endif
