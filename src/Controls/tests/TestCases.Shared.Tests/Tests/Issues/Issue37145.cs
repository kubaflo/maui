#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue37145 : _IssuesUITest
{
	public Issue37145(TestDevice device) : base(device) { }

	public override string Issue => "RadioButton border does not clear when BorderColor is reset";

	[Test]
	[Category(UITestCategories.RadioButton)]
	public void ResettingBorderColorClearsPreviouslyRenderedBorder()
	{
		App.SetOrientationPortrait();
		byte[] screenshot = App.Screenshot();
		Assert.That(screenshot, Has.Length.GreaterThan(24), "The portrait screenshot was not a valid PNG.");
		int screenshotWidth = ReadBigEndianInt32(screenshot, 16);
		int screenshotHeight = ReadBigEndianInt32(screenshot, 20);
		Assert.That(screenshotWidth, Is.LessThan(screenshotHeight), "The test device must be in portrait orientation.");

		var target = App.WaitForElement("Issue37145RadioButton").GetRect();
		Assert.Multiple(() =>
		{
			Assert.That(target.Width, Is.GreaterThan(0), "The intended RadioButton must have a nonempty width.");
			Assert.That(target.Height, Is.GreaterThan(0), "The intended RadioButton must have a nonempty height.");
			Assert.That(target.X, Is.GreaterThanOrEqualTo(24), "The RadioButton must retain the reported padded location.");
			Assert.That(target.Y, Is.GreaterThan(24), "The RadioButton must appear below the heading.");
		});

		var initial = WaitForMeasurement(0);
		AssertMeasurementMatchesTarget(initial, target);
		Assert.That(initial.RedBorderPixels, Is.GreaterThan(0), "The initial red RadioButton border was not rendered.");

		App.Tap("Issue37145Button");
		var firstReset = WaitForMeasurement(1);
		Assert.That(App.WaitForElement("Issue37145ApiState").GetText(), Is.EqualTo("BorderColor API state: null"));
		Assert.That(firstReset.Identity, Is.EqualTo(initial.Identity), "The native RadioButton was replaced during the first reset.");

		App.Tap("Issue37145Button");
		var blue = WaitForMeasurement(2);
		Assert.That(App.WaitForElement("Issue37145ApiState").GetText(), Is.EqualTo("BorderColor API state: Blue"));
		Assert.That(blue.Identity, Is.EqualTo(initial.Identity), "The native RadioButton was replaced while applying blue.");
		Assert.That(blue.BlueBorderPixels, Is.GreaterThan(0), "The blue RadioButton border was not rendered.");

		App.Tap("Issue37145Button");
		var finalReset = WaitForMeasurement(3);
		Assert.That(App.WaitForElement("Issue37145ApiState").GetText(), Is.EqualTo("BorderColor API state: null"));
		Assert.That(finalReset.Identity, Is.EqualTo(initial.Identity), "The native RadioButton was replaced during the final reset.");
		AssertMeasurementMatchesTarget(finalReset, target);
		Assert.That(finalReset.BlueBorderPixels, Is.EqualTo(0),
			$"BorderColor reset to null retained blue border pixels: {finalReset.BlueBorderPixels}; expected 0.");
	}

	Measurement WaitForMeasurement(int generation)
	{
		var element = App.WaitForElement(() =>
		{
			var candidate = App.FindElement("Issue37145Result");
			string text = candidate?.GetText() ?? string.Empty;
			return text.StartsWith($"generation={generation};identity=", StringComparison.Ordinal)
				? candidate
				: null;
		}, $"Timed out waiting for native render generation {generation}");

		string measurementText = element.GetText()
			?? throw new InvalidOperationException($"Native render generation {generation} returned no measurement text.");
		return Measurement.Parse(measurementText);
	}

	static void AssertMeasurementMatchesTarget(Measurement measurement, System.Drawing.Rectangle target)
	{
		Assert.Multiple(() =>
		{
			Assert.That(measurement.Width, Is.GreaterThan(0), "The measured native RadioButton width must be nonempty.");
			Assert.That(measurement.Height, Is.GreaterThan(0), "The measured native RadioButton height must be nonempty.");
			Assert.That(measurement.X, Is.EqualTo(target.X).Within(2), "The native measurement must belong to the intended RadioButton.");
			Assert.That(measurement.Y, Is.EqualTo(target.Y).Within(2), "The native measurement must be at the intended RadioButton location.");
		});
	}

	static int ReadBigEndianInt32(byte[] bytes, int offset) =>
		(bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];

	readonly record struct Measurement(int Identity, int X, int Y, int Width, int Height, int RedBorderPixels, int BlueBorderPixels)
	{
		public static Measurement Parse(string value)
		{
			var fields = value.Split(';')
				.Select(part => part.Split('=', 2))
				.ToDictionary(parts => parts[0], parts => int.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture));

			return new Measurement(
				fields["identity"],
				fields["x"],
				fields["y"],
				fields["width"],
				fields["height"],
				fields["redBorderPixels"],
				fields["blueBorderPixels"]);
		}
	}
}
#endif
