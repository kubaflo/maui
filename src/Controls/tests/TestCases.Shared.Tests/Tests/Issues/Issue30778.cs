#if WINDOWS
using System.Linq;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue30778 : _IssuesUITest
{
	public Issue30778(TestDevice device) : base(device)
	{
	}

	public override string Issue => "COMException thrown when setting Shadow via data binding on Windows";

	[Test]
	[Category(UITestCategories.GraphicsView)]
	public void BoundShadowCanBeUpdatedAfterNavigation()
	{
		App.WaitForElement("Options");
		App.Tap("Options");

		App.WaitForElement("Triangle");
		App.WaitForNoElement("Options");

		var initialResult = App.WaitForElement("Result").GetText();
		if (initialResult is null)
			throw new AssertionException("The initial shadow diagnostic result was null.");

		Assert.That(initialResult, Does.Contain("Exception=;"));
		Assert.That(initialResult, Does.Contain("Sequence=-1"));
		Assert.That(initialResult, Does.Contain("Drawable=Square"));

		App.Tap("Triangle");
		Assert.That(App.WaitForTextToBePresentInElement("Result", "Drawable=Triangle"), Is.True);

		App.WaitForElement("Input");
		App.EnterText("Input", "5,5,10,0.5");

		Assert.That(App.WaitForTextToBePresentInElement("Result", "Opacity=0.5"), Is.True);
		var result = App.WaitForElement("Result").GetText();
		if (result is null)
			throw new AssertionException("The completed shadow diagnostic result was null.");

		Assert.That(result, Does.Not.Contain("Sequence=-1"));
		Assert.That(result, Does.Contain("Drawable=Triangle"));
		Assert.That(result, Does.Contain("Offset=5,5"));
		Assert.That(result, Does.Contain("Radius=10"));
		Assert.That(result, Does.Contain("Opacity=0.5"));

		var exceptionType = result
			.Split(';')
			.Single(part => part.StartsWith("Exception=", StringComparison.Ordinal))
			["Exception=".Length..];
		Assert.That(exceptionType, Is.Empty,
			$"Shadow binding update threw '{exceptionType}', expected no exception");
	}
}
#endif
