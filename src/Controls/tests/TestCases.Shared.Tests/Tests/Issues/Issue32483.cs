using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues
{
	public class Issue32483 : _IssuesUITest
	{
		public Issue32483(TestDevice device) : base(device)
		{
		}

		public override string Issue => "CursorPosition not calculated correctly on iOS during TextChanged in behaviors";

		[Test]
		[Category(UITestCategories.Entry)]
		public void EntryCursorPositionShouldBeCorrectDuringTextChanged()
		{
			// Wait for the entry to be ready
			App.WaitForElement("TestEntry");

			// Type a sequence of digits that will trigger masking
			// Typing "123" should become "1.23" after masking
			App.Click("TestEntry");
			App.EnterText("TestEntry", "123");

			// Wait for text processing
			App.WaitForElement("StatusLabel");

			// The status label should show "Valid cursor" - the fix ensures CursorPosition
			// returns the correct value instead of 0 or random values
			var statusText = App.FindElement("StatusLabel").GetText();
			
			// The status should indicate a valid cursor position (not 0 when it shouldn't be)
			Assert.That(statusText, Does.Contain("Valid cursor"), 
				"CursorPosition should return correct value during TextChanged event on iOS");

			// Verify screenshot shows the masked value
			VerifyScreenshot();
		}
	}
}
