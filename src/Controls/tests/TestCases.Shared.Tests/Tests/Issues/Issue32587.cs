#if WINDOWS
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue32587 : _IssuesUITest
{
	public Issue32587(TestDevice device) : base(device)
	{
	}

	public override string Issue => "ContentView inside CollectionView reports invalid bounds during gesture events";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void TemplatedContentViewHasValidBoundsDuringTap()
	{
		var target = App.WaitForElement("Issue32587GestureItem");
		if (target is null)
			throw new InvalidOperationException("The directly templated ContentView was not found.");

		var targetRect = target.GetRect();
		Assert.That(targetRect.Width, Is.GreaterThan(0), "The directly templated ContentView should have a positive rendered width.");
		Assert.That(targetRect.Height, Is.GreaterThan(0), "The directly templated ContentView should have a positive rendered height.");

		var identity = App.WaitForElement("Issue32587ItemIdentity");
		if (identity is null)
			throw new InvalidOperationException("The expected CollectionView item identity was not found.");
		Assert.That(identity.GetText(), Is.EqualTo("Gesture item"));

		Assert.That(ReadText("Issue32587CallbackSequence"), Is.EqualTo("Callback sequence: -1"));
		Assert.That(ReadText("Issue32587CaptureState"), Is.EqualTo("Captured bounds: pending"));
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue32587GeometryState", "NativeWidth=", TimeSpan.FromSeconds(5)),
			Is.True,
			"The native and child geometry should be ready before the tap.");

		var geometry = ReadText("Issue32587GeometryState");
		var initialNativeWidth = ReadDimension(geometry, "NativeWidth");
		var initialNativeHeight = ReadDimension(geometry, "NativeHeight");
		var initialChildWidth = ReadDimension(geometry, "ChildWidth");
		var initialChildHeight = ReadDimension(geometry, "ChildHeight");
		Assert.That(initialNativeWidth, Is.GreaterThan(0), "The Windows handler should report a positive ActualWidth before the tap.");
		Assert.That(initialNativeHeight, Is.GreaterThan(0), "The Windows handler should report a positive ActualHeight before the tap.");
		Assert.That(initialChildWidth, Is.GreaterThan(0), "The child layout should report a positive width before the tap.");
		Assert.That(initialChildHeight, Is.GreaterThan(0), "The child layout should report a positive height before the tap.");

		App.Tap("Issue32587GestureItem");

		Assert.That(
			App.WaitForTextToBePresentInElement("Issue32587CallbackSequence", "Callback sequence: 1", TimeSpan.FromSeconds(5)),
			Is.True,
			"The tap callback should run exactly once.");
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue32587CaptureState", "NativeWidth=", TimeSpan.FromSeconds(5)),
			Is.True,
			"The tap callback should capture the bounds.");

		var capturedBounds = ReadText("Issue32587CaptureState");
		var managedWidth = ReadDimension(capturedBounds, "Width");
		var managedHeight = ReadDimension(capturedBounds, "Height");
		var nativeWidth = ReadDimension(capturedBounds, "NativeWidth");
		var nativeHeight = ReadDimension(capturedBounds, "NativeHeight");

		Assert.That(
			managedWidth > 0 && managedHeight > 0,
			Is.True,
			$"Templated ContentView bounds remained invalid after tap: managed={managedWidth}x{managedHeight}, native={nativeWidth}x{nativeHeight}.");
		Assert.That(managedWidth, Is.EqualTo(nativeWidth).Within(1),
			$"The ContentView width should match its rendered Windows native width. Managed={managedWidth}, native={nativeWidth}.");
		Assert.That(managedHeight, Is.EqualTo(nativeHeight).Within(1),
			$"The ContentView height should match its rendered Windows native height. Managed={managedHeight}, native={nativeHeight}.");
	}

	string ReadText(string automationId)
	{
		var element = App.WaitForElement(automationId);
		if (element is null)
			throw new InvalidOperationException($"Element '{automationId}' was not found.");

		var text = element.GetText();
		if (text is null)
			throw new InvalidOperationException($"Element '{automationId}' returned null text.");

		return text;
	}

	static double ReadDimension(string text, string name)
	{
		var prefix = $"{name}=";
		foreach (var part in text.Split(';', StringSplitOptions.TrimEntries))
		{
			var prefixIndex = part.IndexOf(prefix, StringComparison.Ordinal);
			if (prefixIndex >= 0)
				return double.Parse(part[(prefixIndex + prefix.Length)..], CultureInfo.InvariantCulture);
		}

		throw new InvalidOperationException($"Dimension '{name}' was not found in '{text}'.");
	}
}
#endif
