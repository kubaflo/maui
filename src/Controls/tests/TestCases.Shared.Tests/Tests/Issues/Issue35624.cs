#if IOS
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35624 : _IssuesUITest
{
	public Issue35624(TestDevice device) : base(device) { }

	public override string Issue => "[iOS] SearchHandler CharacterSpacing is not applied";

	[Test]
	[Category(UITestCategories.Shell)]
	public void SearchHandlerAppliesCharacterSpacingToNativeText()
	{
		App.WaitForElement("Issue35624ReferenceLabel");
		var initialStatus = App.WaitForElement("Issue35624Status").GetText();
		if (initialStatus is null)
		{
			Assert.Fail("The initial native kerning measurement payload was null.");
			return;
		}

		var initialSequence = ReadSequence(initialStatus);

		var searchHandler = App.GetShellSearchHandler();
		Assert.That(searchHandler, Is.Not.Null);
		searchHandler.Tap();
		searchHandler.SendKeys("ABCABC");

		Assert.That(
			App.WaitForTextToBePresentInElement("Issue35624Status", "Query=ABCABC", TimeSpan.FromSeconds(10)),
			Is.True,
			"The post-input native kerning measurement did not complete.");

		var status = App.WaitForElement("Issue35624Status").GetText();
		if (status is null)
		{
			Assert.Fail("The native kerning measurement payload was null.");
			return;
		}

		var measurements = status.Split(';');
		Assert.That(measurements, Has.Length.EqualTo(5), $"Unexpected native measurement payload: {status}");

		Assert.That(ReadSequence(status), Is.GreaterThan(initialSequence), "A post-input native kerning measurement was not observed.");
		Assert.That(measurements[1], Is.EqualTo("Query=ABCABC"), "SearchHandler Query should reflect the Appium-entered text.");
		Assert.That(measurements[2], Is.EqualTo("NativeText=ABCABC"), "The native iOS search field should contain the Appium-entered text.");

		var expectedKerning = 12d;
		var referenceKerning = double.Parse(measurements[3]["ReferenceKerning=".Length..], CultureInfo.InvariantCulture);
		var searchKerning = double.Parse(measurements[4]["SearchKerning=".Length..], CultureInfo.InvariantCulture);

		Assert.That(
			referenceKerning,
			Is.EqualTo(expectedKerning).Within(0.01),
			$"Reference Label native kerning mismatch: expected {expectedKerning}, measured {referenceKerning}.");
		Assert.That(
			searchKerning,
			Is.EqualTo(expectedKerning).Within(0.01),
			$"SearchHandler native kerning mismatch: expected {expectedKerning}, measured {searchKerning}.");
	}

	static int ReadSequence(string status)
	{
		var sequence = status.Split(';', 2)[0];
		Assert.That(sequence, Does.StartWith("Sequence="), $"Unexpected native measurement payload: {status}");
		return int.Parse(sequence["Sequence=".Length..], CultureInfo.InvariantCulture);
	}
}
#endif

