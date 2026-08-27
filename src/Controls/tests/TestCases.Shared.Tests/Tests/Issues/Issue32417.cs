using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

#if IOS
public class Issue32417 : _IssuesUITest
{
	public override string Issue => "Shell templates are not applied dynamically at runtime";

	public Issue32417(TestDevice device)
		: base(device)
	{
	}

	[Test]
	[Category(UITestCategories.Shell)]
	public void RuntimeTemplateChangesAreRenderedInFlyout()
	{
		var assignmentObserved = "NotAssigned";
		var replacementItemCount = -1;
		var replacementMenuItemCount = -1;

		App.WaitForElement("OpenFlyoutButton");
		App.Tap("OpenFlyoutButton");

		var initialItem = App.WaitForElement("InitialItemTemplateVisual");
		var initialMenuItem = App.WaitForElement("InitialMenuItemTemplateVisual");
		if (initialItem is null)
		{
			throw new InvalidOperationException("The initial Shell item template was not rendered.");
		}

		if (initialMenuItem is null)
		{
			throw new InvalidOperationException("The initial Shell menu item template was not rendered.");
		}

		var initialItemText = initialItem.GetText();
		var initialMenuItemText = initialMenuItem.GetText();
		if (initialItemText is null || initialMenuItemText is null)
		{
			throw new InvalidOperationException("The initial Shell template text was not available.");
		}

		Assert.That(initialItemText, Is.EqualTo("Flyout Home"));
		Assert.That(initialMenuItemText, Is.EqualTo("Flyout Action"));

		App.Tap("InitialItemTemplateVisual");
		App.WaitForNoElement("InitialItemTemplateVisual");
		App.WaitForElement("ApplyTemplatesButton");
		App.Tap("ApplyTemplatesButton");

		App.WaitForElement("TemplatesAssigned");
		var assignmentMarker = App.FindElement("TemplatesAssignedMarker");
		if (assignmentMarker is null)
		{
			throw new InvalidOperationException("The template assignment marker was not found.");
		}

		var markerText = assignmentMarker.GetText();
		if (markerText is null)
		{
			throw new InvalidOperationException("The template assignment marker text was not available.");
		}

		assignmentObserved = markerText;
		Assert.That(assignmentObserved, Is.EqualTo("TemplatesAssigned"));

		App.Tap("OpenFlyoutButton");
		App.WaitForElement("Flyout Home");
		App.WaitForElement("Flyout Action");

		replacementItemCount = App.FindElements("ReplacementItemTemplateVisual").Count;
		replacementMenuItemCount = App.FindElements("ReplacementMenuItemTemplateVisual").Count;

		Assert.Multiple(() =>
		{
			Assert.That(replacementItemCount, Is.EqualTo(1),
				$"Replacement ItemTemplate visual was not rendered after runtime update: observed {replacementItemCount}, expected 1.");
			Assert.That(replacementMenuItemCount, Is.EqualTo(1),
				$"Replacement MenuItemTemplate visual was not rendered after runtime update: observed {replacementMenuItemCount}, expected 1.");
		});
	}
}
#endif
