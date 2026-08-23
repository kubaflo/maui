#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue32992 : _IssuesUITest
{
	public Issue32992(TestDevice device) : base(device) { }

	public override string Issue => "Shell TabBarBackgroundColor does not reset to null";

	[Test]
	[Category(UITestCategories.Shell)]
	public void ClearingTabBarBackgroundColorRestoresPlatformDefault()
	{
		App.SetOrientationPortrait();
		var launcherRect = App.WaitForElement("LauncherRoot").GetRect();
		Assert.That(launcherRect.Height, Is.GreaterThan(launcherRect.Width), "The issue must run in portrait.");

		App.Tap("OpenReproductionButton");
		App.WaitForElement("Test");
		App.WaitForElement("Second");
		var reproductionRect = App.WaitForElement("ReproductionRoot").GetRect();
		Assert.That(reproductionRect.Height, Is.GreaterThan(reproductionRect.Width), "The Shell must remain in portrait.");

		var defaultProbe = WaitForProbe("sequence=2;phase=default");
		Assert.That(defaultProbe, Does.Contain(";same=True;items=Test|Second;"));
		Assert.That(defaultProbe, Does.Contain(";stable=True;"));
		var defaultRgba = ReadValue(defaultProbe, "default");
		Assert.That(ReadValue(defaultProbe, "current"), Is.EqualTo(defaultRgba));

		App.Tap("ApplyTabBarColorButton");
		Assert.That(
			App.WaitForTextToBePresentInElement("TabBarPropertyStateLabel", "property=LightBlue;sequence=1"),
			Is.True);
		var appliedProbe = WaitForProbe("sequence=3;phase=applied");
		Assert.That(appliedProbe, Does.Contain(";same=True;items=Test|Second;"));
		var appliedRgba = ReadValue(appliedProbe, "current");
		const string lightBlueRgba = "0.678,0.847,0.902,1.000";
		Assert.That(MaxChannelDelta(ParseRgba(lightBlueRgba), ParseRgba(appliedRgba)), Is.LessThanOrEqualTo(0.02),
			$"The native tab-bar background must reach LightBlue. expected={lightBlueRgba}; actual={appliedRgba}");
		Assert.That(MaxChannelDelta(ParseRgba(defaultRgba), ParseRgba(appliedRgba)), Is.GreaterThan(0.02),
			$"Applying LightBlue must change the native tab-bar background. default={defaultRgba}; actual={appliedRgba}");

		App.Tap("RemoveTabBarColorButton");
		Assert.That(
			App.WaitForTextToBePresentInElement("TabBarPropertyStateLabel", "property=null;sequence=2"),
			Is.True);
		var removedProbe = WaitForProbe("sequence=4;phase=removed");
		Assert.That(removedProbe, Does.Contain(";same=True;items=Test|Second;"));
		var removedRgba = ReadValue(removedProbe, "current");
		var removedDelta = MaxChannelDelta(ParseRgba(defaultRgba), ParseRgba(removedRgba));

		Assert.That(removedDelta, Is.LessThanOrEqualTo(0.02),
			$"iOS Shell tab bar background did not return to its captured platform default after clearing TabBarBackgroundColor; expected={defaultRgba}; actual={removedRgba}; maxDelta={removedDelta:F3}");
	}

	string WaitForProbe(string expectedPrefix)
	{
		Assert.That(
			App.WaitForTextToBePresentInElement("NativeTabBarProbeLabel", expectedPrefix, TimeSpan.FromSeconds(10)),
			Is.True,
			$"Native UITabBar probe did not reach {expectedPrefix}.");
		return App.FindElement("NativeTabBarProbeLabel").GetText()
			?? throw new InvalidOperationException("Native UITabBar probe text was null.");
	}

	static string ReadValue(string probe, string key)
	{
		var prefix = key + "=";
		foreach (var part in probe.Split(';'))
		{
			if (part.StartsWith(prefix, StringComparison.Ordinal))
				return part[prefix.Length..];
		}

		Assert.Fail($"Native UITabBar probe did not contain {key}: {probe}");
		return string.Empty;
	}

	static double[] ParseRgba(string value)
	{
		var components = value.Split(',');
		Assert.That(components, Has.Length.EqualTo(4), $"Invalid RGBA probe value: {value}");
		return components
			.Select(component => double.Parse(component, System.Globalization.CultureInfo.InvariantCulture))
			.ToArray();
	}

	static double MaxChannelDelta(double[] expected, double[] actual)
	{
		var maximum = 0d;
		for (var index = 0; index < expected.Length; index++)
			maximum = Math.Max(maximum, Math.Abs(expected[index] - actual[index]));
		return maximum;
	}
}
#endif
