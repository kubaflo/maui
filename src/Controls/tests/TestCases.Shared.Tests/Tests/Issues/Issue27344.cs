#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue27344 : _IssuesUITest
{
	public Issue27344(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "PopModalAsync accesses properties on the removed page's BindingContext";

	[Test]
	[Category(UITestCategories.Navigation)]
	public void PopModalDoesNotReadBindingContextAfterDeletion()
	{
		var initialReadCount = App.WaitForElement("Issue27344PostDeleteReadCount").GetText();
		Assert.That(initialReadCount, Is.Not.Null);
		Assert.That(initialReadCount, Is.EqualTo("-1"));

		App.Tap("Issue27344OpenButton");

		var boundAction = App.WaitForElement("Issue27344BoundAction");
		var isEnabled = boundAction.GetAttribute<string>("enabled");
		Assert.That(isEnabled, Is.Not.Null);
		Assert.That(isEnabled, Is.EqualTo("true").IgnoreCase);
		App.WaitForElement("Delete");

		var preDeleteReadCount = App.WaitForElement("Issue27344PreDeleteReadCount").GetText();
		Assert.That(preDeleteReadCount, Is.Not.Null);
		Assert.That(preDeleteReadCount, Is.EqualTo("0"));

		App.Tap("Delete");

		App.WaitForElement("Issue27344OpenButton");
		App.WaitForElement("ModalPopped");

		var postDeleteReadCount = App.WaitForElement("Issue27344PostDeleteReadCount").GetText();
		Assert.That(postDeleteReadCount, Is.Not.Null);
		Assert.That(
			postDeleteReadCount,
			Is.EqualTo("0"),
			$"PopModalAsync read CanPerformAction after deletion. Observed: {postDeleteReadCount ?? "<null>"}; Expected: 0.");
	}
}
#endif
