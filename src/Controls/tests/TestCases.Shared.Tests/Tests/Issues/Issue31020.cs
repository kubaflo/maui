#if ANDROID
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue31020 : _IssuesUITest
{
	public Issue31020(TestDevice device)
		: base(device)
	{
	}

	public override string Issue => "Shell OnSizeAllocated is not called after device rotation";

	[Test]
	[Category(UITestCategories.Shell)]
	public void ShellReceivesSizeAllocatedAfterRotation()
	{
		App.SetOrientationPortrait();

		var pageRoot = App.WaitForElement("Issue31020PageRoot");
		Assert.That(pageRoot, Is.Not.Null);
		var portraitBounds = pageRoot.GetRect();
		Assert.That(portraitBounds.Width, Is.LessThan(portraitBounds.Height),
			"The page must begin in portrait orientation.");

		App.Tap("ArmRotationButton");
		var armedResult = App.FindElement("ResultLabel");
		Assert.That(armedResult, Is.Not.Null);
		Assert.That(GetRequiredText(armedResult), Is.EqualTo("Armed"),
			"The callback baselines must be armed before rotation.");

		App.SetOrientationLandscape();
		App.RetryAssert(() =>
		{
			var rotatedPageRoot = App.FindElement("Issue31020PageRoot");
			Assert.That(rotatedPageRoot, Is.Not.Null);
			var landscapeBounds = rotatedPageRoot.GetRect();
			Assert.That(landscapeBounds.Width, Is.GreaterThan(landscapeBounds.Height),
				"The native page bounds must prove that the device rotated to landscape.");
		});

		App.RetryAssert(() =>
		{
			var postRotationPageCount = App.FindElement("PageCountLabel");
			Assert.That(postRotationPageCount, Is.Not.Null);
			var postRotationPageCountText = GetRequiredText(postRotationPageCount);
			const string pagePrefix = "MainPage callbacks after rotation: ";
			Assert.That(postRotationPageCountText, Does.StartWith(pagePrefix));
			Assert.That(int.TryParse(postRotationPageCountText[pagePrefix.Length..], out int pageCallbacks), Is.True);
			Assert.That(pageCallbacks, Is.GreaterThan(0),
				"MainPage must receive a post-rotation OnSizeAllocated callback.");

			var geometryElement = App.FindElement("GeometryLabel");
			Assert.That(geometryElement, Is.Not.Null);
			var geometryText = GetRequiredText(geometryElement);
			const string geometryPrefix = "Last page allocation: ";
			Assert.That(geometryText, Does.StartWith(geometryPrefix));
			var dimensions = geometryText[geometryPrefix.Length..].Split('x');
			Assert.That(dimensions, Has.Length.EqualTo(2));
			Assert.That(double.TryParse(dimensions[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double allocatedWidth), Is.True);
			Assert.That(double.TryParse(dimensions[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double allocatedHeight), Is.True);
			Assert.That(allocatedWidth, Is.GreaterThan(allocatedHeight),
				"MainPage's measured post-arm allocation must be in landscape orientation.");
		});

		App.Tap("CheckRotationButton");

		var pageCountElement = App.FindElement("PageCountLabel");
		Assert.That(pageCountElement, Is.Not.Null);
		var pageCountText = GetRequiredText(pageCountElement);
		const string finalPagePrefix = "MainPage callbacks after rotation: ";
		Assert.That(pageCountText, Does.StartWith(finalPagePrefix));
		Assert.That(int.TryParse(pageCountText[finalPagePrefix.Length..], out int pageCallbacks), Is.True);
		Assert.That(pageCallbacks, Is.GreaterThan(0),
			"MainPage must receive a post-rotation OnSizeAllocated callback.");

		var shellCountElement = App.FindElement("ShellCountLabel");
		Assert.That(shellCountElement, Is.Not.Null);
		var shellCountText = GetRequiredText(shellCountElement);
		const string shellPrefix = "Shell callbacks after rotation: ";
		Assert.That(shellCountText, Does.StartWith(shellPrefix));
		Assert.That(int.TryParse(shellCountText[shellPrefix.Length..], out int shellCallbacks), Is.True);

		var landscapePageRoot = App.FindElement("Issue31020PageRoot");
		Assert.That(landscapePageRoot, Is.Not.Null);
		var finalBounds = landscapePageRoot.GetRect();
		Assert.That(shellCallbacks, Is.GreaterThan(0),
			$"Shell OnSizeAllocated callbacks after Android portrait-to-landscape rotation: expected at least 1 but was {shellCallbacks}. " +
			$"MainPage callbacks: {pageCallbacks}; portrait: {portraitBounds.Width}x{portraitBounds.Height}; " +
			$"landscape: {finalBounds.Width}x{finalBounds.Height}.");
	}

	static string GetRequiredText(IUIElement element)
	{
		var text = element.GetText();
		if (text is null)
			throw new AssertionException($"Element '{element}' did not provide text.");

		return text;
	}
}
#endif
