#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue33530 : _IssuesUITest
{
	public Issue33530(TestDevice device) : base(device) { }

	public override string Issue => "Border with Rotation and HorizontalOptions.Start is positioned incorrectly on initial load";

	[Test]
	[Category(UITestCategories.Border)]
	public void RotatedStartAlignedBorderUsesItsVisualBoundsOnInitialModalLayout()
	{
		const double requestedWidth = 160;
		const double requestedHeight = 300;
		const double tolerance = 2;

		App.SetOrientationPortrait();
		Assert.That(App.WaitForTextToBePresentInElement("Issue33530Mode", "Reference state ready"), Is.True);

		var referenceSurface = App.WaitForElement("Issue33530Surface").GetRect();
		var referenceBorder = App.WaitForElement("Issue33530Border").GetRect();
		var stableReferenceSurface = App.FindElement("Issue33530Surface").GetRect();
		var stableReferenceBorder = App.FindElement("Issue33530Border").GetRect();
		var density = referenceBorder.Width / requestedWidth;
		var expectedReferenceX = referenceSurface.X + ((referenceSurface.Width - referenceBorder.Width) / 2d);

		Assert.Multiple(() =>
		{
			Assert.That(referenceSurface.Height, Is.GreaterThan(referenceSurface.Width), "The test surface must be in portrait orientation.");
			Assert.That(referenceSurface.Width, Is.GreaterThan(0), "The reference surface must have a nonzero native width.");
			Assert.That(referenceBorder.Width, Is.GreaterThan(0), "The reference Border must have a nonzero native width.");
			Assert.That(stableReferenceSurface.X, Is.EqualTo(referenceSurface.X).Within(tolerance), "The reference surface position must be stable.");
			Assert.That(stableReferenceSurface.Width, Is.EqualTo(referenceSurface.Width).Within(tolerance), "The reference surface width must be stable.");
			Assert.That(stableReferenceBorder.X, Is.EqualTo(referenceBorder.X).Within(tolerance), "The reference Border position must be stable.");
			Assert.That(stableReferenceBorder.Width, Is.EqualTo(referenceBorder.Width).Within(tolerance), "The reference Border width must be stable.");
			Assert.That(referenceBorder.Width, Is.EqualTo(requestedWidth * density).Within(tolerance), "The reference Border width must match its WidthRequest.");
			Assert.That(referenceBorder.Height, Is.EqualTo(requestedHeight * density).Within(tolerance), "The reference Border height must match its HeightRequest.");
			Assert.That(referenceBorder.X, Is.EqualTo(expectedReferenceX).Within(tolerance), "The unrotated reference Border must be centered.");
		});

		App.Tap("Issue33530OpenButton");
		Assert.That(App.WaitForTextToBePresentInElement("Issue33530Mode", "Modal reproduction loaded"), Is.True);
		Assert.That(App.WaitForTextToBePresentInElement("Issue33530Lifecycle", "1"), Is.True,
			"The rotated modal Border must complete its post-Loaded layout callback.");
		Assert.That(App.WaitForElement("BORDER CONTENT").GetText(), Is.EqualTo("BORDER CONTENT"));

		var modalSurface = App.WaitForElement("Issue33530Surface").GetRect();
		var modalBorder = App.WaitForElement("Issue33530Border").GetRect();
		var stableSurface = App.FindElement("Issue33530Surface").GetRect();
		var stableBorder = App.FindElement("Issue33530Border").GetRect();

		Assert.Multiple(() =>
		{
			Assert.That(stableSurface.X, Is.EqualTo(modalSurface.X).Within(tolerance), "The modal surface position must be stable before evaluating alignment.");
			Assert.That(stableSurface.Width, Is.EqualTo(modalSurface.Width).Within(tolerance), "The modal surface width must be stable before evaluating alignment.");
			Assert.That(stableBorder.X, Is.EqualTo(modalBorder.X).Within(tolerance), "The modal Border position must be stable before evaluating alignment.");
			Assert.That(stableBorder.Width, Is.EqualTo(modalBorder.Width).Within(tolerance), "The modal Border width must be stable before evaluating alignment.");
			Assert.That(modalSurface.Width, Is.GreaterThan(0), "The modal surface must have a nonzero native width.");
			Assert.That(modalBorder.Width, Is.GreaterThan(0), "The modal Border must have a nonzero native width.");
		});

		var expectedVisualLeft = modalSurface.X;
		var expectedVisualWidth = requestedHeight * density;
		var isVisuallyStartAligned =
			Math.Abs(modalBorder.X - expectedVisualLeft) <= tolerance &&
			Math.Abs(modalBorder.Width - expectedVisualWidth) <= tolerance;

		Assert.That(isVisuallyStartAligned, Is.True,
			$"Issue33530 rotated Border visual-left alignment failed: Border X={modalBorder.X}, surface X={modalSurface.X}, " +
			$"Border width={modalBorder.Width}, density={density:F3}, expected X={expectedVisualLeft}, " +
			$"expected width={expectedVisualWidth:F1}, tolerance={tolerance}.");
	}
}
#endif
