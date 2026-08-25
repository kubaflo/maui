#if ANDROID
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue28542 : _IssuesUITest
{
	const string ExpectedIdentities = "Short item 1,Short item 2,Short item 3,Short item 4,Short item 5,Short item 6,Short item 7,Short item 8,Tall item 9,Tall item 10,Tall item 11,Tall item 12,Tall item 13,Tall item 14,Tall item 15,Tall item 16";
	const string FailureSignature = "CollectionView scrollbar thumb height must match the value derived from the arranged 1,888-DIP content after scrolling from short to tall items;";

	public Issue28542(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "CollectionView scrollbar has incorrect sizing for variable-height items";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void ScrollbarThumbUsesTotalVariableHeightContent()
	{
		App.SetOrientationPortrait();
		App.WaitForElement("VariableHeightCollection");
		App.WaitForElement("Short item 6");

		var first = CaptureMetrics("CaptureMetrics", 0);
		var second = CaptureMetrics("CaptureMetrics", 1);

		Assert.That(first.Sequence, Is.GreaterThan(-1));
		Assert.That(second.Sequence, Is.GreaterThan(first.Sequence));
		Assert.That(second.Extent, Is.EqualTo(first.Extent).Within(1));
		Assert.That(second.Range, Is.EqualTo(first.Range).Within(1));
		Assert.That(second.Thumb, Is.EqualTo(first.Thumb).Within(1));
		Assert.That(second.Offset, Is.EqualTo(first.Offset));
		Assert.That((second.First, second.Last), Is.EqualTo((first.First, first.Last)));

		var shortItem = App.WaitForElement("Short item 6");
		var root = App.WaitForElement("RootGrid");
		var collection = App.WaitForElement("VariableHeightCollection");
		var startX = shortItem.GetRect().CenterX();
		var startY = shortItem.GetRect().CenterY();
		var endY = MathF.Max(collection.GetRect().Y + 20, startY - (root.GetRect().Height * 0.65f));

		App.DragCoordinates(startX, startY, startX, endY);
		App.WaitForElement("Tall item 9");

		var post = CaptureMetrics("CheckMetrics", 2);

		Assert.That(post.Offset, Is.GreaterThan(second.Offset));
		Assert.That((post.First, post.Last), Is.Not.EqualTo((second.First, second.Last)));
		Assert.That(post.First, Is.LessThanOrEqualTo(8));
		Assert.That(post.Last, Is.GreaterThanOrEqualTo(8));
		Assert.That(post.Attached, Is.EqualTo(1));
		Assert.That(post.NativeCount, Is.EqualTo(16));
		Assert.That(post.ManagedCount, Is.EqualTo(16));
		Assert.That(post.ShortHeight, Is.EqualTo(56));
		Assert.That(post.TallHeight, Is.EqualTo(180));
		Assert.That(post.Items, Is.EqualTo(ExpectedIdentities));
		Assert.That(post.Extent, Is.GreaterThan(0));
		Assert.That(post.Extent, Is.EqualTo(second.Extent).Within(1));
		Assert.That(post.Range, Is.GreaterThan(0));
		Assert.That(post.Density, Is.GreaterThan(0));

		var expectedRange = (int)Math.Round(((8 * 56) + (8 * 180)) * post.Density);
		var expectedThumb = (int)Math.Round((double)post.Extent * post.Extent / expectedRange);
		var rangeTolerance = post.NativeCount;
		const int thumbTolerance = 1;
		var rangeMatches = Math.Abs(post.Range - expectedRange) <= rangeTolerance;
		var thumbMatches = Math.Abs(post.Thumb - expectedThumb) <= thumbTolerance;
		var thumbStayedFixed = Math.Abs(post.Thumb - second.Thumb) <= thumbTolerance;

		Assert.That(
			rangeMatches && thumbMatches && thumbStayedFixed,
			Is.True,
			$"{FailureSignature} pre extent={second.Extent}, range={second.Range}, offset={second.Offset}, thumb={second.Thumb}, positions={second.First}-{second.Last}; post extent={post.Extent}, range={post.Range}, offset={post.Offset}, thumb={post.Thumb}, density={post.Density.ToString(CultureInfo.InvariantCulture)}, positions={post.First}-{post.Last}; expected range={expectedRange} +/- {rangeTolerance}, thumb={expectedThumb} +/- {thumbTolerance}.");
	}

	ScrollMetrics CaptureMetrics(string buttonId, int expectedSequence)
	{
		App.Tap(buttonId);
		var captureCompleted = App.WaitForTextToBePresentInElement("ScrollbarMetrics", $"sequence={expectedSequence};");
		Assert.That(captureCompleted, Is.True, $"Native metrics capture sequence {expectedSequence} did not complete.");

		var text = App.WaitForElement("ScrollbarMetrics").GetText();
		if (string.IsNullOrEmpty(text))
			throw new InvalidOperationException("The native scrollbar metrics label was empty.");

		var metrics = ParseMetrics(text);
		Assert.That(metrics.Sequence, Is.EqualTo(expectedSequence));
		return metrics;
	}

	static ScrollMetrics ParseMetrics(string text)
	{
		var values = text.Split(';', StringSplitOptions.RemoveEmptyEntries)
			.Select(part => part.Split('=', 2))
			.ToDictionary(parts => parts[0], parts => parts[1], StringComparer.Ordinal);

		return new ScrollMetrics(
			ParseInt(values, "sequence"),
			ParseInt(values, "extent"),
			ParseInt(values, "range"),
			ParseInt(values, "offset"),
			ParseInt(values, "first"),
			ParseInt(values, "last"),
			double.Parse(values["density"], CultureInfo.InvariantCulture),
			ParseInt(values, "nativeCount"),
			ParseInt(values, "managedCount"),
			ParseInt(values, "attached"),
			ParseInt(values, "shortHeight"),
			ParseInt(values, "tallHeight"),
			ParseInt(values, "thumb"),
			values["items"]);
	}

	static int ParseInt(IReadOnlyDictionary<string, string> values, string key) =>
		int.Parse(values[key], CultureInfo.InvariantCulture);

	sealed record ScrollMetrics(
		int Sequence,
		int Extent,
		int Range,
		int Offset,
		int First,
		int Last,
		double Density,
		int NativeCount,
		int ManagedCount,
		int Attached,
		int ShortHeight,
		int TallHeight,
		int Thumb,
		string Items);
}
#endif
