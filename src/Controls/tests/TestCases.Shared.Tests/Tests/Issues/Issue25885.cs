using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

#if IOS
public class Issue25885 : _IssuesUITest
{
	public Issue25885(TestDevice device) : base(device) { }

	public override string Issue => "Command event spills to parent if child command is busy";

	[Test]
	[Category(UITestCategories.Button)]
	public void UnavailableChildCommandDoesNotExecuteParentCommand()
	{
		App.WaitForElement("ChildButton");
		Assert.That(App.WaitForElement("ChildStateLabel").GetText(), Is.EqualTo("Child command: available"));
		Assert.That(App.WaitForElement("ChildCountLabel").GetText(), Is.EqualTo("Child executions: 0"));
		Assert.That(App.WaitForElement("ParentCountLabel").GetText(), Is.EqualTo("Parent executions: 0"));

		App.Tap("ChildButton");

		Assert.That(
			App.WaitForTextToBePresentInElement("ChildCountLabel", "Child executions: 1"),
			Is.True);
		Assert.That(
			App.WaitForTextToBePresentInElement("ChildStateLabel", "Child command: unavailable"),
			Is.True);

		var parentBeforeSecondTap = ReadCount("ParentCountLabel", "Parent executions: ");
		Assert.That(parentBeforeSecondTap, Is.EqualTo(0));

		var parentAfterSecondTap = -1;
		App.Tap("ChildButton");

		parentAfterSecondTap = ReadCount("ParentCountLabel", "Parent executions: ");
		var childAfterSecondTap = ReadCount("ChildCountLabel", "Child executions: ");

		Assert.That(parentAfterSecondTap, Is.Not.EqualTo(-1));
		Assert.That(childAfterSecondTap, Is.EqualTo(1));
		Assert.That(
			parentAfterSecondTap,
			Is.EqualTo(0),
			$"Unavailable child tap reached parent command; baseline={parentBeforeSecondTap}, observed={parentAfterSecondTap}, expected=0");
	}

	int ReadCount(string automationId, string prefix)
	{
		var text = App.WaitForElement(automationId).GetText();
		if (text is null)
		{
			Assert.Fail($"Element '{automationId}' did not expose text.");
			return -1;
		}

		Assert.That(text, Does.StartWith(prefix));
		return int.Parse(text[prefix.Length..]);
	}
}
#endif
