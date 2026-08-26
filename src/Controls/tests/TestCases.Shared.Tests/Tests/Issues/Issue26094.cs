#if WINDOWS
using System.Drawing;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue26094 : _IssuesUITest
{
	const int ExpectedImageSize = 44;
	const int SizeTolerance = 1;

	public Issue26094(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "Image renders at full window size instead of actual image size on AbsoluteLayout";

	[Test]
	[Category(UITestCategories.Image)]
	public void ImageInAbsoluteLayoutRetainsIntrinsicSize()
	{
		var calibrationElement = App.WaitForElement("Issue26094CalibrationImage");
		if (calibrationElement is null)
		{
			Assert.Fail("Issue26094 calibration image must exist");
			return;
		}

		var calibrationRect = calibrationElement.GetRect();

		Assert.Multiple(() =>
		{
			Assert.That(calibrationRect.Width, Is.EqualTo(ExpectedImageSize).Within(SizeTolerance),
				$"Issue26094 calibration image width must be {ExpectedImageSize}; observed {calibrationRect.Width}");
			Assert.That(calibrationRect.Height, Is.EqualTo(ExpectedImageSize).Within(SizeTolerance),
				$"Issue26094 calibration image height must be {ExpectedImageSize}; observed {calibrationRect.Height}");
		});

		var affectedRect = new Rectangle(-1, -1, -1, -1);
		var affectedElement = App.WaitForElement("Issue26094AffectedImage");
		if (affectedElement is null)
		{
			Assert.Fail("Issue26094 affected image must exist");
			return;
		}

		affectedRect = affectedElement.GetRect();

		Assert.That(affectedRect, Is.Not.EqualTo(new Rectangle(-1, -1, -1, -1)));
		Assert.Multiple(() =>
		{
			Assert.That(affectedRect.Width, Is.GreaterThan(0), "Issue26094 affected image must have a positive native width");
			Assert.That(affectedRect.Height, Is.GreaterThan(0), "Issue26094 affected image must have a positive native height");
			Assert.That(affectedRect.Width, Is.EqualTo(ExpectedImageSize).Within(SizeTolerance),
				$"Issue26094 image native frame must remain 44x44 after the initial Windows layout; observed {affectedRect.Width}x{affectedRect.Height}");
			Assert.That(affectedRect.Height, Is.EqualTo(ExpectedImageSize).Within(SizeTolerance),
				$"Issue26094 image native frame must remain 44x44 after the initial Windows layout; observed {affectedRect.Width}x{affectedRect.Height}");
		});
	}
}
#endif
