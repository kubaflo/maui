#if MACCATALYST
using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests;

[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
[Category(TestCategory.Picker)]
[Category("Issue30532")]
public class Issue30532 : ControlsHandlerTestBase
{
	[Fact]
	public async Task CharacterSpacingUpdatesAfterTimePickerIsAttached()
	{
		EnsureHandlerCreated(builder =>
		{
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler<Window, WindowHandlerStub>();
				handlers.AddHandler<Page, PageHandler>();
				handlers.AddHandler<ScrollView, ScrollViewHandler>();
				handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
				handlers.AddHandler<Label, LabelHandler>();
				handlers.AddHandler<Button, ButtonHandler>();
				handlers.AddHandler<TimePicker, TimePickerHandler>();
			});
		});

		const double expectedCharacterSpacing = 20;
		const double tolerance = 0.01;
		var originalCulture = CultureInfo.CurrentCulture;
		var originalDefaultCulture = CultureInfo.DefaultThreadCurrentCulture;
		var testCulture = new CultureInfo("en-US");

		CultureInfo.CurrentCulture = testCulture;
		CultureInfo.DefaultThreadCurrentCulture = testCulture;

		try
		{
			Assert.Equal(testCulture.Name, CultureInfo.CurrentCulture.Name);

			var timePicker = new TimePicker
			{
				Time = new TimeSpan(11, 0, 0),
				Format = "hh:mm tt",
				HorizontalOptions = LayoutOptions.Start,
				CharacterSpacing = 0
			};
			var page = new ContentPage
			{
				Content = new ScrollView
				{
					Content = new VerticalStackLayout
					{
						Padding = 24,
						Spacing = 16,
						Children = { timePicker }
					}
				}
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				var handler = Assert.IsType<TimePickerHandler>(timePicker.Handler);
				var platformView = Assert.IsType<UIDatePicker>(handler.PlatformView);

				Assert.True(
					TryGetDisplayedText(platformView, out var initialText, out var initialCharacterSpacing),
					"The attached TimePicker did not expose its displayed native text.");
				Assert.False(string.IsNullOrWhiteSpace(initialText));
				Assert.Equal(0, initialCharacterSpacing, 2);

				var observedCharacterSpacing = -1d;
				timePicker.PropertyChanged += (_, args) =>
				{
					if (args.PropertyName == TimePicker.CharacterSpacingProperty.PropertyName)
						observedCharacterSpacing = timePicker.CharacterSpacing;
				};

				timePicker.CharacterSpacing = expectedCharacterSpacing;

				Assert.Equal(expectedCharacterSpacing, observedCharacterSpacing);
				await AssertEventually(
					() =>
					{
						if (!TryGetDisplayedText(platformView, out var updatedText, out var updatedCharacterSpacing))
							return false;

						return initialText == updatedText &&
							Math.Abs(updatedCharacterSpacing - expectedCharacterSpacing) <= tolerance;
					},
					message: "TimePicker native character spacing after update did not match the expected value 20.");

				Assert.True(TryGetDisplayedText(platformView, out var finalText, out var actualCharacterSpacing));
				Assert.Equal(initialText, finalText);
				Assert.True(
					Math.Abs(actualCharacterSpacing - expectedCharacterSpacing) <= tolerance,
					$"TimePicker native character spacing after update was {actualCharacterSpacing}, expected {expectedCharacterSpacing}.");
			});
		}
		finally
		{
			CultureInfo.CurrentCulture = originalCulture;
			CultureInfo.DefaultThreadCurrentCulture = originalDefaultCulture;
		}
	}

	static bool TryGetDisplayedText(
		UIView view,
		out string text,
		out double characterSpacing)
	{
		switch (view)
		{
			case UILabel label when label.AttributedText is { Length: > 0 } labelText:
				text = labelText.Value;
				characterSpacing = labelText.GetCharacterSpacing();
				return true;
			case UILabel label when !string.IsNullOrWhiteSpace(label.Text):
				text = label.Text;
				characterSpacing = 0;
				return true;
			case UITextField textField when textField.AttributedText is { Length: > 0 } fieldText:
				text = fieldText.Value;
				characterSpacing = fieldText.GetCharacterSpacing();
				return true;
			case UITextField textField when !string.IsNullOrWhiteSpace(textField.Text):
				text = textField.Text;
				characterSpacing = 0;
				return true;
			case UIButton button when button.GetAttributedTitle(UIControlState.Normal) is { Length: > 0 } buttonText:
				text = buttonText.Value;
				characterSpacing = buttonText.GetCharacterSpacing();
				return true;
			case UIButton button when !string.IsNullOrWhiteSpace(button.CurrentTitle):
				text = button.CurrentTitle;
				characterSpacing = 0;
				return true;
		}

		foreach (var subview in view.Subviews)
		{
			if (TryGetDisplayedText(subview, out text, out characterSpacing))
				return true;
		}

		text = null;
		characterSpacing = 0;
		return false;
	}
}
#endif

