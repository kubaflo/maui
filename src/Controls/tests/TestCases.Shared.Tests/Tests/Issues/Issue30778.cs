using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue30778 : _IssuesUITest
{
	public override string Issue => "Setting a bound GraphicsView Shadow throws on Windows";

	public Issue30778(TestDevice device)
		: base(device)
	{
	}

#if WINDOWS
	[Test]
	[Category(UITestCategories.GraphicsView)]
	public void BoundGraphicsViewShadowUpdatesWithoutException()
	{
		const string input = "5,5,10,0.5";
		const string expectedState = "Applied Offset=5,5;Radius=10;Opacity=0.5;Exception=none";

		var cultureState = App.WaitForElement("CultureState").GetText();
		if (cultureState is null)
			Assert.Fail("The culture diagnostic was null.");

		Assert.That(cultureState, Is.EqualTo("Culture=en-US"));
		var graphicsViewAttached = App.WaitForTextToBePresentInElement(
			"GraphicsViewHandlerState",
			"HandlerReady",
			TimeSpan.FromSeconds(5));
		Assert.That(graphicsViewAttached, Is.True, "The bound GraphicsView did not load with a handler.");

		var initialShadowState = App.WaitForElement("InitialShadowUpdateState").GetText();
		var initialCallbackCount = App.WaitForElement("InitialCallbackCount").GetText();
		if (initialShadowState is null || initialCallbackCount is null)
			Assert.Fail("The initial shadow diagnostics were null.");

		Assert.That(initialShadowState, Is.EqualTo("NotStarted"));
		Assert.That(initialCallbackCount, Is.EqualTo("-1"));

		App.Tap("OptionsButton");
		App.WaitForElement("TriangleOption");
		App.WaitForElement("ShadowInputEntry");

		var optionsShadowState = App.WaitForElement("ShadowUpdateState").GetText();
		var optionsCallbackCount = App.WaitForElement("CallbackCountState").GetText();
		if (optionsShadowState is null || optionsCallbackCount is null)
			Assert.Fail("The options-page shadow diagnostics were null.");

		Assert.That(optionsShadowState, Is.EqualTo("NotStarted"));
		Assert.That(optionsCallbackCount, Is.EqualTo("-1"));

		App.Tap("TriangleOption");

		var triangleSelected = App.WaitForTextToBePresentInElement(
			"TriangleSelectionState",
			"Selected",
			TimeSpan.FromSeconds(5));
		Assert.That(triangleSelected, Is.True, "The triangle drawable selection was not observed.");

		App.EnterText("ShadowInputEntry", input);

		var completeInputObserved = App.WaitForTextToBePresentInElement(
			"ObservedInputState",
			input,
			TimeSpan.FromSeconds(5));
		Assert.That(completeInputObserved, Is.True, "The complete shadow input was not observed.");

		var callbackCountText = App.WaitForElement("CallbackCountState").GetText();
		if (callbackCountText is null)
			Assert.Fail("The shadow input callback count was null.");

		Assert.That(int.TryParse(callbackCountText, out var callbackCount), Is.True);
		Assert.That(callbackCount, Is.GreaterThanOrEqualTo(0));

		var observedInput = App.WaitForElement("ObservedInputState").GetText();
		var actualState = App.WaitForElement("ShadowUpdateState").GetText();
		if (observedInput is null || actualState is null)
			Assert.Fail("The final shadow diagnostics were null.");

		Assert.That(
			actualState,
			Is.EqualTo(expectedState),
			$"Bound GraphicsView Shadow update did not complete without exception. Input={input}; CallbackCount={callbackCountText}; ObservedInput={observedInput}; State={actualState}; Expected={expectedState}");
	}
#endif
}
