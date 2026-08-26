using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

#if ANDROID
public class Issue31128 : _IssuesUITest
{
	const string CustomIndicatorId = "Issue31128CustomIndicator";

	public Issue31128(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "IndicatorTemplate does not update when set dynamically without an initial value";

	[Test]
	[Category(UITestCategories.IndicatorView)]
	public void DynamicIndicatorTemplateRebuildsAttachedIndicators()
	{
		var indicator = App.WaitForElement("Issue31128IndicatorView");
		Assert.That(indicator, Is.Not.Null);
		Assert.That(indicator.IsDisplayed(), Is.True);

		App.Tap("Issue31128ApplyTemplateButton");

		var applyTemplateButton = App.WaitForElement("Issue31128ApplyTemplateButton");
		Assert.That(applyTemplateButton.GetText(), Is.EqualTo("Template applied"), "The Button.Clicked callback did not complete.");

		var customIndicators = App.FindElements(CustomIndicatorId);
		var visibleCustomIndicatorCount = 0;

		foreach (var customIndicator in customIndicators)
		{
			if (customIndicator.IsDisplayed())
			{
				visibleCustomIndicatorCount++;
			}
		}

		Assert.That(
			visibleCustomIndicatorCount,
			Is.EqualTo(4),
			$"Issue31128 dynamic IndicatorTemplate visual count was {visibleCustomIndicatorCount}; expected 4.");
	}
}
#endif
