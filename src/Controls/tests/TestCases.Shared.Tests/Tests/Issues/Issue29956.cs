#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue29956 : _IssuesUITest
{
	const double MinimumRedCoverage = 0.6;

	public Issue29956(TestDevice device) : base(device) { }

	public override string Issue => "[Android] ImageButton border is incomplete with AspectFill";

	[Test]
	[Category(UITestCategories.ImageButton)]
	public void AspectFillPreservesImageButtonBorderOnAllSides()
	{
		App.SetOrientationPortrait();

		var root = App.WaitForElement("Issue29956Root");
		Assert.That(root, Is.Not.Null);
		var rootRect = root!.GetRect();
		Assert.That(rootRect.Height, Is.GreaterThan(rootRect.Width), "The test viewport must be portrait.");

		var image = App.WaitForElement("AffectedImageButton");
		Assert.That(image, Is.Not.Null);
		var imageRect = image!.GetRect();
		Assert.That(imageRect.X, Is.GreaterThanOrEqualTo(rootRect.X));
		Assert.That(imageRect.Y, Is.GreaterThanOrEqualTo(rootRect.Y));
		Assert.That(imageRect.X + imageRect.Width, Is.LessThanOrEqualTo(rootRect.X + rootRect.Width));
		Assert.That(imageRect.Y + imageRect.Height, Is.LessThanOrEqualTo(rootRect.Y + rootRect.Height));

		Assert.That(App.WaitForTextToBePresentInElement("InitialMeasurement", "Generation=0"), Is.True);
		var initial = ReadMeasurement("InitialMeasurement");
		Assert.That(initial.Phase, Is.EqualTo("AspectFit"));
		Assert.That(initial.WidthDip, Is.EqualTo(280).Within(2), "The native view must correspond to the requested 280-unit width.");
		Assert.That(initial.HeightDip, Is.EqualTo(240).Within(2), "The native view must correspond to the requested 240-unit height.");
		Assert.That(initial.NativeWidth, Is.EqualTo(imageRect.Width).Within(2), "The measured native view must be the intended ImageButton.");
		Assert.That(initial.NativeHeight, Is.EqualTo(imageRect.Height).Within(2), "The measured native view must be the intended ImageButton.");
		Assert.That(initial.NativeX, Is.EqualTo(imageRect.X).Within(2), "The native measurement must come from the visible ImageButton location.");
		Assert.That(initial.NativeY, Is.EqualTo(imageRect.Y).Within(2), "The native measurement must come from the visible ImageButton location.");
		Assert.That(initial.DrawableLoaded, Is.True, "The file-backed native drawable must be loaded.");
		AssertBorderCoverage(initial, "AspectFit");

		App.Tap("ApplyAspectFillButton");
		Assert.That(App.WaitForTextToBePresentInElement("CurrentAspectLabel", "Current aspect: AspectFill"), Is.True);
		Assert.That(App.WaitForTextToBePresentInElement("PostMeasurement", "Generation=1"), Is.True);

		var post = ReadMeasurement("PostMeasurement");
		Assert.That(post.Phase, Is.EqualTo("AspectFill"));
		Assert.That(post.Generation, Is.GreaterThan(initial.Generation), "A new native render measurement must follow the tap.");
		Assert.That(post.DrawableLoaded, Is.True, "The file-backed native drawable must remain loaded.");
		var borderIsComplete =
			post.TopCoverage >= MinimumRedCoverage &&
			post.BottomCoverage >= MinimumRedCoverage &&
			post.LeftCoverage >= MinimumRedCoverage &&
			post.RightCoverage >= MinimumRedCoverage;
		Assert.That(borderIsComplete, Is.True,
			$"ImageButton AspectFill border is incomplete: top={post.Top}; bottom={post.Bottom}; left={post.Left}; right={post.Right}.");
	}

	Measurement ReadMeasurement(string automationId)
	{
		var element = App.FindElement(automationId);
		Assert.That(element, Is.Not.Null);
		var text = element!.GetText();
		Assert.That(text, Is.Not.Null.And.Not.Empty);
		return Measurement.Parse(text!);
	}

	static void AssertBorderCoverage(Measurement measurement, string phase)
	{
		Assert.That(measurement.TopCoverage, Is.GreaterThanOrEqualTo(MinimumRedCoverage), $"{phase} top border oracle must be red: top={measurement.Top}.");
		Assert.That(measurement.BottomCoverage, Is.GreaterThanOrEqualTo(MinimumRedCoverage), $"{phase} bottom border oracle must be red: bottom={measurement.Bottom}.");
		Assert.That(measurement.LeftCoverage, Is.GreaterThanOrEqualTo(MinimumRedCoverage), $"{phase} left border oracle must be red: left={measurement.Left}.");
		Assert.That(measurement.RightCoverage, Is.GreaterThanOrEqualTo(MinimumRedCoverage), $"{phase} right border oracle must be red: right={measurement.Right}.");
	}

	readonly record struct Measurement(
		int Generation,
		string Phase,
		int NativeWidth,
		int NativeHeight,
		int WidthDip,
		int HeightDip,
		int NativeX,
		int NativeY,
		bool DrawableLoaded,
		BorderSample Top,
		BorderSample Bottom,
		BorderSample Left,
		BorderSample Right)
	{
		public double TopCoverage => Top.Coverage;
		public double BottomCoverage => Bottom.Coverage;
		public double LeftCoverage => Left.Coverage;
		public double RightCoverage => Right.Coverage;

		public static Measurement Parse(string text)
		{
			var values = text.Split(';')
				.Select(part => part.Split('=', 2))
				.ToDictionary(parts => parts[0], parts => parts[1], StringComparer.Ordinal);

			Assert.That(values.ContainsKey("Error"), Is.False, $"Native measurement failed: {text}");
			return new Measurement(
				int.Parse(values["Generation"]),
				values["Phase"],
				int.Parse(values["Width"]),
				int.Parse(values["Height"]),
				int.Parse(values["WidthDip"]),
				int.Parse(values["HeightDip"]),
				int.Parse(values["X"]),
				int.Parse(values["Y"]),
				bool.Parse(values["Drawable"]),
				BorderSample.Parse(values["Top"]),
				BorderSample.Parse(values["Bottom"]),
				BorderSample.Parse(values["Left"]),
				BorderSample.Parse(values["Right"]));
		}
	}

	readonly record struct BorderSample(int Red, int Total)
	{
		public double Coverage => (double)Red / Total;

		public static BorderSample Parse(string value)
		{
			var counts = value.Split('/');
			Assert.That(counts.Length, Is.EqualTo(2));
			var red = int.Parse(counts[0]);
			var total = int.Parse(counts[1]);
			Assert.That(total, Is.GreaterThan(0), "The native border sample must contain pixels.");
			return new BorderSample(red, total);
		}

		public override string ToString() => $"{Red}/{Total}";
	}
}
#endif
