#if IOSUITEST
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue36933 : _IssuesUITest
{
	public Issue36933(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "DatePicker and TimePicker Background is not cleared when set to null at runtime";

	[Test]
	[Category(UITestCategories.DatePicker)]
	public void ClearingBackgroundRestoresNativePlatformDefaults()
	{
		App.WaitForTextToBePresentInElement(
			"StateLabel",
			"Ready: attached platform-default picker backgrounds captured.");

		App.Tap("ToggleBackgroundButton");
		App.WaitForTextToBePresentInElement("StateLabel", "Gold backgrounds applied natively.");

		App.Tap("ToggleBackgroundButton");
		App.WaitForTextToBePresentInElement(
			"StateLabel",
			"Native check complete: click count 2; managed backgrounds null: True.");

		var result = App.FindElement("ResultLabel").GetText();

		Assert.Multiple(() =>
		{
			Assert.That(
				result,
				Does.Contain("DatePicker: CLEARED"),
				"DatePicker native background should return to its platform-default state after Background is set to null");
			Assert.That(
				result,
				Does.Contain("TimePicker: CLEARED"),
				"TimePicker native background should return to its platform-default state after Background is set to null");
		});
	}
}
#endif
