#if WINDOWS
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue36629 : _IssuesUITest
{
	public override string Issue => "SearchHandler font properties are not applied on Windows";

	public Issue36629(TestDevice testDevice) : base(testDevice)
	{
	}

	[Test]
	[Category(UITestCategories.Shell)]
	public void SearchHandlerFontPropertiesUpdateNativeSearchBox()
	{
		App.WaitForElement("TextBox");
		App.WaitForElement("FontSizeButton");
		App.WaitForElement("FontFamilyButton");
		App.WaitForElement("VerticalTextAlignmentButton");
		App.WaitForElement("FontAttributesButton");

		Assert.That(
			ReadMeasurement("FontSizeMeasurement"),
			Is.EqualTo("sequence=0;managed=unmeasured;native=unmeasured"));

		App.Tap("FontSizeButton");
		Assert.That(App.WaitForTextToBePresentInElement("FontSizeMeasurement", "sequence=1;"), Is.True);
		var fontSizeMeasurement = ReadMeasurement("FontSizeMeasurement");

		App.Tap("FontFamilyButton");
		Assert.That(App.WaitForTextToBePresentInElement("FontFamilyMeasurement", "sequence=2;"), Is.True);
		var fontFamilyMeasurement = ReadMeasurement("FontFamilyMeasurement");

		App.Tap("VerticalTextAlignmentButton");
		Assert.That(App.WaitForTextToBePresentInElement("VerticalAlignmentMeasurement", "sequence=3;"), Is.True);
		var verticalAlignmentMeasurement = ReadMeasurement("VerticalAlignmentMeasurement");

		App.Tap("FontAttributesButton");
		Assert.That(App.WaitForTextToBePresentInElement("FontAttributesMeasurement", "sequence=4;"), Is.True);
		var fontAttributesMeasurement = ReadMeasurement("FontAttributesMeasurement");

		var managedFontSize = double.Parse(GetMeasurementValue(fontSizeMeasurement, "managed"), CultureInfo.InvariantCulture);
		var nativeFontSize = double.Parse(GetMeasurementValue(fontSizeMeasurement, "native"), CultureInfo.InvariantCulture);
		var managedFontFamily = GetMeasurementValue(fontFamilyMeasurement, "managed");
		var nativeFontFamily = GetMeasurementValue(fontFamilyMeasurement, "native");
		var managedVerticalAlignment = GetMeasurementValue(verticalAlignmentMeasurement, "managed");
		var nativeVerticalAlignment = GetMeasurementValue(verticalAlignmentMeasurement, "native");
		var managedFontAttributes = GetMeasurementValue(fontAttributesMeasurement, "managed");
		var nativeFontWeight = ushort.Parse(GetMeasurementValue(fontAttributesMeasurement, "native"), CultureInfo.InvariantCulture);
		var expectedNativeVerticalAlignment = managedVerticalAlignment == "End" ? "Bottom" : "Center";
		var expectedNativeFontWeight = managedFontAttributes == "Bold" ? (ushort)700 : (ushort)400;

		Assert.Multiple(() =>
		{
			Assert.That(managedFontSize, Is.EqualTo(30));
			Assert.That(nativeFontSize, Is.EqualTo(managedFontSize).Within(0.01),
				"SearchHandler FontSize was not applied to the native AutoSuggestBox after the FontSize button was tapped.");
			Assert.That(managedFontFamily, Is.EqualTo("OpenSansRegular"));
			Assert.That(nativeFontFamily, Does.Contain("OpenSans-Regular.ttf"),
				"SearchHandler FontFamily was not applied to the native AutoSuggestBox after the FontFamily button was tapped.");
			Assert.That(managedVerticalAlignment, Is.EqualTo("End"));
			Assert.That(nativeVerticalAlignment, Is.EqualTo(expectedNativeVerticalAlignment),
				"SearchHandler VerticalTextAlignment was not applied to the native AutoSuggestBox after the VerticalTextAlignment button was tapped.");
			Assert.That(managedFontAttributes, Is.EqualTo("Bold"));
			Assert.That(nativeFontWeight, Is.GreaterThanOrEqualTo(expectedNativeFontWeight),
				"SearchHandler FontAttributes was not applied to the native AutoSuggestBox after the FontAttributes button was tapped.");
		});
	}

	string ReadMeasurement(string automationId)
	{
		var measurement = App.WaitForElement(automationId).GetText();
		if (measurement is null)
		{
			Assert.Fail($"Measurement element '{automationId}' did not expose text.");
			return string.Empty;
		}

		return measurement;
	}

	static string GetMeasurementValue(string measurement, string key)
	{
		var prefix = $"{key}=";
		foreach (var part in measurement.Split(';'))
		{
			if (part.StartsWith(prefix, StringComparison.Ordinal))
			{
				return part[prefix.Length..];
			}
		}

		Assert.Fail($"Measurement '{measurement}' did not contain '{key}'.");
		return string.Empty;
	}
}
#endif
