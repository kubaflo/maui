#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue34610 : _IssuesUITest
{
	public Issue34610(TestDevice device) : base(device) { }

	public override string Issue => "Shell TitleView on iOS has unremovable horizontal margins";

	[Test]
	[Category(UITestCategories.Shell)]
	public void TitleViewFillsNavigationBarWidth()
	{
		var appeared = App.WaitForTextToBePresentInElement("LifecycleStatus", "ShellAppeared");
		Assert.That(appeared, Is.True, "The Shell content page should appear before its geometry is measured.");

		var titleRect = App.WaitForElement("TitleViewGrid").GetRect();
		var titleTextRect = App.WaitForElement("TitleText").GetRect();
		var contentRect = App.WaitForElement("PageContent").GetRect();
		var backgroundRect = App.WaitForElement("PageBackground").GetRect();

		Assert.Multiple(() =>
		{
			Assert.That(titleRect.Width, Is.GreaterThan(0), "The TitleView should have a positive width.");
			Assert.That(titleRect.Height, Is.GreaterThan(0), "The TitleView should have a positive height.");
			Assert.That(titleTextRect.Width, Is.GreaterThan(0), "The named title should have a positive width.");
			Assert.That(titleTextRect.Height, Is.GreaterThan(0), "The named title should have a positive height.");
			Assert.That(contentRect.Width, Is.GreaterThan(0), "The page content should have a positive width.");
			Assert.That(contentRect.Height, Is.GreaterThan(0), "The page content should have a positive height.");
			Assert.That(backgroundRect.Width, Is.GreaterThan(0), "The page background should have a positive width.");
			Assert.That(backgroundRect.Height, Is.GreaterThan(0), "The page background should have a positive height.");
			Assert.That(backgroundRect.Width, Is.EqualTo(contentRect.Width).Within(2),
				"The full-width BoxView should establish the page-content surface width.");
			Assert.That(backgroundRect.X, Is.EqualTo(contentRect.X).Within(2),
				"The full-width BoxView should align with the page-content surface.");
			Assert.That(titleTextRect.X, Is.GreaterThanOrEqualTo(titleRect.X),
				"The named title should start inside the TitleView.");
			Assert.That(titleTextRect.X + titleTextRect.Width, Is.LessThanOrEqualTo(titleRect.X + titleRect.Width),
				"The named title should end inside the TitleView.");
			Assert.That(titleTextRect.Y, Is.GreaterThanOrEqualTo(titleRect.Y),
				"The named title should start vertically inside the TitleView.");
			Assert.That(titleTextRect.Y + titleTextRect.Height, Is.LessThanOrEqualTo(titleRect.Y + titleRect.Height),
				"The named title should end vertically inside the TitleView.");
		});

		var horizontalInset = contentRect.Width - titleRect.Width;
		Assert.That(titleRect.Width, Is.EqualTo(contentRect.Width).Within(2),
			$"Shell TitleView horizontal inset should be at most 2 px; title width={titleRect.Width}, surface width={contentRect.Width}, inset={horizontalInset}");
	}
}
#endif
