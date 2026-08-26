#if WINDOWS
using System.Collections.Generic;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue33607 : _IssuesUITest
{
	public override string Issue => "[Windows] ObjectDisposedException after closing window";

	public Issue33607(TestDevice device) : base(device)
	{
	}

	[Test]
	[Category(UITestCategories.Window)]
	public void BindableLayoutInsertionAfterWindowCloseDoesNotUseDisposedServices()
	{
		var initialState = App.WaitForElement("Issue33607State").GetText();
		Assert.That(initialState, Is.EqualTo(
			"attempt=0; loaded=false; closed=false; collectionChangeApplied=false; insertReturned=false; collectionCount=0; exception=none"));

		var attemptStates = new List<string>();
		for (var attempt = 1; attempt <= 3; attempt++)
		{
			App.Tap("Issue33607RunAttempt");
			App.WaitForElement($"Issue33607Attempt{attempt}Loaded");
			App.WaitForElement($"Issue33607Attempt{attempt}Closed");
			App.WaitForElement($"Issue33607Attempt{attempt}Complete");

			var attemptState = App.WaitForElement("Issue33607State").GetText();
			if (attemptState is null)
			{
				Assert.Fail($"Issue 33607 post-close insertion failed: state was null for attempt {attempt}");
				return;
			}

			attemptStates.Add(attemptState);
		}

		for (var attempt = 1; attempt <= 3; attempt++)
		{
			var actualState = attemptStates[attempt - 1];
			var expectedState =
				$"attempt={attempt}; loaded=true; closed=true; collectionChangeApplied=true; insertReturned=true; collectionCount=2; exception=none";

			Assert.That(actualState, Is.EqualTo(expectedState),
				$"Issue 33607 post-close insertion failed: {actualState}");
		}
	}
}
#endif
