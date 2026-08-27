#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue32417 : _IssuesUITest
{
	public Issue32417(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "Shell ItemTemplate and MenuItemTemplate are not applied dynamically at runtime";

	[Test]
	[Category(UITestCategories.Shell)]
	public void RuntimeShellTemplatesUpdateAfterAttachment()
	{
		App.WaitForElement("ApplyTemplatesButton");
		App.TapShellFlyoutIcon();
		var oldItemTemplate = App.WaitForElement("OldItemTemplate");
		var oldMenuTemplate = App.WaitForElement("OldMenuTemplate");
		Assert.That(oldItemTemplate, Is.Not.Null);
		Assert.That(oldMenuTemplate, Is.Not.Null);
		Assert.That(oldItemTemplate.GetText(), Is.EqualTo("OLD ITEM TEMPLATE"));
		Assert.That(oldMenuTemplate.GetText(), Is.EqualTo("OLD MENU TEMPLATE"));

		App.Tap("OldItemTemplate");
		App.WaitForElement("ApplyTemplatesButton");

		var initialStatus = App.WaitForElement("TemplateUpdateStatus");
		Assert.That(initialStatus, Is.Not.Null);
		Assert.That(initialStatus.GetText(), Is.EqualTo("Templates not applied"));

		App.Tap("ApplyTemplatesButton");
		var appliedStatus = App.WaitForElement(() =>
		{
			var status = App.FindElements("TemplateUpdateStatus").FirstOrDefault();
			if (status is null)
				return null;

			return status.GetText() == "Templates applied" ? status : null;
		}, "The template replacement callback did not complete");
		Assert.That(appliedStatus, Is.Not.Null);
		Assert.That(appliedStatus.GetText(), Is.EqualTo("Templates applied"));

		App.TapShellFlyoutIcon();

		Assert.Multiple(() =>
		{
			Assert.That(() => App.FindElements("NewItemTemplate").Count,
				Is.GreaterThan(0).After(10).Seconds.PollEvery(250).MilliSeconds,
				"Runtime Shell ItemTemplate remained stale after replacement. Observed OLD ITEM TEMPLATE; expected NEW ITEM TEMPLATE.");
			Assert.That(() => App.FindElements("OldItemTemplate").Count,
				Is.Zero.After(10).Seconds.PollEvery(250).MilliSeconds,
				"Runtime Shell ItemTemplate retained OLD ITEM TEMPLATE after replacement; expected it to be absent.");
			Assert.That(() => App.FindElements("NewMenuTemplate").Count,
				Is.GreaterThan(0).After(10).Seconds.PollEvery(250).MilliSeconds,
				"Runtime Shell MenuItemTemplate remained stale after replacement. Observed OLD MENU TEMPLATE; expected NEW MENU TEMPLATE.");
			Assert.That(() => App.FindElements("OldMenuTemplate").Count,
				Is.Zero.After(10).Seconds.PollEvery(250).MilliSeconds,
				"Runtime Shell MenuItemTemplate retained OLD MENU TEMPLATE after replacement; expected it to be absent.");
		});
	}
}
#endif
