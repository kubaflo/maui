#if IOS && !MACCATALYST
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests;

[Category(TestCategory.DatePicker)]
[Category("Issue36933")]
[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
public class Issue36933 : ControlsHandlerTestBase
{
	const double ColorTolerance = 0.01;

	[Fact]
	public async Task ClearingBackgroundRestoresPlatformDefault()
	{
		EnsureHandlerCreated(builder =>
		{
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler<Window, WindowHandler>();
				handlers.AddHandler<Page, PageHandler>();
				handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
				handlers.AddHandler<Label, LabelHandler>();
				handlers.AddHandler<Button, ButtonHandler>();
				handlers.AddHandler<DatePicker, DatePickerHandler>();
				handlers.AddHandler<TimePicker, TimePickerHandler>();
			});
		});

		var headingLabel = new Label
		{
			FontAttributes = FontAttributes.Bold,
			FontSize = 22,
			Text = "DatePicker and TimePicker background clearing"
		};
		var stateLabel = new Label { Text = "Reference state: platform-default picker backgrounds" };
		var datePicker = new DatePicker { HorizontalOptions = LayoutOptions.Fill };
		var timePicker = new TimePicker { HorizontalOptions = LayoutOptions.Fill };
		var toggleButton = new Button { Text = "Set orange picker backgrounds" };
		var informationLabel = new Label
		{
			FontAttributes = FontAttributes.Bold,
			Text = "Picker background appearance"
		};
		var layout = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 18,
			Children =
			{
				headingLabel,
				stateLabel,
				datePicker,
				timePicker,
				toggleButton,
				informationLabel
			}
		};
		var page = new ContentPage
		{
			Title = "Picker background",
			Content = layout
		};

		var callbackPhase = -1;
		toggleButton.Clicked += (_, _) =>
		{
			if (callbackPhase < 1)
			{
				datePicker.Background = new SolidColorBrush(Colors.Orange);
				timePicker.Background = new SolidColorBrush(Colors.Orange);
				stateLabel.Text = "Reference state: orange picker backgrounds applied";
				toggleButton.Text = "Clear picker backgrounds";
				callbackPhase = 1;
			}
			else
			{
				datePicker.Background = null;
				timePicker.Background = null;
				stateLabel.Text = "Clear requested: picker backgrounds should be default";
				callbackPhase = 2;
			}
		};

		await CreateHandlerAndAddToWindow(page, async () =>
		{
			var datePickerHandler = datePicker.Handler as DatePickerHandler;
			var timePickerHandler = timePicker.Handler as TimePickerHandler;
			var buttonHandler = toggleButton.Handler as ButtonHandler;
			Assert.NotNull(datePickerHandler);
			Assert.NotNull(timePickerHandler);
			Assert.NotNull(buttonHandler);
			Assert.NotNull(datePickerHandler.PlatformView);
			Assert.NotNull(timePickerHandler.PlatformView);
			Assert.NotNull(buttonHandler.PlatformView);

			var nativeDatePicker = datePickerHandler.PlatformView;
			var nativeTimePicker = timePickerHandler.PlatformView;
			var initialDatePickerBackground = nativeDatePicker.BackgroundColor;
			var initialTimePickerBackground = nativeTimePicker.BackgroundColor;

			buttonHandler.PlatformView.SendActionForControlEvents(UIControlEvent.TouchUpInside);

			Assert.Equal(1, callbackPhase);
			await AssertEventually(
				() => IsOrange(nativeDatePicker.BackgroundColor),
				message: "DatePicker native background did not become orange.");
			await AssertEventually(
				() => IsOrange(nativeTimePicker.BackgroundColor),
				message: "TimePicker native background did not become orange.");

			buttonHandler.PlatformView.SendActionForControlEvents(UIControlEvent.TouchUpInside);

			Assert.Equal(2, callbackPhase);
			await AssertEventually(
				() => ColorsMatch(initialDatePickerBackground, nativeDatePicker.BackgroundColor),
				message: $"DatePicker native background remained orange after Background was cleared; actual {FormatColor(nativeDatePicker.BackgroundColor)}, expected platform default {FormatColor(initialDatePickerBackground)}.");
			await AssertEventually(
				() => ColorsMatch(initialTimePickerBackground, nativeTimePicker.BackgroundColor),
				message: $"TimePicker native background remained orange after Background was cleared; actual {FormatColor(nativeTimePicker.BackgroundColor)}, expected platform default {FormatColor(initialTimePickerBackground)}.");
		});
	}

	static bool IsOrange(UIColor color)
	{
		if (color is null)
			return false;

		color.GetRGBA(out var red, out var green, out var blue, out var alpha);
		return CloseTo(red, 1) &&
			CloseTo(green, 165d / 255d) &&
			CloseTo(blue, 0) &&
			CloseTo(alpha, 1);
	}

	static bool ColorsMatch(UIColor expected, UIColor actual)
	{
		if (expected is null || actual is null)
			return expected is null && actual is null;

		expected.GetRGBA(out var expectedRed, out var expectedGreen, out var expectedBlue, out var expectedAlpha);
		actual.GetRGBA(out var actualRed, out var actualGreen, out var actualBlue, out var actualAlpha);
		return CloseTo(expectedRed, actualRed) &&
			CloseTo(expectedGreen, actualGreen) &&
			CloseTo(expectedBlue, actualBlue) &&
			CloseTo(expectedAlpha, actualAlpha);
	}

	static string FormatColor(UIColor color)
	{
		if (color is null)
			return "null";

		color.GetRGBA(out var red, out var green, out var blue, out var alpha);
		return $"rgba({red:F3}, {green:F3}, {blue:F3}, {alpha:F3})";
	}

	static bool CloseTo(double first, double second) =>
		Math.Abs(first - second) < ColorTolerance;
}
#endif

