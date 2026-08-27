#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35861 : _IssuesUITest
{
	public Issue35861(TestDevice device)
		: base(device)
	{
	}

	public override string Issue => "Android permission requests retain stale entries after off-main-thread failures";

	[Test]
	[Category(UITestCategories.Essentials)]
	public void FailedOffMainThreadRequestsDoNotPoisonFinalMainThreadRequest()
	{
		App.WaitForElement("Issue35861ScrollView");
		App.WaitForElement("Issue35861Stack");
		App.WaitForElement("Issue35861Title");
		App.WaitForElement("Issue35861Explanation");

		var details = App.WaitForElement("Issue35861Details").GetText();
		Assert.That(details, Is.Not.Null);
		Assert.That(details, Is.EqualTo("CALLBACK=NOT_STARTED; FAILURES=-1"));

		var finalStateElement = App.WaitForElement("Issue35861FinalState");
		var finalState = finalStateElement.GetText();
		Assert.That(finalState, Is.Not.Null);
		Assert.That(finalState, Is.EqualTo("NOT_STARTED"));

		App.Tap("Issue35861StartButton");

		var permissionDialog = AppiumQuery.ById("com.android.permissioncontroller:id/grant_dialog");
		App.WaitForElement(
			permissionDialog,
			"The initial Android location permission dialog did not appear.",
			TimeSpan.FromSeconds(10));
		App.Back();

		Assert.That(
			App.WaitForTextToBePresentInElement(
				"Issue35861Details",
				"CALLBACK=INITIAL_CALLBACK_COMPLETED",
				TimeSpan.FromSeconds(30)),
			Is.True,
			"The initial Android permission callback did not complete.");
		Assert.That(
			App.WaitForTextToBePresentInElement(
				"Issue35861Details",
				"FAILURES=999",
				TimeSpan.FromSeconds(30)),
			Is.True,
			"The expected 999 off-main-thread PermissionException results were not observed.");

		finalState = finalStateElement.GetText();
		Assert.That(finalState, Is.Not.Null);

		if (finalState == "FINAL_REQUEST_STARTED")
		{
			App.WaitForElement(
				permissionDialog,
				"The corrected final main-thread permission request did not reach Android.",
				TimeSpan.FromSeconds(10));
			App.Back();

			Assert.That(
				App.WaitForTextToBePresentInElement(
					"Issue35861FinalState",
					"COMPLETED",
					TimeSpan.FromSeconds(30)),
				Is.True,
				"The final Android permission callback did not complete.");

			finalState = finalStateElement.GetText();
			Assert.That(finalState, Is.Not.Null);
		}

		Assert.That(
			finalState,
			Is.EqualTo("COMPLETED"),
			$"Issue35861 final main-thread request collided with a stale Android permission request code. Observed state: {finalState}; expected state: COMPLETED.");
	}
}
#endif
