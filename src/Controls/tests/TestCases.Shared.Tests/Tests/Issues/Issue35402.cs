#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35402 : _IssuesUITest
{
	public Issue35402(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "Bundled MauiFont is registered twice at launch on iOS 26";

	[Test]
	[Category(UITestCategories.Fonts)]
	public void BundledFontDoesNotEmitDuplicateRegistrationDiagnostics()
	{
		Assert.That(App, Is.TypeOf<AppiumIOSApp>());
		var iosApp = (AppiumIOSApp)App;
		Assert.That(HelperExtensions.IsIOS26OrHigher(iosApp), Is.True,
			"This test requires an iOS 26 or newer simulator.");

		var syslogType = App.GetLogTypes()
			.First(logType => string.Equals(logType, "syslog", StringComparison.OrdinalIgnoreCase));

		_ = App.GetLogEntries(syslogType).ToArray();

		var alreadyExists = false;
		var failed305 = false;

		void CollectFontDiagnostics()
		{
			foreach (var entry in App.GetLogEntries(syslogType))
			{
				if (!entry.Contains("OpenSans", StringComparison.OrdinalIgnoreCase))
					continue;

				alreadyExists |= entry.Contains("already exists", StringComparison.OrdinalIgnoreCase);
				failed305 |= entry.Contains("GSFontRegisterCGFont", StringComparison.Ordinal) &&
					entry.Contains("failed 305", StringComparison.OrdinalIgnoreCase);
			}
		}

		App.ResetApp();
		App.WaitForGoToTestButtonWithRecovery(Issue);
		App.EnterText("SearchBar", Issue);
		App.WaitForElement("GoToTestButton");
		App.Tap("GoToTestButton");

		var bundledFont = App.WaitForElement("Issue35402BundledFont");
		Assert.That(bundledFont.GetText(), Is.EqualTo("OpenSans bundled font"));

		App.RetryAssert(() =>
		{
			CollectFontDiagnostics();

			var statusElement = App.FindElement("Issue35402NativeFontStatus");
			if (statusElement is null)
			{
				Assert.Fail("The native font status Label was not found after restart.");
				return;
			}

			Assert.That(statusElement.GetText(), Is.Not.EqualTo("Pending"));
		});

		var nativeFontStatus = App.WaitForElement("Issue35402NativeFontStatus").GetText();
		Assert.That(nativeFontStatus, Does.Contain("OpenSans").IgnoreCase,
			"The restarted Label must attach to a native UILabel using the bundled OpenSans font.");

		CollectFontDiagnostics();

		Assert.That((alreadyExists, failed305), Is.EqualTo((false, false)),
			$"Bundled OpenSans MauiFont launch emitted duplicate GSFont diagnostics: alreadyExists={alreadyExists}, failed305={failed305}; expected alreadyExists=False, failed305=False.");
	}
}
#endif
