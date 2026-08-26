#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue28763 : _IssuesUITest
{
	public override string Issue => "Multiple SelectionChanged notifications with a singleton view model";

	public Issue28763(TestDevice testDevice) : base(testDevice)
	{
	}

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void SelectionChangedCommandRunsOnceOnSecondTransientPage()
	{
		App.WaitForElement("TaskEntry");
		App.WaitForElement("AddTask");
		App.WaitForElement("TaskList");

		App.EnterText("TaskEntry", "First task");
		App.Tap("AddTask");
		App.EnterText("TaskEntry", "Second task");
		App.Tap("AddTask");

		App.WaitForElement("TaskRow1");
		App.WaitForElement("TaskRow2");
		App.Tap("TaskRow1");

		App.WaitForElement("DetailItem1");
		var firstPageIdentity = App.WaitForElement("DetailPageIdentity").GetText();
		if (firstPageIdentity is null)
			throw new AssertionException("The first detail page should expose its identity");

		Assert.That(firstPageIdentity, Does.StartWith("Detail page: "));
		var firstViewModelIdentity = App.WaitForElement("ViewModelIdentity").GetText();
		if (firstViewModelIdentity is null)
			throw new AssertionException("The first detail page should expose its view-model identity");

		Assert.That(App.WaitForElement("ReadyState").GetText(),
			Is.EqualTo("Ready: Items=2; SelectedItem=null; Command=set"));
		Assert.That(App.WaitForElement("CommandCount").GetText(),
			Is.EqualTo("SelectionChangedCommand calls: 0"));

		App.Tap("DetailItem1");
		var firstMeasuredCount = WaitForCommandInvocation();
		Assert.That(firstMeasuredCount, Is.EqualTo(1));

		App.Back();
		App.WaitForElement("TaskRow1");
		App.WaitForElement("TaskRow2");
		App.Tap("TaskRow2");

		App.WaitForElement("DetailItem2");
		var secondPageIdentity = App.WaitForElement("DetailPageIdentity").GetText();
		if (secondPageIdentity is null)
			throw new AssertionException("The second detail page should expose its identity");

		Assert.That(secondPageIdentity, Does.StartWith("Detail page: "));
		Assert.That(secondPageIdentity, Is.Not.EqualTo(firstPageIdentity));
		var secondViewModelIdentity = App.WaitForElement("ViewModelIdentity").GetText();
		if (secondViewModelIdentity is null)
			throw new AssertionException("The second detail page should expose its view-model identity");

		Assert.That(secondViewModelIdentity, Is.EqualTo(firstViewModelIdentity));
		Assert.That(App.WaitForElement("ReadyState").GetText(),
			Is.EqualTo("Ready: Items=2; SelectedItem=null; Command=set"));
		Assert.That(App.WaitForElement("CommandCount").GetText(),
			Is.EqualTo("SelectionChangedCommand calls: 0"));

		App.Tap("DetailItem2");
		var measuredCount = WaitForCommandInvocation();
		Assert.That(measuredCount, Is.EqualTo(1),
			"A single selection on the second transient detail page should invoke SelectionChangedCommand once");
	}

	int WaitForCommandInvocation()
	{
		var measuredCount = -1;
		App.RetryAssert(() =>
		{
			var countText = App.FindElement("CommandCount").GetText();
			if (countText is null)
				throw new InvalidOperationException("The command-count label should expose its measured value");

			const string prefix = "SelectionChangedCommand calls: ";
			if (!countText.StartsWith(prefix, StringComparison.Ordinal))
				throw new InvalidOperationException($"The command-count label should start with '{prefix}'");

			var measuredCountText = countText.Substring(prefix.Length);
			if (!int.TryParse(measuredCountText, out measuredCount))
				throw new InvalidOperationException("The command-count label should expose a numeric measured value");

			if (measuredCount <= 0)
				throw new InvalidOperationException("The selection should invoke SelectionChangedCommand");
		});
		return measuredCount;
	}
}
#endif
