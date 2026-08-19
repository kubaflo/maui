#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue6369 : _IssuesUITest
{
	public Issue6369(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "Shell bottom tabs and icons are not displayed correctly on Windows";

	[Test]
	[Category(UITestCategories.Shell)]
	public void ShellTabsAreAtBottom()
	{
		App.Tap("ShowShellButton");

		var content = App.WaitForElement("CatsContent");
		Assert.That(content.GetText(), Is.EqualTo("Cats content"));

		var contentRect = content.GetRect();
		var catsRect = App.WaitForTabElement("Cats").GetRect();
		var dogsRect = App.WaitForTabElement("Dogs").GetRect();
		var contentBottom = contentRect.Y + contentRect.Height;

		Assert.Multiple(() =>
		{
			Assert.That(catsRect.Width, Is.GreaterThan(0), $"The realized Cats tab has no width. Rect={catsRect}.");
			Assert.That(catsRect.Height, Is.GreaterThan(0), $"The realized Cats tab has no height. Rect={catsRect}.");
			Assert.That(dogsRect.Width, Is.GreaterThan(0), $"The realized Dogs tab has no width. Rect={dogsRect}.");
			Assert.That(dogsRect.Height, Is.GreaterThan(0), $"The realized Dogs tab has no height. Rect={dogsRect}.");
			Assert.That(
				catsRect.Y,
				Is.GreaterThan(contentBottom),
				$"Issue6369 Windows Shell tab placement: Cats tab top={catsRect.Y}, content bottom={contentBottom}.");
			Assert.That(
				dogsRect.Y,
				Is.GreaterThan(contentBottom),
				$"Issue6369 Windows Shell tab placement: Dogs tab top={dogsRect.Y}, content bottom={contentBottom}.");
		});
	}
}
#endif
