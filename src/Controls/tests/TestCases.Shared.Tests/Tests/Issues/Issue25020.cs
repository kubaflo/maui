#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue25020 : _IssuesUITest
{
	public override string Issue => "Duplicated items in searched results";

	public Issue25020(TestDevice device)
		: base(device)
	{
	}

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void FilteringCollectionViewDoesNotRenderDuplicateItems()
	{
		App.WaitForElement("SearchEntry");
		App.WaitForElement("InspectButton");

		App.Tap("InspectButton");
		Assert.That(
			App.WaitForTextToBePresentInElement("InspectionGeneration", "0"),
			Is.True,
			"The clean CollectionView inspection did not complete");
		AssertCount("AaaCount", 1, "The clean CollectionView should render one AAA item template");
		AssertCount("BbbCount", 1, "The clean CollectionView should render one BBB item template");
		AssertCount("CccCount", 1, "The clean CollectionView should render one CCC item template");

		App.EnterText("SearchEntry", "a");
		Assert.That(
			App.WaitForTextToBePresentInElement("FilterGeneration", "0"),
			Is.True,
			"The filter callback did not complete after entering lowercase a");

		App.Tap("InspectButton");
		Assert.That(
			App.WaitForTextToBePresentInElement("InspectionGeneration", "1"),
			Is.True,
			"The filtered CollectionView inspection did not complete");
		AssertCount("AaaCount", 1, "Filtered CollectionView rendered duplicate AAA item templates");
		AssertCount("BbbCount", 0, "Filtered CollectionView rendered a BBB item template");
		AssertCount("CccCount", 0, "Filtered CollectionView rendered a CCC item template");
	}

	void AssertCount(string automationId, int expected, string message)
	{
		var element = App.WaitForElement(automationId);
		Assert.That(element, Is.Not.Null);
		Assert.That(element.GetText(), Is.EqualTo(expected.ToString()), message);
	}
}
#endif
