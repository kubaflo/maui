#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue33530 : _IssuesUITest
{
	const double BorderWidthRequest = 160;
	const double MinimumRotatedWidth = 220 + 12 + 32;

	public override string Issue => "Border with Rotation and Start alignment is positioned incorrectly on initial load";

	public Issue33530(TestDevice device) : base(device) { }

	[Test]
	[Category(UITestCategories.Border)]
	public void InitiallyRotatedStartAlignedBorderIsFullyVisible()
	{
		App.SetOrientationPortrait();

		var issueRoot = App.WaitForElement("Issue33530Root");
		Assert.That(issueRoot, Is.Not.Null);
		var issueRect = issueRoot.GetRect();
		Assert.That(issueRect.Height, Is.GreaterThan(issueRect.Width), "The issue requires portrait orientation.");

		var referenceLifecycle = App.WaitForElement("ReferenceLifecycleToken");
		Assert.That(referenceLifecycle, Is.Not.Null);
		Assert.That(referenceLifecycle.GetText(), Is.EqualTo("-1"));

		App.Tap("OpenReferenceButton");

		var referenceContent = App.WaitForElement("ReferenceContentLabel");
		Assert.That(referenceContent, Is.Not.Null);
		var referenceBorder = App.WaitForElement("ReferenceBorder");
		Assert.That(referenceBorder, Is.Not.Null);
		Assert.That(App.WaitForTextToBePresentInElement("ReferenceLifecycleToken", "Loaded"), Is.True,
			"The clean reference Border did not complete Loaded.");
		referenceLifecycle = App.FindElement("ReferenceLifecycleToken");
		Assert.That(referenceLifecycle, Is.Not.Null);
		Assert.That(referenceLifecycle.GetText(), Is.EqualTo("Loaded"));

		var scale = App.FindElement("ReferenceBorder").GetRect().Width / BorderWidthRequest;
		Assert.That(scale, Is.GreaterThan(0), "The clean reference Border did not establish a valid device scale.");

		App.Tap("RotateReferenceButton");

		const string expectedArrangement = "Rotation=-90;HorizontalOptions=Start;Shadow=True;Content=True";
		Assert.That(App.WaitForTextToBePresentInElement("ReferenceArrangementToken", expectedArrangement), Is.True,
			"The clean reference Border did not complete its post-load arrangement.");
		var referenceArrangement = App.FindElement("ReferenceArrangementToken");
		Assert.That(referenceArrangement, Is.Not.Null);
		Assert.That(referenceArrangement.GetText(), Is.EqualTo(expectedArrangement));

		var tolerance = (4 * scale) + 2;
		var expectedVisibleWidth = MinimumRotatedWidth * scale;

		App.Back();
		App.WaitForElement("OpenAffectedButton");

		var affectedLifecycle = App.WaitForElement("AffectedLifecycleToken");
		Assert.That(affectedLifecycle, Is.Not.Null);
		Assert.That(affectedLifecycle.GetText(), Is.EqualTo("-1"));

		App.Tap("OpenAffectedButton");

		var affectedContent = App.WaitForElement("AffectedContentLabel");
		Assert.That(affectedContent, Is.Not.Null);
		var affectedBorder = App.WaitForElement("AffectedBorder");
		Assert.That(affectedBorder, Is.Not.Null);
		Assert.That(App.WaitForTextToBePresentInElement("AffectedLifecycleToken", "Loaded"), Is.True,
			"The affected Border did not complete Loaded.");
		affectedLifecycle = App.FindElement("AffectedLifecycleToken");
		Assert.That(affectedLifecycle, Is.Not.Null);
		Assert.That(affectedLifecycle.GetText(), Is.EqualTo("Loaded"));

		Assert.That(App.WaitForTextToBePresentInElement("AffectedArrangementToken", expectedArrangement), Is.True,
			"The affected Border did not report its initial arrangement.");
		var affectedArrangement = App.FindElement("AffectedArrangementToken");
		Assert.That(affectedArrangement, Is.Not.Null);
		Assert.That(affectedArrangement.GetText(), Is.EqualTo(expectedArrangement));

		var actualVisibleWidth = App.FindElement("AffectedBorder").GetRect().Width;
		Assert.That(actualVisibleWidth, Is.GreaterThanOrEqualTo(expectedVisibleWidth - tolerance),
			$"Rotated Border visible width was {actualVisibleWidth}px; expected at least {expectedVisibleWidth - tolerance}px.");
	}
}
#endif
