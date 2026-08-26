using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue34396 : _IssuesUITest
{
	public override string Issue => "UI becomes unresponsive when adding 200 Entry children to AbsoluteLayout";

	public Issue34396(TestDevice device) : base(device) { }

#if ANDROID
	[Test]
	[Category(UITestCategories.Performance)]
	public void AddingEntriesToAttachedAbsoluteLayoutRemainsResponsive()
	{
		App.WaitForElement("Issue34396AddEditorsButton");

		var initialStatus = App.FindElement("Issue34396Status").GetText();
		if (initialStatus is null)
		{
			Assert.Fail("Issue34396 initial status text was null.");
			return;
		}

		Assert.That(initialStatus, Is.EqualTo("Count=-1;ElapsedMs=-1"));
		Assert.That(App.FindElements("Editor 1"), Is.Empty);

		App.Tap("Issue34396AddEditorsButton");

		App.WaitForElement("Editor 1");
		Assert.That(
			App.WaitForTextToBePresentInElement(
				"Issue34396Status",
				"Count=200;",
				TimeSpan.FromSeconds(10)),
			Is.True,
			"The bulk addition should complete and report its measured duration.");

		var status = App.FindElement("Issue34396Status").GetText();
		if (status is null)
		{
			Assert.Fail("Issue34396 completion status text was null.");
			return;
		}

		const string countPrefix = "Count=";
		const string elapsedPrefix = ";ElapsedMs=";
		int separatorIndex = status.IndexOf(elapsedPrefix, StringComparison.Ordinal);
		if (!status.StartsWith(countPrefix, StringComparison.Ordinal) || separatorIndex < countPrefix.Length)
		{
			Assert.Fail($"Issue34396 completion status had an unexpected format: '{status}'.");
			return;
		}

		bool countParsed = int.TryParse(
			status.AsSpan(countPrefix.Length, separatorIndex - countPrefix.Length),
			NumberStyles.None,
			CultureInfo.InvariantCulture,
			out int count);
		bool elapsedParsed = double.TryParse(
			status.AsSpan(separatorIndex + elapsedPrefix.Length),
			NumberStyles.Float,
			CultureInfo.InvariantCulture,
			out double elapsedMilliseconds);

		Assert.That(countParsed, Is.True, $"Issue34396 could not parse the child count from '{status}'.");
		Assert.That(elapsedParsed, Is.True, $"Issue34396 could not parse the elapsed duration from '{status}'.");
		Assert.That(count, Is.EqualTo(200), "The attached AbsoluteLayout should contain exactly 200 Entry children.");
		Assert.That(
			elapsedMilliseconds < 1000d,
			Is.True,
			"Issue34396 bulk add blocked the UI thread; expected completion in less than 1000 ms.");
	}
#endif
}
