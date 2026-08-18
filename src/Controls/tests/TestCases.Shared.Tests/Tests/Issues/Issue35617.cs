#if WINTEST
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35617 : _IssuesUITest
{
	public Issue35617(TestDevice testDevice) : base(testDevice) { }

	public override string Issue => "Horizontal CollectionView delays rendering the first rapidly added item";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void RapidlyAddedItemsRenderInFirstDispatcherPass()
	{
		App.WaitForElement("ItemsCollectionView");
		App.WaitForElement("item: 0");
		Assert.That(App.WaitForTextToBePresentInElement("ResultLabel", "callbacks=0;index=-1;handler=True"), Is.True);

		App.Tap("ResetButton");
		string firstCycle = RunRapidAddCycle();

		App.Tap("ResetButton");
		string secondCycle = RunRapidAddCycle();

		AssertCycleRendered(firstCycle);
		AssertCycleRendered(secondCycle);
	}

	string RunRapidAddCycle()
	{
		App.WaitForElement("item: 0");
		Assert.That(App.WaitForTextToBePresentInElement("ResultLabel", "callbacks=0;index=-1;handler=True"), Is.True);

		App.Tap("AddButton");
		App.Tap("AddButton");
		App.Tap("AddButton");
		App.Tap("AddButton");

		Assert.That(App.WaitForTextToBePresentInElement("ResultLabel", "callbacks=4;index=4"), Is.True,
			"Every post-add inspection callback should complete for its initiating item.");
		return App.FindElement("ResultLabel").GetText() ?? string.Empty;
	}

	static void AssertCycleRendered(string result)
	{
		for (int itemIndex = 1; itemIndex <= 4; itemIndex++)
		{
			string prefix = $"item: {itemIndex};index={itemIndex};";
			string record = FindRecord(result, prefix);
			Assert.That(record, Is.Not.Empty, $"The first-pass callback for item: {itemIndex} was not recorded. {result}");
			Assert.That(record, Does.Contain("source=True"), $"The source item was not present at index {itemIndex}. {record}");

			string message = itemIndex == 1
				? $"Issue35617 first-pass rendering failure for item: 1; {record}"
				: $"Issue35617 first-pass rendering failure for item: {itemIndex}; {record}";
			Assert.That(record, Does.Contain("rendered=True"), message);
		}
	}

	static string FindRecord(string result, string prefix)
	{
		foreach (string part in result.Split('|'))
		{
			if (part.StartsWith(prefix, StringComparison.Ordinal))
				return part;
		}

		return string.Empty;
	}
}
#endif
