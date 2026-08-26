#if WINDOWS
using System.Drawing;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue31411 : _IssuesUITest
{
	public Issue31411(TestDevice testDevice)
		: base(testDevice)
	{
	}

	public override string Issue => "Poor CollectionView performance, ghosting, and crashing on Windows";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void RepeatedBulkVisibilityUpdatesDoNotRetainHiddenItems()
	{
		var appiumApp = App as AppiumApp;
		if (appiumApp is null)
			throw new InvalidOperationException("The Windows test requires the Appium application driver.");

		appiumApp.Driver.Manage().Window.Size = new Size(1280, 720);
		var windowSize = appiumApp.Driver.Manage().Window.Size;
		Assert.That(windowSize.Width, Is.EqualTo(1280));
		Assert.That(windowSize.Height, Is.EqualTo(720));

		var collection = App.WaitForElement("Issue31411Collection");
		var collectionBounds = collection.GetRect();
		Assert.That(collectionBounds.Width, Is.GreaterThan(0));
		Assert.That(collectionBounds.Height, Is.GreaterThan(0));
		Assert.That(collectionBounds.Width, Is.LessThanOrEqualTo(windowSize.Width));
		Assert.That(collectionBounds.Height, Is.LessThanOrEqualTo(windowSize.Height));
		Assert.That(App.WaitForElement("Issue31411Status").GetText(), Does.Contain("Ready:2000"));

		App.ScrollDown("Issue31411Collection", ScrollStrategy.Gesture);
		App.ScrollDown("Issue31411Collection", ScrollStrategy.Gesture);
		App.RetryAssert(() =>
		{
			var status = App.WaitForElement("Issue31411Status").GetText();
			Assert.That(status, Does.Not.Contain("FirstVisible:-1"));
			Assert.That(status, Does.Not.Contain("FirstVisible:0"));
		}, timeout: TimeSpan.FromSeconds(20));

		var scrolledEvenIds = FindRealizedItemIds("Issue31411ItemEven");
		var scrolledOddIds = FindRealizedItemIds("Issue31411ItemOdd");
		Assert.That(scrolledEvenIds, Is.Not.Empty, "Scrolling should realize at least one even item root.");
		Assert.That(scrolledOddIds, Is.Not.Empty, "Scrolling should realize at least one odd item root.");

		App.Tap("Issue31411BulkUpdateButton");
		WaitForCompletedCycle(1);

		App.Tap("Issue31411BulkUpdateButton");
		WaitForCompletedCycle(2);
		var restoredEvenIds = FindRealizedItemIds("Issue31411ItemEven");
		var restoredOddIds = FindRealizedItemIds("Issue31411ItemOdd");
		Assert.That(restoredEvenIds, Is.Not.Empty, "The restored state should realize even item roots.");
		Assert.That(restoredOddIds, Is.Not.Empty, "The restored state should realize odd item roots.");

		App.Tap("Issue31411BulkUpdateButton");
		WaitForCompletedCycle(3);
		var finalOddIds = FindRealizedItemIds("Issue31411ItemOdd");
		var finalEvenIds = FindRealizedItemIds("Issue31411ItemEven");

		Assert.That(finalOddIds, Is.Not.Empty, "The CollectionView should remain responsive and retain visible odd item roots.");
		Assert.That(
			finalEvenIds,
			Is.Empty,
			$"Windows CollectionView retained a hidden even item after the third bulk visibility update. Retained count: {finalEvenIds.Length}; IDs: {string.Join(", ", finalEvenIds)}");
	}

	void WaitForCompletedCycle(int cycle)
	{
		App.RetryAssert(() =>
		{
			var status = App.WaitForElement("Issue31411Status").GetText();
			Assert.That(status, Does.Contain($"Cycle:{cycle}"));
		}, timeout: TimeSpan.FromSeconds(20));
	}

	string[] FindRealizedItemIds(string prefix)
	{
		var elements = App.FindElements(
			AppiumQuery.ByXPath($"//*[starts-with(@Name, '{prefix}')]"));
		var ids = new List<string>();

		foreach (var element in elements)
		{
			var semanticName = element.GetAttribute<string>("Name");
			if (semanticName is not null)
				ids.Add(semanticName);
		}

		return ids.ToArray();
	}
}
#endif
