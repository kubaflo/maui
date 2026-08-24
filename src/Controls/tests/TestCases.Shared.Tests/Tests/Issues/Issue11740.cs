using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue11740 : _IssuesUITest
{
	public Issue11740(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "Binding does not respect Binding.DoNothing returned from IValueConverter";

#if ANDROID
	[Test]
	[Category(UITestCategories.Entry)]
	public void BindingDoNothingDoesNotUpdateEntryText()
	{
		var initialEntry = App.WaitForElement("ReproductionEntry");
		if (initialEntry is null)
			throw new InvalidOperationException("The reproduction Entry was not found.");

		Assert.That(initialEntry.GetText() ?? string.Empty, Is.Empty);

		var initialResult = App.WaitForElement("BindingResult");
		if (initialResult is null)
			throw new InvalidOperationException("The converter result label was not found.");

		Assert.That(initialResult.GetText(), Is.EqualTo("Converter calls: 0"));

		App.WaitForElement("ApplyBindingButton");
		App.Tap("ApplyBindingButton");
		Assert.That(
			App.WaitForTextToBePresentInElement("BindingResult", "returned Binding.DoNothing"),
			Is.True,
			"The converter did not report returning Binding.DoNothing.");

		var observedCallCount = -1;
		var result = App.FindElement("BindingResult");
		if (result is null)
			throw new InvalidOperationException("The converter result label disappeared after the binding was applied.");

		var resultText = result.GetText() ?? string.Empty;
		const string prefix = "Converter calls: ";
		const string suffix = "; returned Binding.DoNothing";
		if (resultText.StartsWith(prefix, StringComparison.Ordinal) &&
			resultText.EndsWith(suffix, StringComparison.Ordinal))
		{
			if (int.TryParse(resultText[prefix.Length..^suffix.Length], out var parsedCallCount))
				observedCallCount = parsedCallCount;
		}

		Assert.That(observedCallCount, Is.GreaterThan(0), "The converter callback was not observed after the binding trigger.");
		Assert.That(resultText, Does.EndWith(suffix), "The converter did not return Binding.DoNothing.");

		var updatedEntry = App.FindElement("ReproductionEntry");
		if (updatedEntry is null)
			throw new InvalidOperationException("The reproduction Entry disappeared after the binding was applied.");

		var actualText = updatedEntry.GetText() ?? string.Empty;
		Assert.That(
			actualText,
			Is.Empty,
			$"Binding.DoNothing should leave Entry.Text empty after binding; observed '{actualText}'.");
	}
#endif
}
