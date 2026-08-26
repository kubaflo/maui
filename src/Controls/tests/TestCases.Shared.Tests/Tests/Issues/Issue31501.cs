#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue31501 : _IssuesUITest
{
	public Issue31501(TestDevice device) : base(device) { }

	public override string Issue => "CollectionView header binding does not update after replacing its source";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void ReplacingHeaderSourceUpdatesRenderedHeaderTemplate()
	{
		var initialHeader = App.WaitForElement("HeaderValueLabel");
		var initialHeaderText = initialHeader.GetText();
		Assert.That(initialHeaderText, Is.Not.Null);
		Assert.That(initialHeaderText, Is.EqualTo("Before tap"));

		App.WaitForElement("HeaderHasDataContent");
		var pagePropertyLabel = App.WaitForElement("PagePropertyLabel");
		var pagePropertyText = pagePropertyLabel.GetText();
		Assert.That(pagePropertyText, Is.Not.Null);
		Assert.That(pagePropertyText, Is.EqualTo("Property in MVVM"));

		var initialTriggerState = App.WaitForElement("TriggerState").GetText();
		Assert.That(initialTriggerState, Is.Not.Null);
		Assert.That(initialTriggerState, Is.EqualTo("NotTriggered"));

		App.WaitForElement("ReplaceDataButton");
		App.Tap("ReplaceDataButton");

		Assert.That(
			App.WaitForTextToBePresentInElement("TriggerState", "Observed:", TimeSpan.FromSeconds(10)),
			Is.True,
			"The dispatched post-trigger probe did not run.");

		var observedProbeState = App.FindElement("TriggerState");
		Assert.That(observedProbeState, Is.Not.Null);
		var observedProbeText = observedProbeState.GetText();
		Assert.That(observedProbeText, Is.Not.Null);
		Assert.That(
			observedProbeText,
			Is.EqualTo("Observed:After tap"),
			$"CollectionView header binding had not rendered the replacement Data.StringValue when the dispatched post-trigger probe ran. {observedProbeText}");
	}
}
#endif
