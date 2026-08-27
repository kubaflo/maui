#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue33530 : _IssuesUITest
{
	public override string Issue => "[Android] Initially rotated Border with Start alignment is positioned incorrectly";

	public Issue33530(TestDevice device) : base(device)
	{
	}

	[Test]
	[Category(UITestCategories.Border)]
	public void InitiallyRotatedBorderShouldAlignWithModalPageLeftEdge()
	{
		const int edgeTolerance = 4;

		App.SetOrientationPortrait();

		var hostPage = App.WaitForElement("Issue33530HostPage");
		var hostStatus = App.WaitForElement("Issue33530HostStatus");
		Assert.That(hostStatus.GetText(), Is.EqualTo("HOST_READY"));

		var hostRect = hostPage.GetRect();
		Assert.That(hostRect.Height, Is.GreaterThan(hostRect.Width), "The test requires portrait orientation.");

		App.Tap("Issue33530OpenClean");
		Assert.That(
			App.WaitForTextToBePresentInElement(
				"Issue33530CleanStatus",
				"LOADED Rotation=-90 HorizontalOptions=Start",
				TimeSpan.FromSeconds(10)),
			Is.True,
			"The clean modal did not complete its Loaded property transition.");

		var cleanPage = App.WaitForElement("Issue33530CleanPage");
		var cleanBorder = App.WaitForElement("Issue33530CleanBorder");
		var cleanBox = App.WaitForElement("Issue33530CleanBox");
		var cleanContent = App.WaitForElement("Issue33530CleanContent");
		var cleanStatus = App.WaitForElement("Issue33530CleanStatus");
		Assert.That(cleanContent.GetText(), Is.EqualTo("Rotated Border content"));
		Assert.That(cleanStatus.GetText(), Is.EqualTo("LOADED Rotation=-90 HorizontalOptions=Start"));

		var cleanPageRect = cleanPage.GetRect();
		var cleanBorderRect = cleanBorder.GetRect();
		var cleanBoxRect = cleanBox.GetRect();
		AssertPositiveRect(cleanPageRect.Width, cleanPageRect.Height, "clean modal page");
		AssertPositiveRect(cleanBorderRect.Width, cleanBorderRect.Height, "clean Border");
		AssertPositiveRect(cleanBoxRect.Width, cleanBoxRect.Height, "clean BoxView");
		Assert.That(
			Math.Abs(cleanBorderRect.X - cleanPageRect.X),
			Is.LessThanOrEqualTo(edgeTolerance),
			"The post-layout property transition should align the clean Border with the modal page.");
		Assert.That(
			cleanBorderRect.X + cleanBorderRect.Width,
			Is.LessThanOrEqualTo(cleanPageRect.X + cleanPageRect.Width + edgeTolerance),
			"The clean Border should remain within the modal page.");

		App.Tap("Issue33530CloseClean");
		var returnedStatus = App.WaitForElement("Issue33530HostStatus");
		Assert.That(returnedStatus.GetText(), Is.EqualTo("HOST_READY"));

		App.Tap("Issue33530OpenAffected");
		Assert.That(
			App.WaitForTextToBePresentInElement(
				"Issue33530AffectedStatus",
				"LOADED Rotation=-90 HorizontalOptions=Start",
				TimeSpan.FromSeconds(10)),
			Is.True,
			"The initially configured modal did not complete its Loaded callback.");

		var affectedPage = App.WaitForElement("Issue33530AffectedPage");
		var affectedBorder = App.WaitForElement("Issue33530AffectedBorder");
		var affectedBox = App.WaitForElement("Issue33530AffectedBox");
		var affectedContent = App.WaitForElement("Issue33530AffectedContent");
		var affectedStatus = App.WaitForElement("Issue33530AffectedStatus");
		var affectedLayoutResult = App.WaitForElement("Issue33530LayoutResult");
		Assert.That(affectedContent.GetText(), Is.EqualTo("Rotated Border content"));
		Assert.That(affectedStatus.GetText(), Is.EqualTo("LOADED Rotation=-90 HorizontalOptions=Start"));
		Assert.That(affectedLayoutResult.GetText(), Is.EqualTo("NOT_MEASURED"));

		var affectedPageRect = affectedPage.GetRect();
		var affectedBorderRect = affectedBorder.GetRect();
		var affectedBoxRect = affectedBox.GetRect();
		AssertPositiveRect(affectedPageRect.Width, affectedPageRect.Height, "affected modal page");
		AssertPositiveRect(affectedBorderRect.Width, affectedBorderRect.Height, "affected Border");
		AssertPositiveRect(affectedBoxRect.Width, affectedBoxRect.Height, "affected BoxView");

		App.Tap("Issue33530LayoutCheck");
		Assert.That(
			App.WaitForTextToBePresentInElement(
				"Issue33530LayoutResult",
				"borderLeft=",
				TimeSpan.FromSeconds(10)),
			Is.True,
			"The native transformed-bounds measurement did not complete.");
		var layoutResult = App.WaitForElement("Issue33530LayoutResult").GetText();
		Assert.That(
			layoutResult,
			Does.StartWith("ALIGNED:"),
			$"Rotated Border should touch the modal page's left edge; observed {layoutResult}, tolerance={edgeTolerance}.");
	}

	static void AssertPositiveRect(int width, int height, string elementName)
	{
		Assert.Multiple(() =>
		{
			Assert.That(width, Is.GreaterThan(0), $"{elementName} should have positive width.");
			Assert.That(height, Is.GreaterThan(0), $"{elementName} should have positive height.");
		});
	}
}
#endif
