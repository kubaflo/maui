#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue36040 : _IssuesUITest
{
	public override string Issue => "Modal page reserves title bar space in full-screen mode";

	public Issue36040(TestDevice device) : base(device)
	{
	}

	[Test]
	[Category(UITestCategories.Navigation)]
	public void ModalPageCoversFullScreenWindow()
	{
		var fullScreenStatus = App.WaitForElement("FullScreenStatus", timeout: TimeSpan.FromSeconds(15));
		if (fullScreenStatus is null)
		{
			Assert.Fail("The full-screen status marker was not found.");
			return;
		}

		Assert.That(fullScreenStatus.GetText(), Is.EqualTo("FullScreen"));

		var mainSurface = App.WaitForElement("MainSurface", timeout: TimeSpan.FromSeconds(15));
		if (mainSurface is null)
		{
			Assert.Fail("The blue main surface was not found.");
			return;
		}

		var mainRect = mainSurface.GetRect();
		var mainTop = (double)mainRect.Y;
		Assert.That(mainRect.Width, Is.GreaterThan(0), "The blue main surface must have positive width.");
		Assert.That(mainRect.Height, Is.GreaterThan(0), "The blue main surface must have positive height.");
		Assert.That(mainTop, Is.EqualTo(0).Within(1), "The blue main surface must begin at the full-screen top edge.");

		App.Tap("PushModalButton");

		var modalLoaded = false;
		var modalTop = double.NaN;
		var modalWidth = double.NaN;
		var modalHeight = double.NaN;

		App.RetryAssert(() =>
		{
			var loadedMarker = App.FindElement("ModalLoadedMarker");
			if (loadedMarker is null)
			{
				Assert.Fail("The modal loaded marker was not found.");
				return;
			}

			Assert.That(loadedMarker.GetText(), Is.EqualTo("ModalLoaded"));

			var modalSurface = App.FindElement("ModalSurface");
			if (modalSurface is null)
			{
				Assert.Fail("The red modal surface was not found.");
				return;
			}

			var modalRect = modalSurface.GetRect();
			modalLoaded = true;
			modalTop = modalRect.Y;
			modalWidth = modalRect.Width;
			modalHeight = modalRect.Height;
		});

		Assert.That(modalLoaded, Is.True, "The modal Loaded transition must complete.");
		Assert.That(double.IsNaN(modalTop), Is.False, "The modal top position must be observed after loading.");
		Assert.That(modalWidth, Is.GreaterThan(0), "The red modal surface must have positive width.");
		Assert.That(modalHeight, Is.GreaterThan(0), "The red modal surface must have positive height.");
		Assert.That(modalTop, Is.EqualTo(0).Within(1),
			$"Modal surface must begin at the full-screen top edge. Observed top={modalTop:F2}px; expected=0.00 +/- 1.00px; clean main top={mainTop:F2}px.");
	}
}
#endif
