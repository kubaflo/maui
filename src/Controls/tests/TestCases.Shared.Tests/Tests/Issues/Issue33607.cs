#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue33607 : _IssuesUITest
{
	public override string Issue => "[Windows] ObjectDisposedException after closing window";

	public Issue33607(TestDevice device) : base(device) { }

	[Test]
	[Category(UITestCategories.Window)]
	public void RetainedILayoutCanBeMutatedAfterWindowIsClosed()
	{
		Assert.That(ReadText("Issue33607CompletedCycles"), Is.EqualTo("-1"));
		Assert.That(ReadText("Issue33607DestroyedWindows"), Is.EqualTo("0"));
		Assert.That(ReadText("Issue33607PostCloseCallbacks"), Is.EqualTo("0"));
		Assert.That(ReadText("Issue33607ExceptionCount"), Is.EqualTo("0"));

		var runButton = App.WaitForElement("Issue33607Run");
		if (runButton is null)
			Assert.Fail("The window-cycle button was not found.");

		App.Tap("Issue33607Run");

		var destroyedComplete = App.WaitForElement("Issue33607DestroyedComplete");
		if (destroyedComplete is null)
			Assert.Fail("Three distinct windows were not destroyed.");

		var callbacksComplete = App.WaitForElement("Issue33607CallbacksComplete");
		if (callbacksComplete is null)
			Assert.Fail("Three post-close collection mutation callbacks did not run.");

		var completion = App.WaitForElement("Issue33607Completed");
		if (completion is null)
			Assert.Fail("The window-close cycles did not complete.");

		Assert.That(ReadText("Issue33607CompletedCycles"), Is.EqualTo("3"));
		Assert.That(ReadText("Issue33607DestroyedWindows"), Is.EqualTo("3"));
		Assert.That(ReadText("Issue33607PostCloseCallbacks"), Is.EqualTo("3"));
		Assert.That(ReadText("Issue33607ArrangedLayouts"), Is.EqualTo("3"));
		Assert.That(ReadText("Issue33607MutatedCollections"), Is.EqualTo("3"));

		var exceptionCount = ReadText("Issue33607ExceptionCount");
		Assert.That(
			exceptionCount,
			Is.EqualTo("0"),
			$"Post-close ILayout collection changes must not access a disposed IServiceProvider; observed exception count={exceptionCount}");
	}

	string ReadText(string automationId)
	{
		var element = App.WaitForElement(automationId);
		if (element is null)
			throw new AssertionException($"Element '{automationId}' was not found.");

		var text = element.GetText();
		if (text is null)
			throw new AssertionException($"Element '{automationId}' did not expose text.");

		return text;
	}
}
#endif
