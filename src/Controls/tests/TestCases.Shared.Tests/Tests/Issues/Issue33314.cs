#if WINDOWS
using System.Globalization;
using NUnit.Framework;
using OpenQA.Selenium;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue33314 : _IssuesUITest
{
	public Issue33314(TestDevice device)
		: base(device)
	{
	}

	public override string Issue => "Editor caret collapses after clearing text while hiding adjacent content";

	[Test]
	[Category(UITestCategories.Editor)]
	public void ClearedEditorRetainsNativeContentLineHeight()
	{
		var editor = App.WaitForElement("Issue33314Editor");
		Assert.That(editor, Is.Not.Null);
		editor.Tap();
		editor.SendKeys("Caret test");

		Assert.That(
			App.WaitForTextToBePresentInElement("Issue33314Measurement", "Phase=Clean", TimeSpan.FromSeconds(10)),
			Is.True,
			"The clean native Editor measurement was not reported.");
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue33314Measurement", "TextLength=10", TimeSpan.FromSeconds(10)),
			Is.True,
			"The clean measurement did not observe the entered Editor text.");

		var cleanMeasurementElement = App.WaitForElement("Issue33314Measurement");
		Assert.That(cleanMeasurementElement, Is.Not.Null);
		var cleanMeasurementText = cleanMeasurementElement.GetText();
		if (cleanMeasurementText is null)
		{
			throw new AssertionException("The clean native Editor measurement was null.");
		}

		Measurement clean = ParseMeasurement(cleanMeasurementText);
		Assert.That(clean.TextLength, Is.EqualTo(10));
		Assert.That(clean.CancelVisible, Is.True);
		Assert.That(clean.FontSize, Is.GreaterThan(0));
		Assert.That(clean.Height, Is.GreaterThanOrEqualTo(clean.FontSize - 0.5),
			$"The clean Editor content height {clean.Height} should be at least its active font size {clean.FontSize}.");

		editor.SendKeys(Keys.Shift);

		Assert.That(
			App.WaitForTextToBePresentInElement("Issue33314KeyDownToken", "KeyDown=1", TimeSpan.FromSeconds(10)),
			Is.True,
			"The native TextBox KeyDown handler did not receive Shift.");
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue33314Measurement", "Phase=Post", TimeSpan.FromSeconds(10)),
			Is.True,
			"The post-Shift native Editor measurement was not reported.");
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue33314Measurement", "TextLength=0", TimeSpan.FromSeconds(10)),
			Is.True,
			"The Shift handler did not clear the Editor text.");

		var postMeasurementElement = App.WaitForElement("Issue33314Measurement");
		Assert.That(postMeasurementElement, Is.Not.Null);
		var postMeasurementText = postMeasurementElement.GetText();
		if (postMeasurementText is null)
		{
			throw new AssertionException("The post-Shift native Editor measurement was null.");
		}

		Measurement post = ParseMeasurement(postMeasurementText);
		Assert.That(post.Sequence, Is.GreaterThan(clean.Sequence));
		Assert.That(post.TextBoxId, Is.EqualTo(clean.TextBoxId));
		Assert.That(post.ContentId, Is.EqualTo(clean.ContentId));
		Assert.That(post.TextLength, Is.Zero);
		Assert.That(post.CancelVisible, Is.False);
		Assert.That(post.FontSize, Is.EqualTo(clean.FontSize).Within(0.01));
		Assert.That(post.Height, Is.GreaterThanOrEqualTo(post.FontSize - 0.5),
			$"Issue33314 cleared Editor native content height collapsed after Shift: measured {post.Height}, minimum {post.FontSize - 0.5}.");
	}

	static Measurement ParseMeasurement(string value)
	{
		string[] parts = value.Split(';');
		Assert.That(parts, Has.Length.EqualTo(8), $"Unexpected measurement payload: {value}");

		return new Measurement(
			ParseInt(parts[1], "Sequence="),
			ParseInt(parts[2], "TextBoxId="),
			ParseInt(parts[3], "ContentId="),
			ParseInt(parts[4], "TextLength="),
			ParseDouble(parts[5], "FontSize="),
			ParseDouble(parts[6], "Height="),
			ParseBool(parts[7], "CancelVisible="));
	}

	static int ParseInt(string part, string prefix) =>
		int.Parse(RemovePrefix(part, prefix), CultureInfo.InvariantCulture);

	static double ParseDouble(string part, string prefix) =>
		double.Parse(RemovePrefix(part, prefix), CultureInfo.InvariantCulture);

	static bool ParseBool(string part, string prefix) =>
		bool.Parse(RemovePrefix(part, prefix));

	static string RemovePrefix(string part, string prefix)
	{
		Assert.That(part, Does.StartWith(prefix));
		return part[prefix.Length..];
	}

	readonly record struct Measurement(
		int Sequence,
		int TextBoxId,
		int ContentId,
		int TextLength,
		double FontSize,
		double Height,
		bool CancelVisible);
}
#endif
