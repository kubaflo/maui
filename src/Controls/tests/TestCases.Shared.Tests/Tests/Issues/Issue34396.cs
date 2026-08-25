using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue34396 : _IssuesUITest
{
	public override string Issue => "UI becomes unresponsive when adding more than 200 Entry children to AbsoluteLayout";

	public Issue34396(TestDevice device) : base(device) { }

#if ANDROID
	[Test]
	[Category(UITestCategories.Layout)]
	public void AddingEntriesDoesNotBlockDispatcher()
	{
		App.WaitForElement("AddEditorsButton");
		Assert.That(App.WaitForTextToBePresentInElement("TimingMetrics", "CallbackToken=0"), Is.True);
		Assert.That(App.WaitForTextToBePresentInElement("TimingMetrics", "ChildCount=0"), Is.True);

		var baselineText = App.WaitForElement("TimingMetrics").GetText();
		if (baselineText is null)
			throw new AssertionException("The baseline timing metrics were not available.");

		var baselineParts = baselineText.Split(';');
		var baselineMilliseconds = double.Parse(baselineParts[2]["BaselineMs=".Length..], CultureInfo.InvariantCulture);
		Assert.That(baselineMilliseconds, Is.GreaterThan(0));
		Assert.That(baselineMilliseconds, Is.LessThan(Math.Max(1000, baselineMilliseconds * 20)));

		App.Tap("AddEditorsButton");

		Assert.That(App.WaitForTextToBePresentInElement("TimingMetrics", "CallbackToken=1", TimeSpan.FromSeconds(30)), Is.True);
		Assert.That(App.WaitForTextToBePresentInElement("TimingMetrics", "ChildCount=201", TimeSpan.FromSeconds(30)), Is.True);

		var postAddText = App.WaitForElement("TimingMetrics").GetText();
		if (postAddText is null)
			throw new AssertionException("The post-add timing metrics were not available.");

		var postAddParts = postAddText.Split(';');
		var callbackToken = int.Parse(postAddParts[0]["CallbackToken=".Length..], CultureInfo.InvariantCulture);
		var childCount = int.Parse(postAddParts[1]["ChildCount=".Length..], CultureInfo.InvariantCulture);
		var measuredBaselineMilliseconds = double.Parse(postAddParts[2]["BaselineMs=".Length..], CultureInfo.InvariantCulture);
		var postAddMilliseconds = double.Parse(postAddParts[3]["PostAddMs=".Length..], CultureInfo.InvariantCulture);
		var limitMilliseconds = Math.Max(1000, measuredBaselineMilliseconds * 20);
		var dispatcherRemainedResponsive = postAddMilliseconds < limitMilliseconds;

		Assert.That(callbackToken, Is.EqualTo(1));
		Assert.That(childCount, Is.EqualTo(201));
		Assert.That(
			dispatcherRemainedResponsive,
			Is.True,
			"Adding 201 default Entry children blocked the Android UI dispatcher.");
	}
#endif
}
