#if IOSUITEST
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue33401 : _IssuesUITest
{
	public Issue33401(TestDevice device) : base(device) { }

	public override string Issue => "CollectionView SelectionChanged is not raised inside a Grid with a TapGestureRecognizer";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void ItemTapRaisesSelectionChangedAlongsideParentGridTap()
	{
		if (App is not AppiumIOSApp iosApp || !HelperExtensions.IsIOS26OrHigher(iosApp))
		{
			return;
		}

		Assert.That(App.WaitForElement("Issue33401Item").GetText(), Is.EqualTo("Issue 33401 item"));
		Assert.That(App.WaitForElement("Issue33401TapStatus").GetText(), Is.EqualTo("Grid tap not received."));
		Assert.That(App.WaitForElement("Issue33401SelectionStatus").GetText(), Is.EqualTo("SelectionChanged not received."));

		App.Tap("Issue33401Item");

		App.WaitForTextToBePresentInElement("Issue33401TapStatus", "Grid tap received.");
		Assert.That(App.FindElement("Issue33401TapStatus").GetText(), Is.EqualTo("Grid tap received."));

		var selectionChangedReceived = App.WaitForTextToBePresentInElement(
			"Issue33401SelectionStatus",
			"SelectionChanged received.");
		var selectionStatus = App.FindElement("Issue33401SelectionStatus").GetText();

		Assert.That(selectionChangedReceived, Is.True,
			$"CollectionView selection callback state after item tap was '{selectionStatus}'; expected 'SelectionChanged received.'.");
	}
}
#endif
