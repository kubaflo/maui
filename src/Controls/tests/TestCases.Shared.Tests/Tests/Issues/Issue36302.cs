#if IOS
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue36302 : _IssuesUITest
{
	public Issue36302(TestDevice device) : base(device) { }

	public override string Issue => "Image and ImageButton BackgroundColor does not reset when set to null";

	[Test]
	[Category(UITestCategories.Image)]
	public void NullBackgroundColorClearsNativeBackground()
	{
		App.WaitForTextToBePresentInElement("ImageState", "generation=-1");
		App.WaitForTextToBePresentInElement("ImageButtonState", "generation=-1");

		var initialImage = GetRequiredText("ImageState");
		var initialImageButton = GetRequiredText("ImageButtonState");
		var references = GetRequiredText("ReferenceState");

		Assert.That(initialImage, Does.Contain("handler=ImageHandler;attached=True;window=True;sourceConfigured=True;bounds=150.000x90.000;managed=Blue"));
		Assert.That(initialImageButton, Does.Contain("handler=ImageButtonHandler;attached=True;window=True;sourceConfigured=True;bounds=150.000x90.000;managed=Blue"));
		AssertRgba(initialImage, 0, 0, 1, 1, "Image initial native background");
		AssertRgba(initialImageButton, 0, 0, 1, 1, "ImageButton initial native background");
		AssertRgba(references, 0, 0, 0, 0, "reference Image native background", "image=");
		AssertRgba(references, 0, 0, 0, 0, "reference ImageButton native background", "imageButton=");

		App.Tap("ApplyRedButton");
		App.WaitForTextToBePresentInElement("ImageState", "generation=1");
		App.WaitForTextToBePresentInElement("ImageButtonState", "generation=1");

		var redImage = GetRequiredText("ImageState");
		var redImageButton = GetRequiredText("ImageButtonState");
		Assert.That(redImage, Does.Contain("managed=Red"));
		Assert.That(redImageButton, Does.Contain("managed=Red"));
		AssertRgba(redImage, 1, 0, 0, 1, "Image red native background");
		AssertRgba(redImageButton, 1, 0, 0, 1, "ImageButton red native background");

		App.Tap("ClearBackgroundButton");
		App.WaitForTextToBePresentInElement("ImageState", "generation=2");
		App.WaitForTextToBePresentInElement("ImageButtonState", "generation=2");

		var clearedImage = GetRequiredText("ImageState");
		var clearedImageButton = GetRequiredText("ImageButtonState");
		Assert.That(clearedImage, Does.Contain("managed=null"));
		Assert.That(clearedImageButton, Does.Contain("managed=null"));

		var imageRgba = ParseRgba(clearedImage);
		var imageButtonRgba = ParseRgba(clearedImageButton);
		Assert.Multiple(() =>
		{
			Assert.That(imageRgba.Alpha, Is.EqualTo(0).Within(0.05),
				$"Image native background remained red after BackgroundColor was set to null; observed rgba={imageRgba}");
			Assert.That(imageButtonRgba.Alpha, Is.EqualTo(0).Within(0.05),
				$"ImageButton native background remained red after BackgroundColor was set to null; observed rgba={imageButtonRgba}");
		});
	}

	string GetRequiredText(string automationId)
	{
		var text = App.WaitForElement(automationId).GetText();
		if (text is null)
			throw new AssertionException($"{automationId} did not expose observation text.");
		return text;
	}

	static void AssertRgba(string observation, double red, double green, double blue, double alpha, string description, string marker = "rgba=")
	{
		var actual = ParseRgba(observation, marker);
		Assert.Multiple(() =>
		{
			Assert.That(actual.Red, Is.EqualTo(red).Within(0.05), $"{description} red component was {actual.Red}");
			Assert.That(actual.Green, Is.EqualTo(green).Within(0.05), $"{description} green component was {actual.Green}");
			Assert.That(actual.Blue, Is.EqualTo(blue).Within(0.05), $"{description} blue component was {actual.Blue}");
			Assert.That(actual.Alpha, Is.EqualTo(alpha).Within(0.05), $"{description} alpha component was {actual.Alpha}");
		});
	}

	static (double Red, double Green, double Blue, double Alpha) ParseRgba(string observation, string marker = "rgba=")
	{
		var start = observation.IndexOf(marker, StringComparison.Ordinal);
		Assert.That(start, Is.GreaterThanOrEqualTo(0), $"Missing {marker} in '{observation}'.");
		start += marker.Length;
		var end = observation.IndexOf(';', start);
		var value = end < 0 ? observation[start..] : observation[start..end];
		var components = value.Split(',');
		Assert.That(components, Has.Length.EqualTo(4), $"Invalid RGBA value '{value}'.");
		return (
			double.Parse(components[0], CultureInfo.InvariantCulture),
			double.Parse(components[1], CultureInfo.InvariantCulture),
			double.Parse(components[2], CultureInfo.InvariantCulture),
			double.Parse(components[3], CultureInfo.InvariantCulture));
	}
}
#endif
