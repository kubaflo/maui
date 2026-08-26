using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue34396 : _IssuesUITest
{
	public override string Issue => "UI becomes unresponsive when adding 200 Entry children to AbsoluteLayout";

	public Issue34396(TestDevice device) : base(device) { }

#if ANDROID
	[Test]
	[Category(UITestCategories.Performance)]
	public void BulkAddingEntriesKeepsUIResponsive()
	{
		App.WaitForElement("EntryCanvas");
		App.WaitForElement("AddEditorsButton");
		App.WaitForElement("ResponsivenessButton");

		var initialStatus = App.WaitForElement("BulkAddStatus").GetText();
		if (initialStatus is null)
			throw new InvalidOperationException("BulkAddStatus text was null before the trigger.");

		Assert.That(
			initialStatus,
			Is.EqualTo("Children=0;FinalEntry=False;Responsive=Unknown;Complete=False"));

		App.Tap("AddEditorsButton");
		App.WaitForTextToBePresentInElement("BulkAddStatus", "Complete=True");

		var completedStatus = App.WaitForElement("BulkAddStatus").GetText();
		if (completedStatus is null)
			throw new InvalidOperationException("BulkAddStatus text was null after the trigger.");

		var statusParts = completedStatus.Split(';', StringSplitOptions.TrimEntries);
		Assert.That(statusParts, Has.Length.EqualTo(4));
		Assert.That(statusParts[0], Is.EqualTo("Children=200"));
		Assert.That(statusParts[1], Is.EqualTo("FinalEntry=True"));
		Assert.That(statusParts[3], Is.EqualTo("Complete=True"));

		App.Tap("ResponsivenessButton");
		App.WaitForTextToBePresentInElement("ResponsivenessButton", "Clicked 1");

		Assert.That(
			statusParts[2],
			Is.EqualTo("Responsive=True"),
			"Bulk adding 200 Entry children should keep the UI responsive");
	}
#endif
}
