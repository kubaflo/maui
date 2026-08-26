#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue32213 : _IssuesUITest
{
	public Issue32213(TestDevice device) : base(device) { }

	public override string Issue => "Windows CollectionView header and footer templates are ignored";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void HeaderAndFooterTemplatesRenderWhenCollectionViewBecomesVisible()
	{
		const string expectedArrangement =
			"Header=Header; Footer=Footer; HeaderTemplate=True; FooterTemplate=True; Items=4";

		var arrangement = App.WaitForElement("ArrangementLabel");
		if (arrangement is null)
		{
			Assert.Fail("The CollectionView arrangement label was not found.");
			return;
		}

		Assert.That(arrangement.GetText(), Is.EqualTo(expectedArrangement));

		var headerCount = -1;
		var footerCount = -1;
		var observedResult = "<not observed>";
		headerCount = App.FindElements("HeaderTemplateLabel").Count;
		footerCount = App.FindElements("FooterTemplateLabel").Count;
		Assert.That(headerCount, Is.EqualTo(0), "HeaderTemplate content should be absent while the CollectionView is hidden");
		Assert.That(footerCount, Is.EqualTo(0), "FooterTemplate content should be absent while the CollectionView is hidden");

		App.Tap("ShowCollectionButton");

		var collectionView = App.WaitForElement("IssueCollectionView");
		if (collectionView is null)
		{
			Assert.Fail("The CollectionView did not become visible.");
			return;
		}

		var collectionViewRect = collectionView.GetRect();
		Assert.That(collectionViewRect.Width, Is.GreaterThan(0), "The visible CollectionView should have positive width");
		Assert.That(collectionViewRect.Height, Is.GreaterThan(0), "The visible CollectionView should have positive height");

		App.WaitForElement("CheckTemplatesButton");
		App.Tap("CheckTemplatesButton");
		headerCount = App.FindElements("HeaderTemplateLabel").Count;
		footerCount = App.FindElements("FooterTemplateLabel").Count;
		observedResult =
			$"Status={App.FindElement("ArrangementLabel").GetText()}; Header={headerCount}; Footer={footerCount}";

		Assert.That(
			observedResult,
			Is.EqualTo("Status=Both templates loaded; Header=1; Footer=1"),
			$"HeaderTemplate content was not rendered after CollectionView became visible; observed '{observedResult}', expected both templates loaded and one rendered instance of each");
	}
}
#endif
