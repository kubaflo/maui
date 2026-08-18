#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35301 : _IssuesUITest
{
	public Issue35301(TestDevice device)
		: base(device)
	{
	}

	public override string Issue => "Windows CollectionView applies WinUI styling by default";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void DefaultSelectionDoesNotAddWinUIPill()
	{
		const string readyState = "Native ready: Apple[0], Banana[1], Cherry[2]; Apple selected: False";
		const string selectedState = "Selected: Apple; Callback: True; Native index: 0; IsSelected: True";
		const string cornerPrefix = "Corner: ";
		const string indicatorSeparator = "; Indicator: ";

		App.WaitForElement("Issue35301NativeState");
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue35301NativeState", readyState),
			Is.True,
			"Native Apple, Banana, and Cherry containers did not become ready.");
		Assert.That(App.WaitForElement("Issue35301NativeState").GetText(), Is.EqualTo(readyState));
		Assert.That(App.WaitForElement("Issue35301SelectionState").GetText(), Is.EqualTo("Selected: none"));
		App.WaitForElement("Apple");
		App.WaitForElement("Banana");
		App.WaitForElement("Cherry");

		App.Tap("Apple");
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue35301SelectionState", selectedState),
			Is.True,
			"Pointer selection did not produce the Apple selection callback and native selected state.");

		var nativeState = App.WaitForElement("Issue35301NativeState").GetText()!;
		var separatorIndex = nativeState.IndexOf(indicatorSeparator, StringComparison.Ordinal);
		Assert.That(nativeState, Does.StartWith(cornerPrefix));
		Assert.That(separatorIndex, Is.GreaterThan(cornerPrefix.Length));

		var cornerValue = nativeState.Substring(cornerPrefix.Length, separatorIndex - cornerPrefix.Length);
		var indicatorValue = nativeState.Substring(separatorIndex + indicatorSeparator.Length);

		Assert.Multiple(() =>
		{
			Assert.That(
				cornerValue,
				Is.EqualTo("CornerRadius(0,0,0,0)"),
				$"Rounded-corner suppression for selected Apple was {cornerValue}; expected CornerRadius(0,0,0,0).");
			Assert.That(
				indicatorValue,
				Is.EqualTo("False"),
				$"Selection-indicator suppression for selected Apple was {indicatorValue}; expected False.");
		});
	}
}
#endif
