#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue30834 : _IssuesUITest
{
	public Issue30834(TestDevice device) : base(device) { }

	public override string Issue => "Shell TitleView children are cleared before the outgoing page unloads";

	[Test]
	[Category(UITestCategories.Shell)]
	public void TitleViewChildrenRemainAttachedUntilOutgoingPageUnloads()
	{
		App.SetOrientationPortrait();
		App.WaitForElement("OpenDetail");
		App.Tap("OpenDetail");

		App.WaitForElement("TitleGrid");
		App.WaitForElement("TitleLabel");
		App.WaitForElement("TitleButton");
		App.WaitForElement("ContentProbe");
		App.WaitForElement("InitialProbeReady");

		Assert.That(ReadRequiredText("InitialTitleLabelAttached"), Is.EqualTo("True"));
		Assert.That(ReadRequiredText("InitialTitleButtonAttached"), Is.EqualTo("True"));
		Assert.That(ReadRequiredText("InitialContentAttached"), Is.EqualTo("True"));
		Assert.That(ReadRequiredText("TitleInteractionStatus"), Is.EqualTo("-1"));

		App.Tap("TitleButton");
		Assert.That(ReadRequiredText("TitleInteractionStatus"), Is.EqualTo("1"));

		App.Back();
		App.WaitForElement("OpenDetail");
		App.WaitForElement("PostPopObservationReady");

		string unloadObservation = ReadRequiredText("UnloadObservation");
		string postPopCallback = ReadRequiredText("PostPopCallback");
		string expectedPageId = ReadRequiredText("ExpectedPageId");
		string observedPageId = ReadRequiredText("ObservedPageId");
		string titleLabelAttached = ReadRequiredText("ObservedTitleLabelAttached");
		string titleButtonAttached = ReadRequiredText("ObservedTitleButtonAttached");
		string pageLoaded = ReadRequiredText("ObservedPageLoaded");
		string contentAttached = ReadRequiredText("ObservedContentAttached");
		string earlyDetachCount = ReadRequiredText("EarlyDetachCount");

		Assert.That(unloadObservation, Is.EqualTo("0"), "The TitleView unload observation did not advance from its -1 sentinel.");
		Assert.That(postPopCallback, Is.EqualTo("0"), "The post-pop callback did not advance from its -1 sentinel.");
		Assert.That(expectedPageId, Is.Not.EqualTo("-1"));
		Assert.That(observedPageId, Is.EqualTo(expectedPageId), "The unload observation came from a different detail page.");
		Assert.That(earlyDetachCount, Is.EqualTo("0"),
			$"Shell.TitleView children detached before the outgoing page unloaded: LabelAttached={titleLabelAttached}, ButtonAttached={titleButtonAttached}, PageLoaded={pageLoaded}, ContentAttached={contentAttached}, EarlyDetachCount={earlyDetachCount}; expected both TitleView children to remain attached.");

		string ReadRequiredText(string automationId)
		{
			var value = App.WaitForElement(automationId).GetText();
			if (value is null)
				throw new AssertionException($"Element '{automationId}' returned null text.");

			return value;
		}
	}
}
#endif
