#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue34057 : _IssuesUITest
{
	const string ExpectedResult = "Destroying=1;Attempts=1;Exception=None";

	public override string Issue => "[Windows] AnimationManager ObjectDisposedException IServiceProvider on closing window";

	public Issue34057(TestDevice device) : base(device) { }

	[Test]
	[Category(UITestCategories.Window)]
	public void PendingAnimationDoesNotUseDisposedWindowServices()
	{
		var initialResult = App.WaitForElement("Issue34057Result").GetText();
		Assert.That(initialResult, Is.EqualTo("Destroying=-1;Attempts=-1"));

		App.Tap("Issue34057Trigger");

		var result = App.WaitForElement("Issue34057Completion").GetText();
		Assert.That(result, Does.Contain("Destroying=1"), "The child Window.Destroying callback did not run exactly once.");
		Assert.That(result, Does.Contain("Attempts=1"), "The pending popup animation was not attempted exactly once.");
		Assert.That(
			result,
			Is.EqualTo(ExpectedResult),
			$"Issue34057 pending animation result was {result}, expected {ExpectedResult}");
	}
}
#endif
