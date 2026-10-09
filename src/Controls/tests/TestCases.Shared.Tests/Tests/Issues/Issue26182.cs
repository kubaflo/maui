#if IOS
using System.Collections.Generic;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue26182 : _IssuesUITest
{
	public Issue26182(TestDevice device) : base(device)
	{
	}

	public override string Issue => "CollectionView items are not selected when a parent has a TapGestureRecognizer";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void ItemTapSelectsItemInsteadOfInvokingParentGesture()
	{
		const string failureSignature = "Issue26182 CollectionView item tap must select the tapped item";

		App.WaitForElement("ScenarioTitle");
		App.WaitForElement("ItemsCollection");
		App.WaitForElement("Hello");
		App.WaitForElement("World");
		App.WaitForElement("HelloWorldButton");

		Assert.That(GetRequiredText("ButtonStatus"), Is.EqualTo("Button clicked: 0"));
		Assert.That(GetRequiredText("ParentStatus"), Is.EqualTo("Parent taps: 0"));
		Assert.That(GetRequiredText("SelectionStatus"), Is.EqualTo("Selected item: none"));

		App.Tap("HelloWorldButton");
		Assert.That(
			App.WaitForTextToBePresentInElement("ButtonStatus", "Button clicked: 1", TimeSpan.FromSeconds(5)),
			Is.True,
			"The ordinary button should receive its click.");
		Assert.That(GetRequiredText("ParentStatus"), Is.EqualTo("Parent taps: 0"));

		var results = new List<(string ExpectedItem, bool SelectionObserved, string SelectionText, string ParentText)>();
		var expectedItems = new[] { "Hello", "World", "Hello" };

		for (var cycle = 0; cycle < expectedItems.Length; cycle++)
		{
			var expectedItem = expectedItems[cycle];
			App.Tap(expectedItem);

			var selectionObserved = App.WaitForTextToBePresentInElement(
				"SelectionStatus",
				$"Selected item: {expectedItem}",
				TimeSpan.FromSeconds(3));

			results.Add((
				expectedItem,
				selectionObserved,
				GetRequiredText("SelectionStatus"),
				GetRequiredText("ParentStatus")));

			if (cycle < expectedItems.Length - 1)
			{
				App.Tap("ResetButton");
				Assert.That(
					App.WaitForTextToBePresentInElement("ResetStatus", $"Reset complete: {cycle + 1}", TimeSpan.FromSeconds(3)),
					Is.True,
					$"Reset {cycle + 1} should complete before the next item tap.");
				Assert.That(GetRequiredText("SelectionStatus"), Is.EqualTo("Selected item: none"));
			}
		}

		Assert.Multiple(() =>
		{
			for (var cycle = 0; cycle < results.Count; cycle++)
			{
				var result = results[cycle];
				var details = $"{failureSignature}; cycle {cycle + 1}, tapped {result.ExpectedItem}, selection '{result.SelectionText}', parent '{result.ParentText}'";

				Assert.That(result.SelectionObserved, Is.True, details);
				Assert.That(result.SelectionText, Is.EqualTo($"Selected item: {result.ExpectedItem}"), details);
				Assert.That(result.ParentText, Is.EqualTo("Parent taps: 0"), details);
			}
		});
	}

	string GetRequiredText(string automationId)
	{
		var element = App.FindElement(automationId);
		if (element is null)
			Assert.Fail($"Element '{automationId}' was not found.");

		var text = element!.GetText();
		if (text is null)
			Assert.Fail($"Element '{automationId}' did not expose text.");

		return text!;
	}
}
#endif
