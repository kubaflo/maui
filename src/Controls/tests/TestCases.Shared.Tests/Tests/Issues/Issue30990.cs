#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue30990 : _IssuesUITest
{
	public Issue30990(TestDevice testDevice) : base(testDevice) { }

	public override string Issue => "Shell toolbar ignores shell properties";

	[Test]
	[Category(UITestCategories.Shell)]
	public void ShellForegroundColorTintsIconToolbarItem()
	{
		Assert.That(
			App.WaitForTextToBePresentInElement("NativeStateLabel", "CALLBACK=COMPLETE", TimeSpan.FromSeconds(10)),
			Is.True);

		var nativeStateElement = App.FindElement("NativeStateLabel");
		Assert.That(nativeStateElement, Is.Not.Null);
		if (nativeStateElement is null)
		{
			Assert.Fail("The native toolbar state element was not found.");
			return;
		}

		var stateText = nativeStateElement.GetText();
		Assert.That(stateText, Is.Not.Null);
		if (stateText is null)
		{
			Assert.Fail("The native toolbar state was not reported.");
			return;
		}

		var state = stateText.Split(';')
			.Select(part => part.Split('=', 2))
			.Where(parts => parts.Length == 2)
			.ToDictionary(parts => parts[0], parts => parts[1]);

		string GetRequiredState(string key)
		{
			if (!state.TryGetValue(key, out var value) || value is null)
				throw new AssertionException($"The native toolbar state did not contain '{key}'. State={stateText}");

			return value;
		}

		var callback = GetRequiredState("CALLBACK");
		var textId = GetRequiredState("TextId");
		var iconId = GetRequiredState("IconId");
		var iconPresent = GetRequiredState("IconPresent");
		var effectiveColor = GetRequiredState("Effective");
		var textColor = GetRequiredState("Text");
		var iconColor = GetRequiredState("Icon");

		Assert.That(callback, Is.EqualTo("COMPLETE"));
		Assert.That(textId, Is.EqualTo("Text 1"));
		Assert.That(iconId, Is.EqualTo("IconToolbarItem"));
		Assert.That(iconPresent, Is.EqualTo("True"));
		Assert.That(effectiveColor, Is.EqualTo("FFFF0000"));
		Assert.That(textColor, Is.EqualTo(effectiveColor));
		Assert.That(
			iconColor,
			Is.EqualTo(effectiveColor),
			$"Shell toolbar icon tint did not match Shell.ForegroundColor. Effective={effectiveColor}; Icon={iconColor}");
	}
}
#endif
