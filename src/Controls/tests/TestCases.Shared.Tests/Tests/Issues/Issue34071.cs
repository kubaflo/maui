#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue34071 : _IssuesUITest
{
	public override string Issue => "The Shell foreground color is not applied to ToolbarItems";

	public Issue34071(TestDevice device) : base(device) { }

	[Test]
	[Category(UITestCategories.Shell)]
	public void ShellForegroundColorAppliesToToolbarItemIcon()
	{
		var completion = App.WaitForElement(() =>
		{
			var element = App.FindElement("MeasurementResult");
			return element?.GetText()?.StartsWith("complete=1", StringComparison.Ordinal) == true ? element : null;
		}, "Timed out waiting for the rendered toolbar color measurement");

		string measurement = completion.GetText()
			?? throw new InvalidOperationException("The completed toolbar color measurement must contain text");
		Assert.That(GetMeasurement("complete"), Is.EqualTo("1"), "The post-render measurement callback did not complete");
		Assert.That(GetMeasurement("source"), Is.EqualTo("shopping_cart.png"));
		Assert.That(GetMeasurement("foreground"), Is.EqualTo("Purple"));
		Assert.That(GetMeasurement("toolbar"), Is.EqualTo("ToolbarColorItem"));

		int iconWidth = int.Parse(GetMeasurement("iconWidth"));
		int iconHeight = int.Parse(GetMeasurement("iconHeight"));
		int referenceWidth = int.Parse(GetMeasurement("referenceWidth"));
		int referenceHeight = int.Parse(GetMeasurement("referenceHeight"));
		int iconOpaquePixels = int.Parse(GetMeasurement("iconOpaque"));
		int referenceOpaquePixels = int.Parse(GetMeasurement("referenceOpaque"));
		int referencePurplePixels = int.Parse(GetMeasurement("referencePurple"));
		int iconPurplePixels = int.Parse(GetMeasurement("iconPurple"));

		Assert.That(iconWidth, Is.GreaterThan(0), "The native toolbar icon rendered width must be positive");
		Assert.That(iconHeight, Is.GreaterThan(0), "The native toolbar icon rendered height must be positive");
		Assert.That(referenceWidth, Is.GreaterThan(0), "The native reference rendered width must be positive");
		Assert.That(referenceHeight, Is.GreaterThan(0), "The native reference rendered height must be positive");
		Assert.That(iconOpaquePixels, Is.GreaterThan(0), "The packaged toolbar image must be present in the rendered icon surface");
		Assert.That(referenceOpaquePixels, Is.GreaterThan(0), "The native purple reference surface must contain rendered content");
		Assert.That(referencePurplePixels, Is.GreaterThan(0), "The purple-pixel detector must recognize the rendered reference");
		Assert.That(iconPurplePixels, Is.GreaterThanOrEqualTo(1),
			$"expected at least 1 purple icon pixel after Shell.ForegroundColor Purple; measured icon={iconPurplePixels}, reference={referencePurplePixels}");

		string GetMeasurement(string key)
		{
			foreach (string part in measurement.Split('|'))
			{
				int separator = part.IndexOf('=', StringComparison.Ordinal);
				if (separator > 0 && part[..separator] == key)
					return part[(separator + 1)..];
			}

			return string.Empty;
		}
	}
}
#endif
